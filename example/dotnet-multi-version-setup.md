# Create Multi-Version .NET Setup Copilot Workflow

## Objective
Create a GitHub Actions workflow file that sets up a multi-version .NET environment for GitHub Copilot to work with .NET 6, 8, and 10 projects.

## Role
You are a DevOps engineer specializing in GitHub Actions and .NET CI/CD pipelines. Your task is to create a workflow that properly configures multiple .NET SDK versions.

## Context
The workflow needs to be named `copilot-setup-steps.yml` and placed in `.github/workflows/`. It will automatically run when changed to validate the setup, and can be manually triggered through the Actions tab. The job must be named `copilot-setup-steps` to be recognized by Copilot. Docs: https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment

## Requirements
- Create a GitHub Actions workflow that sets up .NET 6.0, 8.0, and 10.0 SDKs
- Use the `actions/setup-dotnet@v4` action for each version
- Include proper permissions for repository access
- Add steps to checkout code and restore global tools
- Ensure the workflow triggers on push, pull request, and manual dispatch

## Instructions
Create a workflow file with the following structure:
- Name: "Copilot Setup Steps for Multi-Version .NET"
- Triggers: workflow_dispatch, push, and pull_request on the workflow file path
- Job name: copilot-setup-steps
- Runner: ubuntu-latest
- Permissions: contents: read
- Steps: checkout, setup .NET 6.0, setup .NET 8.0, setup .NET 10.0, setup global tools

## Expected Output
The workflow should create a `.github/workflows/copilot-setup-steps.yml` file that:
1. Sets up all three .NET SDK versions in sequence
2. Uses proper GitHub Actions syntax
3. Includes comments explaining each section
4. Restores any global tools defined in global.json

## Acceptance Criteria
- Workflow file is properly formatted YAML
- All three .NET versions are configured correctly
- Job is named `copilot-setup-steps`
- Workflow triggers on appropriate events
- Permissions are set to minimum required
- Global tools restoration step is included
