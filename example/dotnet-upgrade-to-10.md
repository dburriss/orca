# .NET Upgrade: .NET Projects to 10

## Objective
Upgrade an existing C# or F# project from .NET 6 or 8 to .NET 10, ensuring compatibility, updating dependencies, and addressing any breaking changes.

## Role
You are a .NET developer specializing in C# and F# and migration tasks. Your task is to systematically update the codebase to target .NET 10 while maintaining functionality.

## Context
The project is an F# application/library that currently targets .NET 6 or 8. It may use various .NET libraries, NuGet packages, and F# features. The upgrade involves updating the target framework, reviewing and updating package references, and modifying code to handle API changes introduced in newer .NET versions.

## Requirements
- Work in small increments, 1 small step at a time
- Update the target framework in the project file (.csproj or .fsproj) to `net10.0`
- Review and update NuGet package versions to compatible versions for .NET 10
- Address any breaking changes in .NET APIs used in the C# or F# code
- Ensure the project builds and tests pass after the upgrade
- Update any configuration files if necessary

## Instructions
- Run `dotnet build` and `dotnet test` before changing anything to verify everything is working before changes
- Calculate the dependency order and upgrade 1 project at a time
- Update only the csproj or fsproj files to net10.0 and then run `dotnet clean` and then `dotnet build`
- If build fails fix and then repeat until build succeeds
- Run the tests and fix any changes needed in the project files or configuration. DO NOT CHANGE ANY TESTS OR LOGIC TO MAKE TESTS PASS.
- Update documentation if it references specific versions

## Examples of Updates Needed

### 1. Project File Updates
```xml
<!-- Before -->
<TargetFramework>net6.0</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

### 2. Package Reference Updates
- Update package versions only when needed for the .NET upgrade

### 3. Code Changes for Breaking Changes
- Update usage of deprecated APIs (e.g., `System.Web` related code if any)
- Handle changes in async/await patterns if applicable
- Update F# specific features if new versions introduce changes

### 4. Configuration Updates
- Update `global.json` if present to specify .NET 10 SDK
- Review and update any runtime configuration files

## Acceptance Criteria
- Project targets .NET 10 successfully
- All dependencies are updated to compatible versions
- Code compiles without errors
- Tests pass
- No deprecated API warnings remain
