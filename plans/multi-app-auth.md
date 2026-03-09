# Multi-App Auth & Named PEM Files

## Summary

Support multiple named GitHub App profiles in `auth.json`. Each profile stores its own credentials and PEM file named after the app. An `"active"` key controls which profile is used. A new `orca auth switch <profile>` command lets users change the active profile.

## Motivation

Currently `auth.json` holds a single credential (PAT or App) and the PEM is always saved as `~/.config/orca/app.pem`. This makes it impossible to manage more than one GitHub App with the CLI without manually overwriting the config each time.

---

## New `auth.json` Schema

```json
{
  "active": "my-app",
  "profiles": {
    "my-app": {
      "type": "app",
      "appId": "123",
      "keyPath": "~/.config/orca/my-app.pem",
      "installationId": "456789"
    },
    "pat": {
      "type": "pat",
      "token": "ghp_..."
    }
  }
}
```

- `"active"` — name of the currently selected profile.
- `"profiles"` — map of profile name → credential entry.
- PAT lives under a profile named `"pat"` by default.
- App profiles are named after the app (see Profile Naming below).

---

## Profile Naming

| Command | Profile name | PEM filename |
|---|---|---|
| `orca auth create-app --app-name foo` | `"foo"` | `foo.pem` |
| `orca auth create-app --org acme` | `"orca-acme-gh-app"` | `orca-acme-gh-app.pem` |
| `orca auth app --app-id 12345 ...` | `"12345"` | *(user-provided via `--key`, no PEM written)* |
| `orca auth pat --token ghp_...` | `"pat"` | *(no PEM)* |

Profile names are auto-derived from existing arguments — no new `--name` flag is required.

---

## New Command

```
orca auth switch <profile-name>
```

Sets `"active"` in `auth.json` to the given profile name. Errors if the profile does not exist. Prints the new active profile on success.

---

## File-by-File Changes

### `src/Orca.Auth/AuthConfig.fs`

- Replace the flat `AuthConfigFile` record with:
  - `ProfileEntry` — per-profile record (`type`, `token`, `appId`, `keyPath`, `installationId` — all optional except `type`)
  - `AuthConfigFile` — `{ Active: string; Profiles: Map<string, ProfileEntry> }`
- Update `writeConfigTo`/`writeConfig` and `readConfigFrom`/`readConfig` for the new shape.
- Add helpers:
  - `getActiveProfile cfg` — returns the active `ProfileEntry` or an error.
  - `upsertProfile name profile cfg` — adds/replaces a profile and sets it as `active`.
  - `switchActive name cfg` — changes `active`; errors if `name` not in `profiles`.
  - `removeOldPem ()` — deletes `~/.config/orca/app.pem` if present (migration helper).

### `src/Orca.Auth/AppAuth.fs`

- `storeConfig` / `storeConfigTo`: add `profileName: string` parameter. Read existing config, upsert the profile, write back.
- `loadConfig` / `loadConfigFrom`: read the active profile, validate `type=app`, map to `AppAuthConfig`.
- `resolveConfigWith` / `resolveConfig`: unchanged in signature.

### `src/Orca.Auth/PatAuth.fs`

- `storeToken`: write into a profile named `"pat"`, set as active.
- `loadToken` / `loadTokenWith`: read the active profile, validate `type=pat`, return token.

### `src/Orca.Auth/CreateAppCommand.fs`

- `pemPath (appName: string)` — `~/.config/orca/<appName>.pem`.
- `savePem (appName: string) (pem: string)` — uses named path.
- `storeAppCredentials (appName: string) (appId: string) (keyPath: string)` — profile name = `appName`.
- `execute` — passes `input.AppName` through to the above; calls `removeOldPem ()` after saving.

### `src/Orca.Tool/Args.fs`

Add `AuthSwitchArgs` and `Switch` subcommand to `AuthArgs`:

```fsharp
[<CliPrefix(CliPrefix.DoubleDash)>]
type AuthSwitchArgs =
    | [<MainCommand; Mandatory>] Profile of name: string

type AuthArgs =
    | ...existing...
    | [<CliPrefix(CliPrefix.None); SubCommand>] Switch of ParseResults<AuthSwitchArgs>
```

### `src/Orca.Tool/Program.fs`

- `orca auth app` handler: derive profile name from `--app-id`; pass to `storeConfig`.
- Add `orca auth switch` handler:
  1. Read `auth.json`.
  2. Call `switchActive profileName cfg`.
  3. Write updated config.
  4. Print `Active profile: <name>`.
- `resolveAuthContextWith`: no change (already delegates to `loadConfig` which reads the active profile).

---

## Test Changes

### `tests/Orca.Auth.Tests/AppAuthTests.fs`

- Update `storeConfigTo`/`loadConfigFrom` round-trip tests to pass a profile name.
- Add: `loadConfigFrom returns active profile config`.
- Add: `loadConfigFrom returns error when active profile does not exist`.
- Update fixture `auth.json` strings to use new schema.

### `tests/Orca.Auth.Tests/PatAuthTests.fs`

- Update fixture `auth.json` strings to use new `profiles`/`active` schema.

### `tests/Orca.Auth.Tests/AuthConfigTests.fs` *(new file)*

- `upsertProfile adds a new profile and sets active`
- `upsertProfile replaces an existing profile`
- `switchActive succeeds when profile exists`
- `switchActive returns error when profile does not exist`
- `getActiveProfile returns correct profile`
- `getActiveProfile returns error when active key points to missing profile`
- `removeOldPem deletes app.pem when it exists`
- `removeOldPem is a no-op when app.pem is absent`

---

## Migration

On first run after upgrading:

1. `removeOldPem ()` is called during `create-app` / `auth app`, silently deleting `~/.config/orca/app.pem` if present.
2. Existing `auth.json` (old flat format) will fail to deserialise into the new schema. The existing error path already falls through to env vars and then `gh` CLI ambient auth.
3. Users with a stored app config will need to re-run `orca auth app` or `orca auth create-app` once.

No automatic migration of the old config is performed.

---

## Out of Scope

- Listing all profiles (`orca auth list`) — can be added as a follow-up.
- Deleting a profile (`orca auth remove <profile>`) — can be added as a follow-up.
- Per-command `--profile` flag override — can be added as a follow-up.
