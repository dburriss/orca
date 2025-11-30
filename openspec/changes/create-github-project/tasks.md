1. Implement YAML parsing to extract project name and organization from the input YAML file.
2. Create a Nushell function that calls `gh project create` with the extracted name and org.
3. Add validation to ensure project name and org are present and valid in the YAML.
4. Implement error handling for gh command failures, including rate limits and permissions.
5. Ensure idempotency: check if project already exists before creating.
6. Write unit tests for the project creation logic.
7. Integrate project creation as the first step in the main CLI command.
8. Manually test with the example dotnet-migration.yml file.