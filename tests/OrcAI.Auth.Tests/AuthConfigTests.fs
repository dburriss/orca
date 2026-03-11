module OrcAI.Auth.Tests.AuthConfigTests

open System
open System.IO
open Xunit
open OrcAI.Auth.AuthConfig

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private withTempHome (f: string -> unit) =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    try f dir
    finally
        if Directory.Exists(dir) then Directory.Delete(dir, true)

let private makeConfig (active: string) (profiles: (string * ProfileEntry) list) : AuthConfigFile =
    let dict = System.Collections.Generic.Dictionary<string, ProfileEntry>()
    for (name, entry) in profiles do dict.[name] <- entry
    { Active = active; Profiles = dict }

let private appEntry appId keyPath installId =
    { Type = "app"; Token = None; AppId = Some appId; KeyPath = Some keyPath; InstallationId = Some installId }

let private patEntry token =
    { Type = "pat"; Token = Some token; AppId = None; KeyPath = None; InstallationId = None }

// ---------------------------------------------------------------------------
// getActiveProfile
// ---------------------------------------------------------------------------

[<Fact>]
let ``getActiveProfile returns correct profile`` () =
    let cfg = makeConfig "my-app" [ "my-app", appEntry "123" "/key.pem" "install-1" ]
    match getActiveProfile cfg with
    | Error e -> Assert.Fail($"Expected Ok but got Error: {e}")
    | Ok p    -> Assert.Equal("app", p.Type)

[<Fact>]
let ``getActiveProfile returns error when active key is empty`` () =
    let cfg = makeConfig "" [ "my-app", appEntry "123" "/key.pem" "install-1" ]
    match getActiveProfile cfg with
    | Ok _    -> Assert.Fail("Expected error for empty active key")
    | Error e -> Assert.Contains("No active profile", e)

[<Fact>]
let ``getActiveProfile returns error when active points to missing profile`` () =
    let cfg = makeConfig "missing" [ "my-app", appEntry "123" "/key.pem" "install-1" ]
    match getActiveProfile cfg with
    | Ok _    -> Assert.Fail("Expected error when active profile is missing")
    | Error e -> Assert.Contains("missing", e)

// ---------------------------------------------------------------------------
// upsertProfile
// ---------------------------------------------------------------------------

[<Fact>]
let ``upsertProfile adds a new profile and sets it as active`` () =
    let cfg    = makeConfig "" []
    let entry  = appEntry "123" "/key.pem" "install-1"
    let result = upsertProfile "my-app" entry cfg
    Assert.Equal("my-app", result.Active)
    Assert.True(result.Profiles.ContainsKey("my-app"))

[<Fact>]
let ``upsertProfile replaces an existing profile`` () =
    let cfg    = makeConfig "my-app" [ "my-app", appEntry "old-id" "/old.pem" "old-install" ]
    let entry  = appEntry "new-id" "/new.pem" "new-install"
    let result = upsertProfile "my-app" entry cfg
    Assert.Equal("new-id", result.Profiles.["my-app"].AppId |> Option.defaultValue "")

[<Fact>]
let ``upsertProfile sets the named profile as active`` () =
    let cfg    = makeConfig "other" [ "other", patEntry "tok"; "my-app", appEntry "id" "/k.pem" "i" ]
    let result = upsertProfile "my-app" (appEntry "id" "/k.pem" "i") cfg
    Assert.Equal("my-app", result.Active)

// ---------------------------------------------------------------------------
// switchActive
// ---------------------------------------------------------------------------

[<Fact>]
let ``switchActive succeeds when profile exists`` () =
    let cfg = makeConfig "app1" [ "app1", appEntry "1" "/k1.pem" "i1"; "app2", appEntry "2" "/k2.pem" "i2" ]
    match switchActive "app2" cfg with
    | Error e -> Assert.Fail($"Expected Ok but got Error: {e}")
    | Ok updated -> Assert.Equal("app2", updated.Active)

[<Fact>]
let ``switchActive returns error when profile does not exist`` () =
    let cfg = makeConfig "app1" [ "app1", appEntry "1" "/k1.pem" "i1" ]
    match switchActive "nonexistent" cfg with
    | Ok _    -> Assert.Fail("Expected error when profile does not exist")
    | Error e -> Assert.Contains("nonexistent", e)

[<Fact>]
let ``switchActive does not modify other profiles`` () =
    let cfg = makeConfig "app1" [ "app1", appEntry "1" "/k1.pem" "i1"; "app2", appEntry "2" "/k2.pem" "i2" ]
    match switchActive "app2" cfg with
    | Error e -> Assert.Fail($"Expected Ok: {e}")
    | Ok updated ->
        Assert.Equal(2, updated.Profiles.Count)
        Assert.True(updated.Profiles.ContainsKey("app1"))

// ---------------------------------------------------------------------------
// writeConfigTo / readConfigFrom round-trip
// ---------------------------------------------------------------------------

[<Fact>]
let ``writeConfigTo then readConfigFrom round-trips active and profiles`` () =
    withTempHome (fun home ->
        let cfg = makeConfig "my-app" [ "my-app", appEntry "123" "/key.pem" "install-1" ]
        match writeConfigTo home cfg with
        | Error e -> Assert.Fail($"writeConfigTo failed: {e}")
        | Ok () ->
            match readConfigFrom home with
            | Error e -> Assert.Fail($"readConfigFrom failed: {e}")
            | Ok loaded ->
                Assert.Equal("my-app", loaded.Active)
                Assert.True(loaded.Profiles.ContainsKey("my-app"))
                Assert.Equal(Some "123", loaded.Profiles.["my-app"].AppId))

[<Fact>]
let ``readConfigFrom returns error when file does not exist`` () =
    withTempHome (fun home ->
        match readConfigFrom home with
        | Ok _    -> Assert.Fail("Expected error when no config file exists")
        | Error e -> Assert.Contains("No auth config found", e))

// ---------------------------------------------------------------------------
// removeOldPem
// ---------------------------------------------------------------------------

[<Fact>]
let ``removeOldPem deletes app.pem when it exists`` () =
    // Use a temp directory to simulate HOME, write a fake app.pem, then call
    // removeOldPem via HOME env var override.
    withTempHome (fun tmpHome ->
        let orcaDir = Path.Combine(tmpHome, ".config", "orcai")
        Directory.CreateDirectory(orcaDir) |> ignore
        let pemPath = Path.Combine(orcaDir, "app.pem")
        File.WriteAllText(pemPath, "fake pem content")
        Assert.True(File.Exists(pemPath), "Setup: app.pem should exist before calling removeOldPem")
        let original = Environment.GetEnvironmentVariable("HOME")
        try
            Environment.SetEnvironmentVariable("HOME", tmpHome)
            removeOldPem ()
        finally
            Environment.SetEnvironmentVariable("HOME", original)
        Assert.False(File.Exists(pemPath), "app.pem should have been deleted"))

[<Fact>]
let ``removeOldPem is a no-op when app.pem is absent`` () =
    withTempHome (fun tmpHome ->
        let original = Environment.GetEnvironmentVariable("HOME")
        try
            Environment.SetEnvironmentVariable("HOME", tmpHome)
            // Should not throw
            removeOldPem ()
        finally
            Environment.SetEnvironmentVariable("HOME", original))
