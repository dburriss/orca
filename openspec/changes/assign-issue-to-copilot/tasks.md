# Tasks for Assign Issue to Copilot

1. Update orca.nu script to check issue assignees after adding to project
2. Add logic to assign issue to copilot only if no assignees exist
3. Use gh issue edit --assignee copilot command for assignment
4. Test idempotency: ensure no assignment if already assigned
5. Validate with openspec validate assign-issue-to-copilot --strict</content>
<parameter name="filePath">openspec/changes/assign-issue-to-copilot/tasks.md