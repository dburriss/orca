## 1. Implementation
- [ ] 1.1 Update YAML parsing to extract repos list from 'repos' key
- [ ] 1.2 Update YAML parsing to extract issue config (template path, labels) from 'issue' key
- [ ] 1.3 Add logic to calculate path relative to YAML file
- [ ] 1.4 Add logic to check existence of template
- [ ] 1.5 Modify project creation to capture and return project ID
- [ ] 1.6 Add loop to create issues for each repository using gh issue create
- [ ] 1.7 Add logic to add each created issue to the project using gh project item-add
- [ ] 1.8 Add error handling for issue creation and project addition failures

## 2. Testing
- [ ] 2.1 Test with sample YAML to verify issues are created and added to project (add dryrun functionality)
- [ ] 2.2 Test error cases (invalid repo, permissions, etc.)
- [ ] 2.3 Verify idempotency (don't create duplicates if run again)
