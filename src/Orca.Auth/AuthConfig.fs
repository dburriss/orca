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
// Schema (type-discriminated):
//   { "type": "pat", "token": "ghp_..." }
//   { "type": "app", "appId": "123", "keyPath": "/path/to/key.pem", "installationId": "456789" }
// ---------------------------------------------------------------------------

[<CLIMutable>]
type AuthConfigFile =
    { [<JsonPropertyName("type")>]           Type           : string
      // PAT fields
      [<JsonPropertyName("token")>]          Token          : string option
      // App fields
      [<JsonPropertyName("appId")>]          AppId          : string option
      [<JsonPropertyName("keyPath")>]        KeyPath        : string option
      [<JsonPropertyName("installationId")>] InstallationId : string option }

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
