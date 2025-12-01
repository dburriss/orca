# create-github-project Specification Delta

## MODIFIED Requirements

### Requirement: Assign Issue to Copilot

The tool SHALL assign each created issue to copilot after adding it to the project, but only if no assignees are already present.

#### Scenario: Issue has no assignees

Given an issue has been created and added to the project,

When no assignees are present,

Then the tool assigns the issue to copilot.

#### Scenario: Issue already has assignees

Given an issue has been created and added to the project,

When assignees are already present,

Then the tool does not assign the issue to copilot.</content>
<parameter name="filePath">openspec/changes/assign-issue-to-copilot/specs/create-github-project/spec.md