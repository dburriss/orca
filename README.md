# Orca Nushell Scripts

This repository contains Nushell scripts for managing GitHub projects and issues in bulk.

## Prerequisites

- **Nushell**: Install Nushell from [nushell.sh](https://www.nushell.sh/)
- **GitHub CLI**: Install the GitHub CLI from [cli.github.com](https://cli.github.com/)

## Authentication

### Local Environment
Run `gh auth login` to authenticate with GitHub CLI.

### CI Environment
Set the `GH_TOKEN` environment variable with a GitHub Personal Access Token that has the necessary permissions (project and issue management).

## Scripts

### orca.nu

Creates a GitHub project and adds issues to it across multiple repositories.

**Usage:**
```
./orca.nu [--verbose] <yaml_file>
```

**Options:**
- `--verbose`: Enable verbose output
- `yaml_file`: Path to YAML configuration file

**YAML Configuration Structure:**
```yaml
job:
  title: "Project Title"
  org: "organization-name"
repos:
  - "repo1"
  - "repo2"
issue:
  template: "path/to/issue-template.md"
  labels:
    - "label1"
    - "label2"
```

### cleanup.nu

Deletes a GitHub project and cleans up associated issues and PRs.

**Usage:**
```
./cleanup.nu [--dryrun] <yaml_file>
```

**Options:**
- `--dryrun`: Preview what would be deleted without actually deleting
- `yaml_file`: Path to YAML configuration file

**YAML Configuration Structure:**
```yaml
job:
  title: "Project Title"
  org: "organization-name"
```