## ADDED Requirements

### Requirement: Parse Project Configuration from YAML

The tool SHALL parse the input YAML file and extract the project name and organization from the 'project' section.

#### Scenario: Valid YAML with project section

Given a YAML file with:
```
project:
  name: "test-project"
  org: "test-org"
```

When the tool parses the file,

Then it extracts name as "test-project" and org as "test-org".

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