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

    let project_id = if $project_exists {
        let project = $existing_projects.projects | where {|p| $p.title == $project_name} | first
        print $"Project '($project_name)' already exists in org '($org)'. Using existing project."
        $project.id
    } else {
        # Create the project
        let create_result = do { gh project create --title $project_name --owner $org --format json } | complete
        if $create_result.exit_code != 0 {
            error make {msg: $"Failed to create project: ($create_result.stderr)"}
        } else {
            print $"Successfully created project '($project_name)' in org '($org)'"
            let project_data = $create_result.stdout | from json
            $project_data.id
        }
    }

    # Create issues and add to project
    for $repo in $repos {
        # Check if issue already exists
        let list_result = do { gh issue list --repo $"($org)/($repo)" --state open --json title } | complete
        if $list_result.exit_code == 0 {
            let existing_issues = $list_result.stdout | from json
            if ($existing_issues | any {|i| $i.title == $title}) {
                print $"Issue '($title)' already exists in ($org)/($repo), skipping"
                continue
            }
        }

        # Create issue
        let label_str = $labels | str join ","
        let issue_result = do { gh issue create --repo $"($org)/($repo)" --title $title --body $template_content --label $label_str } | complete
        if $issue_result.exit_code != 0 {
            print $"Failed to create issue in ($org)/($repo): ($issue_result.stderr)"
            continue
        }

        let issue_url = $issue_result.stdout | str trim
        print $"Created issue in ($org)/($repo): ($issue_url)"

        # Add to project
        let add_result = do { gh project item-add --project-id $project_id --url $issue_url } | complete
        if $add_result.exit_code != 0 {
            print $"Failed to add issue to project: ($add_result.stderr)"
        } else {
            print $"Added issue to project"
        }
    }
}
