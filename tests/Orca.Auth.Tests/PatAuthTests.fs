module Orca.Auth.Tests.PatAuthTests

open System
open System.IO
open Xunit
open Orca.Auth.PatAuth
open Orca.Auth.AuthConfig
open Orca.Auth.Tests.TestHelpers

// ---------------------------------------------------------------------------
// loadTokenWith — pure tests (no env mutation)
// ---------------------------------------------------------------------------

let private noConfig () : Result<AuthConfigFile, string> = Error "no config"

let private makeConfig (profileName: string) (entry: ProfileEntry) : unit -> Result<AuthConfigFile, string> =
    fun () ->
        let profiles = System.Collections.Generic.Dictionary<string, ProfileEntry>()
        profiles.[profileName] <- entry
        Ok { Active = profileName; Profiles = profiles }

let private patConfig token =
    makeConfig "pat" { Type = "pat"; Token = Some token; AppId = None; KeyPath = None; InstallationId = None }

[<Fact>]
let ``loadTokenWith returns token from getEnv when ORCA_PAT is set`` () =
    let getEnv name = if name = "ORCA_PAT" then Some "ghp_from_env" else None
    Assert.Equal(Ok "ghp_from_env", loadTokenWith getEnv noConfig)

[<Fact>]
let ``loadTokenWith ignores empty ORCA_PAT and falls back to config`` () =
    let getEnv name = if name = "ORCA_PAT" then Some "" else None
    Assert.Equal(Ok "ghp_stored", loadTokenWith getEnv (patConfig "ghp_stored"))

[<Fact>]
let ``loadTokenWith returns error when env var absent and no config`` () =
    let getEnv _ = None
    Assert.True(Result.isError (loadTokenWith getEnv noConfig))

[<Fact>]
let ``loadTokenWith returns error when active profile type is not pat`` () =
    let getEnv _ = None
    let appConfig =
        makeConfig "my-app" { Type = "app"; Token = None; AppId = Some "id"; KeyPath = Some "/k"; InstallationId = Some "i" }
    match loadTokenWith getEnv appConfig with
    | Ok _    -> Assert.Fail("Expected error for non-PAT config")
    | Error e -> Assert.Contains("PAT", e)

[<Fact>]
let ``loadTokenWith returns error when pat token in config is empty`` () =
    let getEnv _ = None
    let emptyToken =
        makeConfig "pat" { Type = "pat"; Token = Some ""; AppId = None; KeyPath = None; InstallationId = None }
    Assert.True(Result.isError (loadTokenWith getEnv emptyToken))

// ---------------------------------------------------------------------------
// loadToken — integration: still uses real env + real config file path
// ---------------------------------------------------------------------------

[<Fact>]
let ``loadToken returns ORCA_PAT when env var is set`` () =
    withEnv "ORCA_PAT" (Some "ghp_test_token_from_env") (fun () ->
        Assert.Equal(Ok "ghp_test_token_from_env", loadToken ()))

[<Fact>]
let ``loadToken ignores ORCA_PAT when it is empty string`` () =
    withEnv "ORCA_PAT" (Some "") (fun () ->
        match loadToken () with
        | Error _ -> () // expected — no config file present
        | Ok _    -> Assert.Fail("Expected an error when ORCA_PAT is empty and no file exists"))

[<Fact>]
let ``loadToken returns error when ORCA_PAT absent and no config file exists`` () =
    withEnv "ORCA_PAT" None (fun () ->
        let tmpHome = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        let originalHome = Environment.GetEnvironmentVariable("HOME")
        try
            Environment.SetEnvironmentVariable("HOME", tmpHome)
            match loadToken () with
            | Error _ -> ()
            | Ok _    -> Assert.Fail("Expected an error when no env var and no config file")
        finally
            Environment.SetEnvironmentVariable("HOME", originalHome)
            if Directory.Exists(tmpHome) then Directory.Delete(tmpHome, true))

