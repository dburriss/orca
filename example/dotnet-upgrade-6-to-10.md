# .NET Upgrade: F# Project from 6 to 10

## Objective
Upgrade an existing F# project from .NET 6 to .NET 10, ensuring compatibility, updating dependencies, and addressing any breaking changes.

## Role
You are a .NET developer specializing in F# and migration tasks. Your task is to systematically update the codebase to target .NET 10 while maintaining functionality.

## Context
The project is an F# application/library that currently targets .NET 6. It may use various .NET libraries, NuGet packages, and F# features. The upgrade involves updating the target framework, reviewing and updating package references, and modifying code to handle API changes introduced in newer .NET versions.

## Requirements
- Update the target framework in the project file (.fsproj) from `net6.0` to `net10.0`
- Review and update NuGet package versions to compatible versions for .NET 10
- Address any breaking changes in .NET APIs used in the F# code
- Ensure the project builds and tests pass after the upgrade
- Update any configuration files if necessary

## Examples of Updates Needed

### 1. Project File Updates
```xml
<!-- Before -->
<TargetFramework>net6.0</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

### 2. Package Reference Updates
```xml
<!-- Before -->
<PackageReference Include="Some.Package" Version="6.0.0" />

<!-- After -->
<PackageReference Include="Some.Package" Version="10.0.0" />
```

### 3. Code Changes for Breaking Changes
- Update usage of deprecated APIs (e.g., `System.Web` related code if any)
- Handle changes in async/await patterns if applicable
- Update F# specific features if new versions introduce changes

### 4. Configuration Updates
- Update `global.json` if present to specify .NET 10 SDK
- Review and update any runtime configuration files

## Steps to Follow
1. Analyze the current project structure and dependencies
2. Update the target framework in .fsproj files
3. Update NuGet packages to versions compatible with .NET 10
4. Review code for deprecated API usage and update accordingly
5. Build the project and resolve any compilation errors
6. Run tests and fix any failing tests
7. Update documentation if necessary

## Acceptance Criteria
- Project targets .NET 10 successfully
- All dependencies are updated to compatible versions
- Code compiles without errors
- Tests pass
- No deprecated API warnings remain