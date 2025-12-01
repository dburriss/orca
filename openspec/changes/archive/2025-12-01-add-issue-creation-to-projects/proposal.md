# Change: Add Issue Creation to Projects

## Why
The current tool only creates empty GitHub projects. To enable bulk upgrade workflows, we need to create tracking issues in each repository using a standardized template and add them to the project for centralized management.

## What Changes
- Parse repositories list from YAML configuration
- Parse issue template path and labels from YAML
- Read issue template content from file (path relative to YAML)
- Use YAML job title as the issue title
- Create an issue in each repository using the template
- Add each created issue to the GitHub project

## Impact
- Affected specs: create-github-project
- Affected code: orca.nu script
- No breaking changes to existing functionality
