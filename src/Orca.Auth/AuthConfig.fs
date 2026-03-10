module Orca.Auth.AuthConfig

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

// ---------------------------------------------------------------------------
// Shared on-disk config for both PAT and GitHub App auth.
//
// Location: ~/.config/orca/auth.json
//
// Schema:
//   {
//     "active": "my-app",
//     "profiles": {
//       "my-app": { "type": "app", "appId": "123", "keyPath": "/path/to/my-app.pem", "installationId": "456" },
//       "pat":    { "type": "pat", "token": "ghp_..." }
//     }
//   }
// ---------------------------------------------------------------------------

/// A single named credential entry inside the profiles map.
[<CLIMutable>]
type ProfileEntry =
    { [<JsonPropertyName("type")>]           Type           : string
      // PAT fields
      [<JsonPropertyName("token")>]          Token          : string option
      // App fields
      [<JsonPropertyName("appId")>]          AppId          : string option
      [<JsonPropertyName("keyPath")>]        KeyPath        : string option
      [<JsonPropertyName("installationId")>] InstallationId : string option }

/// The top-level on-disk config file.
[<CLIMutable>]
type AuthConfigFile =
    { [<JsonPropertyName("active")>]   Active   : string
      [<JsonPropertyName("profiles")>] Profiles : System.Collections.Generic.Dictionary<string, ProfileEntry> }

let private configPath () =
    let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    Path.Combine(home, ".config", "orca", "auth.json")

/// Derive the auth config file path for a given home directory.
/// Pure: no I/O.
let configPathFor (homeDir: string) : string =
    Path.Combine(homeDir, ".config", "orca", "auth.json")

let private jsonOptions =
    let opts = JsonSerializerOptions()
    opts.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    opts.WriteIndented <- true
    opts

// ---------------------------------------------------------------------------
// Read / write
// ---------------------------------------------------------------------------

/// Write a config record to disk at a given home directory, creating directories as needed.
let writeConfigTo (homeDir: string) (cfg: AuthConfigFile) : Result<unit, string> =
    try
        let path = configPathFor homeDir
        let dir  = Path.GetDirectoryName(path) |> Option.ofObj |> Option.defaultValue "."
        Directory.CreateDirectory(dir) |> ignore
        let json = JsonSerializer.Serialize(cfg, jsonOptions)
        File.WriteAllText(path, json)
        Ok ()
    with ex ->
        Error $"Failed to write auth config: {ex.Message}"

/// Write a config record to disk, creating directories as needed.
let writeConfig (cfg: AuthConfigFile) : Result<unit, string> =
    try
        let path = configPath ()
        let dir  = Path.GetDirectoryName(path) |> Option.ofObj |> Option.defaultValue "."
        Directory.CreateDirectory(dir) |> ignore
        let json = JsonSerializer.Serialize(cfg, jsonOptions)
        File.WriteAllText(path, json)
        Ok ()
    with ex ->
        Error $"Failed to write auth config: {ex.Message}"

/// Read the config record from a given home directory.
let readConfigFrom (homeDir: string) : Result<AuthConfigFile, string> =
    try
        let path = configPathFor homeDir
        if not (File.Exists(path)) then
            Error "No auth config found. Run 'orca auth pat --token <tok>' or 'orca auth app ...' first."
        else
            let json = File.ReadAllText(path)
            match JsonSerializer.Deserialize<AuthConfigFile>(json, jsonOptions) |> Option.ofObj with
            | None     -> Error "Auth config file is empty or malformed."
            | Some cfg -> Ok cfg
    with ex ->
        Error $"Failed to read auth config: {ex.Message}"

/// Read the config record from disk.
let readConfig () : Result<AuthConfigFile, string> =
    try
        let path = configPath ()
        if not (File.Exists(path)) then
            Error "No auth config found. Run 'orca auth pat --token <tok>' or 'orca auth app ...' first."
        else
            let json = File.ReadAllText(path)
            match JsonSerializer.Deserialize<AuthConfigFile>(json, jsonOptions) |> Option.ofObj with
            | None     -> Error "Auth config file is empty or malformed."
            | Some cfg -> Ok cfg
    with ex ->
        Error $"Failed to read auth config: {ex.Message}"

// ---------------------------------------------------------------------------
// Profile helpers
// ---------------------------------------------------------------------------

/// Return the active profile entry, or an error if the active key is missing
/// or points to a profile that does not exist.
let getActiveProfile (cfg: AuthConfigFile) : Result<ProfileEntry, string> =
    if String.IsNullOrWhiteSpace(cfg.Active) then
        Error "No active profile set in auth config."
    else
        match cfg.Profiles.TryGetValue(cfg.Active) with
        | true, profile -> Ok profile
        | _             -> Error $"Active profile '{cfg.Active}' not found in auth config."

/// Return an empty config with no profiles and no active profile.
let private emptyConfig () : AuthConfigFile =
    { Active   = ""
      Profiles = System.Collections.Generic.Dictionary<string, ProfileEntry>() }

/// Add or replace a named profile and set it as the active profile.
/// Reads the existing config first (or starts fresh if none exists) then writes back.
let upsertProfile (name: string) (profile: ProfileEntry) (cfg: AuthConfigFile) : AuthConfigFile =
    cfg.Profiles.[name] <- profile
    { cfg with Active = name }

/// Change the active profile to `name`. Returns an error if the profile does not exist.
let switchActive (name: string) (cfg: AuthConfigFile) : Result<AuthConfigFile, string> =
    if cfg.Profiles.ContainsKey(name) then
        Ok { cfg with Active = name }
    else
        let available = String.concat ", " cfg.Profiles.Keys
        Error $"Profile '{name}' not found. Available profiles: {available}"

// ---------------------------------------------------------------------------
// Convenience read-modify-write helpers
// ---------------------------------------------------------------------------

/// Read config from `homeDir`, apply `f`, write back. Returns `Ok ()` or an error.
let modifyConfigIn (homeDir: string) (f: AuthConfigFile -> Result<AuthConfigFile, string>) : Result<unit, string> =
    let cfg =
        match readConfigFrom homeDir with
        | Ok c  -> c
        | Error _ -> emptyConfig ()
    match f cfg with
    | Error e    -> Error e
    | Ok updated -> writeConfigTo homeDir updated

/// Read config, apply `f`, write back. Returns `Ok ()` or an error.
let modifyConfig (f: AuthConfigFile -> Result<AuthConfigFile, string>) : Result<unit, string> =
    let cfg =
        match readConfig () with
        | Ok c  -> c
        | Error _ -> emptyConfig ()
    match f cfg with
    | Error e    -> Error e
    | Ok updated -> writeConfig updated

// ---------------------------------------------------------------------------
// Migration helper
// ---------------------------------------------------------------------------

/// Delete the legacy ~/.config/orca/app.pem if it exists.
/// Silent no-op if the file is absent or cannot be deleted.
let removeOldPem () : unit =
    try
        let home =
            match Environment.GetEnvironmentVariable("HOME") with
            | null | "" -> Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            | h -> h
        let path = Path.Combine(home, ".config", "orca", "app.pem")
        if File.Exists(path) then File.Delete(path)
    with _ -> ()
