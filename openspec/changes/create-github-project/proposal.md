# Create GitHub Project from YAML

## Why

The CLI tool needs to create a tracking project for bulk upgrade jobs across repositories, as per the project purpose.

## What

Implement the initial step of creating an empty GitHub Project based on the project configuration in the input YAML file.

## How

- Parse the YAML file to extract the project name and organization.
- Use the `gh project create` command to create an empty project.
- Ensure idempotency and error handling.

## Impact

This adds the foundational capability for project creation, enabling subsequent steps like issue generation and PR creation.