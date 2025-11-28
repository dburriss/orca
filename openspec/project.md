# Project Context

## Purpose
This project delivers a lightweight CLI (Nushell script) tool that orchestrates bulk upgrade jobs across multiple GitHub repositories by creating a tracking project, generating standardized issues from a template, and invoking Copilot to produce initial pull requests for each repository. It focuses on simple, repeatable automation with minimal state, basic idempotency, and human-in-the-loop review, enabling teams to execute large migrations efficiently without building a full workflow engine.

## Tech Stack
- Nushell
- `gh` CLI
- `copilot` CLI

## Project Conventions

### Code Style
- Use snake_case for variable and function names
- Indent with 4 spaces
- Add comments for complex logic
- Follow Nushell best practices for script structure

### Architecture Patterns
- Modular scripts with reusable functions
- Command-line interface with subcommands
- Idempotent operations to allow safe retries
- Minimal state management using temporary files or environment variables

### Testing Strategy
- Manual testing of CLI commands
- Test on sample repositories before production use
- Validate GitHub API responses and error handling

### Git Workflow
- Use main branch for stable releases
- Feature branches for development
- Commit messages: "feat: add feature", "fix: resolve issue", "docs: update documentation"

## Domain Context
- GitHub repository management and automation
- Bulk operations across multiple repositories
- Pull request creation and issue tracking
- Copilot-assisted code generation for upgrades

## Important Constraints
- Human-in-the-loop review required for all changes
- Minimal state to avoid complexity
- Respect GitHub API rate limits
- Ensure idempotency for safe retries

## External Dependencies
- GitHub API (via gh CLI)
- GitHub Copilot CLI for code generation
- Nushell runtime environment
