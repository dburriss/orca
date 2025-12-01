# Assign Issue to Copilot

## Summary
After an issue has been created and added to a project, assign it to copilot. This assignment should be idempotent, only assigning if no one is already assigned.

## Motivation
To automate the assignment of upgrade issues to Copilot for initial PR generation, ensuring consistent ownership and workflow initiation.

## Impact
- Modifies the issue creation flow to include assignment
- Adds idempotency check for existing assignees
- Uses gh issue edit command for assignment

## Implementation Approach
Extend the existing issue creation loop to check current assignees and assign to copilot if none exist, after adding to project.</content>
<parameter name="filePath">openspec/changes/assign-issue-to-copilot/proposal.md