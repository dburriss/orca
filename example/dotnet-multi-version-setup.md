# Create Multi-Version .NET Setup Copilot Workflow

## Objective
Create a GitHub Actions workflow file that sets up a multi-version .NET environment for GitHub Copilot to work with .NET 6, 8, and 10 projects using best practices for security, performance, and maintainability.

## Role
You are a DevOps engineer specializing in GitHub Actions and .NET CI/CD pipelines. Your task is to create a workflow that properly configures multiple .NET SDK versions following documented best practices.

## Context
The workflow needs to be named `copilot-setup-steps.yml` and placed in `.github/workflows/`. It will automatically run when changed to validate the setup, and can be manually triggered through the Actions tab. The job must be named `copilot-setup-steps` to be recognized by Copilot. Docs: https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment

## Requirements
- Create a GitHub Actions workflow that sets up .NET 6.0, 8.0, and 10.0 SDKs in a single step using multi-version syntax
- Use specific version numbers for reproducibility (not wildcards)
- Include NuGet package caching with lock files for performance
- Add .NET environment variables setup
- Install dotnet-format tool for code formatting
- Use minimum required permissions for security
- Add proper validation and build steps
- Enable package lock files for deterministic restores

## Instructions
- If `./gitub/workflows/copilot-setup-steps.yml` already exists, do not continue.
Create a workflow file with the following structure:
- Name: "Copilot Setup Steps for Multi-Version .NET"
- Triggers: workflow_dispatch, push, and pull_request on the workflow file path
- Job name: copilot-setup-steps
- Runner: ubuntu-latest
- Permissions: contents: read (minimum required)
- Steps: checkout with full history, setup .NET SDKs (multi-version with caching), restore dependencies with --locked-mode, install dotnet-format, set environment variables, build validation

## Project File Updates Required
For the caching to work correctly, each .NET project file (.csproj or .fsproj) must include:
```xml
<PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

And commit the generated packages.lock.json files to source control.

## Expected Output
The workflow should create a `.github/workflows/copilot-setup-steps.yml` file that:
1. Sets up all three .NET SDK versions in a single step using proper multi-version syntax
2. Uses specific version numbers (e.g., 6.0.428, 8.0.416, 10.0.101)
3. Includes NuGet package caching with lock files and cache-dependency-path for performance optimization
4. Installs dotnet-format tool globally
5. Sets up .NET environment variables (DOTNET_ROOT, DOTNET_CLI_TELEMETRY_OPTOUT)
6. Includes build validation to ensure setup works correctly
7. Uses proper GitHub Actions syntax with comments explaining each section
8. Follows security best practices with minimal permissions
9. Uses --locked-mode for dotnet restore to ensure exact package versions

## Acceptance Criteria
- If an existing `./gitub/workflows/copilot-setup-steps.yml` exists, it is left untouched and no other files are changed
- Workflow file is properly formatted YAML
- All three .NET versions are configured in a single setup step using multi-version syntax
- Specific version numbers are used (not wildcards)
- NuGet caching is implemented with cache: true and cache-dependency-path: "**/packages.lock.json"
- dotnet-format tool installation step is included
- Environment variables for .NET are configured
- Build validation step is included to verify setup
- Job is named `copilot-setup-steps`
- Workflow triggers on appropriate events
- Permissions are set to minimum required (contents: read)
- Comments explain each section and security considerations
- Follows best practices for reproducibility and performance
- Uses --locked-mode for dotnet restore to ensure deterministic package versions
