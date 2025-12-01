# create-github-project Specification

## Purpose
TBD - created by archiving change create-github-project. Update Purpose after archive.
## Requirements
### Requirement: Parse Project Configuration from YAML

The tool SHALL parse the input YAML file and extract the project title, organization, repositories list, and issue configuration from the 'job' and 'issue' sections.

#### Scenario: Valid YAML with job and issue sections

Given a YAML file with:
```
job:
  title: "test-project"
  org: "test-org"

repos:
  - "repo1"
  - "repo2"

issue:
  template: "./template.md"
  labels: ["label1", "label2"]
```

When the tool parses the file,

Then it extracts title as "test-project", org as "test-org", repos as ["repo1", "repo2"], template as "./template.md", labels as ["label1", "label2"].

### Requirement: Create Empty GitHub Project

The tool SHALL create an empty GitHub Project using the gh CLI with the extracted name and org.

#### Scenario: Successful project creation

Given valid project name and org,

When the tool runs `gh project create`,

Then an empty project is created in the specified organization.

### Requirement: Handle Creation Errors

The tool SHALL handle errors from the gh command, such as permissions or rate limits, and provide appropriate feedback.

#### Scenario: Permission denied

Given insufficient permissions,

When attempting to create the project,

Then the tool reports the error and does not proceed.

### Requirement: Read Issue Template

The tool SHALL read the issue template file specified in the YAML, using a path relative to the YAML file location.

#### Scenario: Template file exists

Given a valid template path relative to YAML,

When the tool reads the template,

Then it loads the file content as the issue body.

### Requirement: Use Project Title as Issue Title

The tool SHALL use the project title from the YAML as the issue title.

#### Scenario: Project title set

Given project title "Upgrade Project",

When creating issues,

Then the issue title is "Upgrade Project".

### Requirement: Create Issues in Repositories

The tool SHALL create an issue in each specified repository using the extracted title, template content as body, and specified labels.

#### Scenario: Successful issue creation

Given valid repositories and template,

When creating issues,

Then one issue is created per repository with correct title, body, and labels.

### Requirement: Add Issues to Project

The tool SHALL add each created issue to the GitHub project.

#### Scenario: Issues added to project

Given created issues and project ID,

When adding to project,

Then all issues are linked to the project.

### Requirement: Handle Issue Creation Errors

The tool SHALL handle errors during issue creation, such as repository access issues or API limits, and provide appropriate feedback.

#### Scenario: Repository not found

Given an invalid repository name,

When attempting to create issue,

Then the tool reports the error and continues with other repositories.

