#!/usr/bin/env nu

# Orca CLI Tool for Bulk Repository Upgrades
# Creates a GitHub Project from YAML configuration

def main [yaml_file: string] {
    # Load and parse YAML configuration
    let config = try {
        open $yaml_file
    } catch { |err|
        error make {msg: $"Failed to parse YAML file '($yaml_file)': ($err.msg)"}
    }

    print $config
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
    let existing_projects = gh project list --owner $org --format json | from json
    print $existing_projects
    let project_exists = $existing_projects | any {|p| $p.title == $project_name}

    if $project_exists {
        print $"Project '($project_name)' already exists in org '($org)'. Skipping creation."
        return
    }

    # Create the project
    try {
        gh project create $project_name --owner $org
        print $"Successfully created project '($project_name)' in org '($org)'"
    } catch { |err|
        error make {msg: $"Failed to create project: ($err.msg)"}
    }
}
