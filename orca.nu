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

    # Extract project configuration
    let project = $config | get project
    let project_name = $project | get name
    let org = $project | get org

    # Validate inputs
    if ($project_name | is-empty) {
        error make {msg: "Project name is required in YAML"}
    }
    if ($org | is-empty) {
        error make {msg: "Organization is required in YAML"}
    }

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

    if $project_exists {
        print $"Project '($project_name)' already exists in org '($org)'. Skipping creation."
        return
    }

    # Create the project
    let create_result = do { gh project create $project_name --owner $org } | complete
    if $create_result.exit_code != 0 {
        error make {msg: $"Failed to create project: ($create_result.stderr)"}
    } else {
        print $"Successfully created project '($project_name)' in org '($org)'"
    }
}
