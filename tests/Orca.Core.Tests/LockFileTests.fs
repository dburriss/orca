module Orca.Core.Tests.LockFileTests

open System
open System.IO
open Xunit
open Orca.Core.LockFile
open Orca.Core.Domain

// ---------------------------------------------------------------------------
// Unit tests for LockFile — path derivation, read, and write.
// ---------------------------------------------------------------------------

[<Fact>]
let ``lockFilePath derives correct path from yaml path`` () =
    let result = lockFilePath "/projects/myjob.yml"
    Assert.Equal("/projects/myjob.lock.json", result)

[<Fact>]
let ``lockFilePath handles yaml file without directory`` () =
    let result = lockFilePath "job.yml"
    Assert.Equal("job.lock.json", result)

[<Fact>]
let ``tryRead returns None when lock file does not exist`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    // Don't create the lock file
    let result = tryRead yamlPath
    Assert.True(result.IsNone)

let private sampleLock () : LockFile =
    { LockedAt     = DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero)
      YamlHash     = "abc123"
      Project      = { Org = OrgName "myorg"; Number = 42; Title = "My Project" }
      Repos        = [ RepoName "myorg/repo-a"; RepoName "myorg/repo-b" ]
      Issues       =
          [ { Repo      = RepoName "myorg/repo-a"
              Number    = IssueNumber 7
              Url       = "https://github.com/myorg/repo-a/issues/7"
              Assignees = [ "copilot" ] } ]
      PullRequests =
          [ { Repo        = RepoName "myorg/repo-a"
              Number      = PrNumber 3
              Url         = "https://github.com/myorg/repo-a/pull/3"
              ClosesIssue = IssueNumber 7 } ] }

[<Fact>]
let ``write then tryRead round-trips the lock file`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    let original = sampleLock ()

    write yamlPath original

    match tryRead yamlPath with
    | None      -> Assert.Fail("Expected Some but got None")
    | Some read ->
        Assert.Equal(original.YamlHash, read.YamlHash)
        Assert.Equal(original.Project.Number, read.Project.Number)
        Assert.Equal(original.Project.Title,  read.Project.Title)
        let (OrgName orgOrig) = original.Project.Org
        let (OrgName orgRead) = read.Project.Org
        Assert.Equal(orgOrig, orgRead)
        Assert.Equal(original.Repos.Length, read.Repos.Length)
        Assert.Equal(original.Issues.Length, read.Issues.Length)
        Assert.Equal(original.PullRequests.Length, read.PullRequests.Length)

[<Fact>]
let ``write creates a JSON file at the expected path`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "myjob.yml")
    write yamlPath (sampleLock ())
    let expected = Path.Combine(dir, "myjob.lock.json")
    Assert.True(File.Exists(expected), $"Expected lock file at {expected}")

[<Fact>]
let ``write preserves lockedAt timestamp`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    let lock     = sampleLock ()
    write yamlPath lock
    match tryRead yamlPath with
    | None      -> Assert.Fail("Expected Some but got None")
    | Some read ->
        // Compare to second precision to avoid formatting drift
        Assert.Equal(lock.LockedAt.ToUnixTimeSeconds(), read.LockedAt.ToUnixTimeSeconds())

[<Fact>]
let ``write round-trips issue assignees`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    write yamlPath (sampleLock ())
    match tryRead yamlPath with
    | None      -> Assert.Fail("Expected Some but got None")
    | Some read ->
        let issue = read.Issues |> List.head
        Assert.Contains("copilot", issue.Assignees)
