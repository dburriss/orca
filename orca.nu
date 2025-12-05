#!/usr/bin/env nu

# Orca CLI Tool for Bulk Repository Upgrades
# Creates a GitHub Project from YAML configuration

def ensure_auth [] {
    if ($env | get -o CI | default false) {
        # In CI, assume GH_TOKEN is set
        if ($env | get -o GH_TOKEN | default "" | is-empty) {
            error make {msg: "In CI environment but GH_TOKEN is not set"}
        }
        print "Using GH_TOKEN for authentication in CI"
    } else {
        # Local environment, check if authenticated
        let auth_result = do { gh auth status } | complete
        if $auth_result.exit_code != 0 {
            print "GitHub CLI is not authenticated. Please run 'gh auth login' to authenticate locally."
            error make {msg: "GitHub CLI authentication required"}
        }
        print "GitHub CLI is authenticated"
    }
}

def main [--verbose, yaml_file: string] {
    # Ensure authentication is set up
    ensure_auth

    # Load and parse YAML configuration
    let config = try {
        open $yaml_file
    } catch { |err|
        error make {msg: $"Failed to parse YAML file '($yaml_file)': ($err.msg)"}
    }

    if $verbose {
        print $"Loaded config: ($config)"
    }

    # Extract job configuration
    let job = $config | get job
    let project_name = $job | get title
    let org = $job | get org
    let repos = $config | get repos
    let issue_config = $config | get issue
    let template_rel_path = $issue_config | get template
    let labels = $issue_config | get labels

    # Validate inputs
    if ($project_name | is-empty) {
        error make {msg: "Project name is required in YAML"}
    }
    if ($org | is-empty) {
        error make {msg: "Organization is required in YAML"}
    }
    if ($repos | is-empty) {
        error make {msg: "Repositories list is required in YAML"}
    }
    if ($template_rel_path | is-empty) {
        error make {msg: "Issue template path is required in YAML"}
    }

    # Read issue template
    let yaml_dir = $yaml_file | path dirname
    let template_path = $yaml_dir + "/" + $template_rel_path
    let template_content = try {
        open $template_path
    } catch { |err|
        error make {msg: $"Failed to read template file '($template_path)': ($err.msg)"}
    }

    # Use project name as issue title
    let title = $project_name

    # Check if project already exists (idempotency)
    let list_result = do { gh project list --owner $org --format json } | complete
    if $list_result.exit_code != 0 {
        error make {msg: $"Failed to list projects: ($list_result.stderr)"}
    }
    let existing_projects = $list_result.stdout | from json
    if $verbose {
        print $"Existing projects: ($existing_projects)"
    }
    let project_exists = $existing_projects.projects | any {|p| $p.title == $project_name}

    let project_number = if $project_exists {
        let project = $existing_projects.projects | where {|p| $p.title == $project_name} | first
        print $"Project '($project_name)' already exists in org '($org)'. Using existing project."
        $project.number
    } else {
        # Create the project
        let create_result = do { gh project create --title $project_name --owner $org --format json } | complete
        if $create_result.exit_code != 0 {
            error make {msg: $"Failed to create project: ($create_result.stderr)"}
        } else {
            print $"Successfully created project '($project_name)' in org '($org)'"
            # List again to get the number
            let list_result2 = do { gh project list --owner $org --format json } | complete
            if $list_result2.exit_code != 0 {
                error make {msg: $"Failed to list projects after create: ($list_result2.stderr)"}
            }
            let projects_after = $list_result2.stdout | from json
            let new_project = $projects_after.projects | where {|p| $p.title == $project_name} | first
            $new_project.number
        }
    }

    # Create issues and add to project
    for $repo in $repos {
        # Get issue URL (create if doesn't exist)
        mut issue_url = null
        let list_result = do { gh issue list --repo $"($org)/($repo)" --state open --json title,number,url } | complete
        if $list_result.exit_code == 0 {
            let existing_issues = $list_result.stdout | from json
            let matching_issues = $existing_issues | where {|i| $i.title == $title}
            if ($matching_issues | is-not-empty) {
                let existing_issue = $matching_issues | first
                $issue_url = $existing_issue.url
                print $"Issue '($title)' already exists in ($org)/($repo): ($issue_url)"
            }
        }

        if ($issue_url | is-empty) {
            # Create issue
            let label_str = $labels | str join ","
            let issue_result = do { gh issue create --repo $"($org)/($repo)" --title $title --body $template_content } | complete
            if $issue_result.exit_code != 0 {
                print $"Failed to create issue in ($org)/($repo): ($issue_result.stderr)"
                continue
            }
            $issue_url = $issue_result.stdout | str trim
            print $"Created issue in ($org)/($repo): ($issue_url)"
        }

        # Check if issue is already in project
        let item_list_result = do { gh project item-list $project_number --owner $org --format json } | complete
        if $item_list_result.exit_code != 0 {
            print $"Failed to list project items: ($item_list_result.stderr)"
            continue
        }
        let project_items = $item_list_result.stdout | from json
        let current_issue_url = $issue_url

        let url = $current_issue_url | default ""
        if ($url | is-empty) {
            continue
        }

        let is_in_project = $project_items.items | any {|item| $item.content.url == $current_issue_url}
        print $"is_in_project: ($is_in_project)"

        if $is_in_project {
            print $"Issue already in project, skipping add"
        } else {
            # Add to project
            print $"Attempting to add issue to project: ($current_issue_url)"
            let add_result = do { gh project item-add $project_number --owner $org --url $current_issue_url } | complete
            print $"add_result.exit_code: ($add_result.exit_code)"
            print $"add_result.stderr: ($add_result.stderr)"
            if $add_result.exit_code != 0 {
                print $"Failed to add issue to project: ($add_result.stderr)"
            } else {
                print $"Added issue to project"
            }
        }

        # Check assignees and assign to copilot if none
        let issue_view_result = do { gh issue view $url --json assignees } | complete
        if $issue_view_result.exit_code == 0 {
            let issue_data = $issue_view_result.stdout | from json
            if ($issue_data.assignees | is-empty) {
                let issue_number = $url | split row '/' | last
                let assign_result = do { gh issue edit $issue_number --repo $"($org)/($repo)" --add-assignee @copilot } | complete
                if $assign_result.exit_code == 0 {
                    print $"Assigned issue to copilot"
                } else {
                    print $"Failed to assign issue to copilot: ($assign_result.stderr)"
                }
            } else {
                print $"Issue already has assignees, skipping assignment"
            }
        } else {
            print $"Failed to check assignees: ($issue_view_result.stderr)"
        }
    }
}
