## 1. Implementation
- [x] 1.1 Update YAML parsing to extract repos list from 'repos' key
- [x] 1.2 Update YAML parsing to extract issue config (template path, labels) from 'issue' key
- [x] 1.3 Add logic to read template file content using path relative to YAML file
- [x] 1.4 Use project name as issue title
- [x] 1.5 Modify project creation to capture and return project ID
- [x] 1.6 Add loop to create issues for each repository using gh issue create
- [x] 1.7 Add logic to add each created issue to the project using gh project item-add
- [x] 1.8 Add error handling for issue creation and project addition failures

## 2. Testing
- [x] 2.1 Test with sample YAML to verify issues are created and added to project
- [x] 2.2 Test error cases (invalid repo, permissions, etc.)
- [x] 2.3 Verify idempotency (don't create duplicates if run again)
