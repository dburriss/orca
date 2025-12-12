# Add AGENTS.md to Repository

## Objective
Add an AGENTS.md file to a repository that provides AI assistants with essential information about the project structure, tech stack, and build/test instructions.

## Role
You are a developer creating documentation for AI assistants. Your task is to analyze the repository and create a concise AGENTS.md file that helps AI assistants work effectively with the codebase.

## Context
The repository may or may not already have an AGENTS.md file. If it exists, this migration should not be applied. The file should provide essential information without creating a high maintenance burden.

## Requirements
- Check if AGENTS.md already exists - if so, skip this migration
- Analyze the repository structure and tech stack
- Create AGENTS.md with General instructions, tech stack, and build/test instructions
- Include brief repository overview and structure
- Keep information concise and minimize maintenance burden
- Adapt instructions based on the actual project 

## Instructions
- First check if AGENTS.md already exists in the repository root
- If it exists, exit with a message that the file already exists
- Analyze the repository to understand:
  - Programming language(s) and frameworks
  - Build system and commands
  - Test framework and commands
  - Project structure
- Create AGENTS.md with the following sections:
  - General (coding principles and approach)
  - Tech stack (key technologies and tools)
  - Build and test instructions
  - Repository overview (brief description and structure)
- Keep descriptions concise and focus on what AI assistants need to know
- Avoid overly detailed information that would require frequent updates

## Examples of AGENTS.md Structure

```markdown
# General

- Prefer simple solutions
- Ask if unsure
- Keep answers concise
- Break solutions into small incremental steps
- Do one step at a time

# Tech stack

- .NET 8
- C# / F#
- Entity Framework Core
- xUnit for testing

# Build and test

- Build: `dotnet build`
- Test: `dotnet test`
- Clean: `dotnet clean`

# Structure

This is a .NET web application with the following structure:
- `src/` - Main source code
- `tests/` - Test projects
- `docs/` - Documentation
```


## Acceptance Criteria
- AGENTS.md file created in repository root
- File contains all required sections
- Information is accurate based on repository analysis
- File is concise and maintainable
- Migration is skipped if AGENTS.md already exists
