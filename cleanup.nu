#!/usr/bin/env nu

# Cleanup script for deleting GitHub projects from YAML configuration
# Run with --dryrun to see what would be deleted without actually deleting

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

def main [--dryrun, yaml_file: string] {
    # Ensure authentication is set up
    ensure_auth

    # Load and parse YAML configuration
    let config = try {
        open $yaml_file
    } catch { |err|
        error make {msg: $"Failed to parse YAML file '($yaml_file)': ($err.msg)"}
    }

    # Extract job configuration
    let job = $config | get job
    let project_name = $job | get title
    let org = $job | get org

    # Validate inputs
    if ($project_name | is-empty) {
        error make {msg: "Project name is required in YAML"}
    }
    if ($org | is-empty) {
        error make {msg: "Organization is required in YAML"}
    }

    # Find the project to delete
    let list_result = do { gh project list --owner $org --format json } | complete
    if $list_result.exit_code != 0 {
        error make {msg: $"Failed to list projects: ($list_result.stderr)"}
    }
    let existing_projects = $list_result.stdout | from json
    let matches = $existing_projects.projects | where {|p| $p.title == $project_name}

    if ($matches | is-empty) {
        print $"Project '($project_name)' not found in org '($org)'. Nothing to delete."
        return
    }

    let project_to_delete = $matches | first
    let project_number = $project_to_delete.number

    # Get issues in the project
    let items_result = do { gh project item-list --owner $org --number $project_number --format json } | complete
    if $items_result.exit_code != 0 {
        error make {msg: $"Failed to list project items: ($items_result.stderr)"}
    }
    let items = $items_result.stdout | from json
    let issues = $items.items | where {|i| $i.type == "Issue"}

    for issue in $issues {
        let repo = $issue.content.repository.nameWithOwner
        let issue_number = $issue.content.number

        # Find PRs mentioning this issue
        let prs_result = do { gh pr list --repo $repo --search $"is:pr mentions:#($issue_number)" --json number } | complete
        if $prs_result.exit_code != 0 {
            error make {msg: $"Failed to list PRs for issue ($issue_number): ($prs_result.stderr)"}
        }
        let prs = $prs_result.stdout | from json

        for pr in $prs {
            if $dryrun {
                print $"DRY RUN: Would close PR ($pr.number) in repo ($repo) for issue #($issue_number)"
            } else {
                let close_result = do { gh pr close $pr.number --repo $repo } | complete
                if $close_result.exit_code != 0 {
                    error make {msg: $"Failed to close PR ($pr.number): ($close_result.stderr)"}
                } else {
                    print $"Closed PR ($pr.number) in repo ($repo)"
                }
            }
        }

        # Delete the issue
        if $dryrun {
            print $"DRY RUN: Would delete issue #($issue_number) in repo ($repo)"
        } else {
            let delete_result = do { gh issue delete $issue_number --repo $repo } | complete
            if $delete_result.exit_code != 0 {
                error make {msg: $"Failed to delete issue ($issue_number): ($delete_result.stderr)"}
            } else {
                print $"Deleted issue #($issue_number) in repo ($repo)"
            }
        }
    }

    if $dryrun {
        print $"DRY RUN: Would delete project '($project_name)' with number ($project_number) from org '($org)'"
    } else {
        # Delete the project
        let delete_result = do { gh project delete $project_number --owner $org } | complete
        if $delete_result.exit_code != 0 {
            error make {msg: $"Failed to delete project: ($delete_result.stderr)"}
        } else {
            print $"Successfully deleted project '($project_name)' with number ($project_number) from org '($org)'"
        }
    }
}