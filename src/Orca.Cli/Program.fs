module Orca.Cli.Program

open System.Diagnostics
open Argu
open Orca.Cli.Args
open Orca.Auth.PatAuth
open Orca.Auth.AppAuth

// ---------------------------------------------------------------------------
// Entry point — parses CLI arguments and dispatches to the appropriate
// command module in Orca.Core.
// ---------------------------------------------------------------------------

/// Run `gh auth status` with the given token injected as GH_TOKEN.
/// Returns Ok with the status output, or Error with the error message.
let private validateToken (token: string) : Result<string, string> =
    try
        let psi = ProcessStartInfo("gh", "auth status")
        psi.Environment.["GH_TOKEN"] <- token
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError  <- true
        psi.UseShellExecute        <- false
        match Process.Start(psi) |> Option.ofObj with
        | None ->
            Error "Failed to start 'gh' process."
        | Some proc ->
            let stdout = proc.StandardOutput.ReadToEnd()
            let stderr = proc.StandardError.ReadToEnd()
            proc.WaitForExit()
            if proc.ExitCode = 0 then
                Ok (stdout.Trim())
            else
                Error (stderr.Trim())
    with ex ->
        Error $"Could not run 'gh auth status': {ex.Message}"

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<OrcaArgs>(programName = "orca")
    try
        let results = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)
        match results.GetSubCommand() with
        | Run args ->
            let _yamlFile = args.GetResult(RunArgs.Yaml_File)
            let _verbose  = args.Contains(RunArgs.Verbose)
            failwith "not implemented: run command"
        | Cleanup args ->
            let _yamlFile = args.GetResult(CleanupArgs.Yaml_File)
            let _dryRun   = args.Contains(CleanupArgs.Dryrun)
            failwith "not implemented: cleanup command"
        | Info args ->
            let _yamlFile  = args.GetResult(InfoArgs.Yaml_File)
            let _noLock    = args.Contains(InfoArgs.No_Lock)
            let _saveLock  = args.Contains(InfoArgs.Save_Lock)
            failwith "not implemented: info command"
        | Auth args ->
            match args.GetSubCommand() with
            | Pat patArgs ->
                let token = patArgs.GetResult(AuthPatArgs.Token)
                match storeToken token with
                | Error e ->
                    eprintfn "Error saving PAT: %s" e
                    1
                | Ok () ->
                    match validateToken token with
                    | Ok status ->
                        printfn "PAT saved and validated."
                        if status.Length > 0 then printfn "%s" status
                        0
                    | Error e ->
                        eprintfn "PAT saved but validation failed: %s" e
                        eprintfn "Ensure the token has the required scopes (project, repo)."
                        1
            | App appArgs ->
                let appId          = appArgs.GetResult(AuthAppArgs.App_Id)
                let key            = appArgs.GetResult(AuthAppArgs.Key)
                let installationId = appArgs.GetResult(AuthAppArgs.Installation_Id)
                let config         = { AppId = appId; PrivateKeyPath = key; InstallationId = installationId }
                match storeConfig config with
                | Error e ->
                    eprintfn "Error saving App config: %s" e
                    1
                | Ok () ->
                    // Exchange for an installation token and validate it.
                    let result =
                        (AppAuthContext(config) :> Orca.Core.AuthContext.IAuthContext)
                            .GetToken()
                        |> Async.RunSynchronously
                    match result with
                    | Error e ->
                        eprintfn "App config saved but token exchange failed: %s" e
                        1
                    | Ok installToken ->
                        match validateToken installToken with
                        | Ok status ->
                            printfn "GitHub App config saved and validated."
                            if status.Length > 0 then printfn "%s" status
                            0
                        | Error e ->
                            eprintfn "App config saved but validation failed: %s" e
                            1
    with
    | :? ArguParseException as ex ->
        eprintfn "%s" ex.Message
        1
    | ex ->
        eprintfn "Error: %s" ex.Message
        1
