module OrcAI.Core.Tests.LockFileTests

open System
open Xunit
open Testably.Abstractions.Testing
open OrcAI.Core.LockFile
open OrcAI.Core.Domain

// ---------------------------------------------------------------------------
// Unit tests for LockFile — path derivation, read, and write.
// All file I/O uses MockFileSystem (in-memory); no real disk access.
// ---------------------------------------------------------------------------

[<Fact>]
let ``lockFilePath derives correct path from yaml path`` () =
    let dir      = "/projects"
    let yamlPath = "/projects/myjob.yml"
    let expected = "/projects/myjob.lock.json"
    let result   = lockFilePath yamlPath
    Assert.Equal(expected, result)

[<Fact>]
let ``lockFilePath handles yaml file without directory`` () =
    let result = lockFilePath "job.yml"
    Assert.Equal("job.lock.json", result)

[<Fact>]
let ``tryRead returns None when lock file does not exist`` () =
    let fs       = MockFileSystem()
    let yamlPath = "/work/job.yml"
    // Don't create the lock file
    let result = tryRead fs yamlPath
    Assert.True(result.IsNone)

let private sampleLock () : LockFile =
    { LockedAt     = DateTimeOffset(2026, 3, 2, 10, 0, 0, TimeSpan.Zero)
      YamlHash     = "abc123"
      Project      = { Org = OrgName "myorg"; Number = 42; Title = "My Project"; Url = "https://github.com/users/myorg/projects/42" }
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
    let fs       = MockFileSystem()
    let yamlPath = "/work/job.yml"
    let original = sampleLock ()

    write fs yamlPath original

    match tryRead fs yamlPath with
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
    let fs       = MockFileSystem()
    let yamlPath = "/work/myjob.yml"
    write fs yamlPath (sampleLock ())
    let expected = "/work/myjob.lock.json"
    Assert.True(fs.File.Exists(expected), $"Expected lock file at {expected}")

[<Fact>]
let ``write preserves lockedAt timestamp`` () =
    let fs       = MockFileSystem()
    let yamlPath = "/work/job.yml"
    let lock     = sampleLock ()
    write fs yamlPath lock
    match tryRead fs yamlPath with
    | None      -> Assert.Fail("Expected Some but got None")
    | Some read ->
        // Compare to second precision to avoid formatting drift
        Assert.Equal(lock.LockedAt.ToUnixTimeSeconds(), read.LockedAt.ToUnixTimeSeconds())

[<Fact>]
let ``write round-trips issue assignees`` () =
    let fs       = MockFileSystem()
    let yamlPath = "/work/job.yml"
    write fs yamlPath (sampleLock ())
    match tryRead fs yamlPath with
    | None      -> Assert.Fail("Expected Some but got None")
    | Some read ->
        let issue = read.Issues |> List.head
        Assert.Contains("copilot", issue.Assignees)

// ---------------------------------------------------------------------------
// DTO mapping — tested via round-trip with specific field assertions
// ---------------------------------------------------------------------------

[<Fact>]
let ``round-trip preserves all repo names`` () =
    let fs   = MockFileSystem()
    let path = "/work/job.yml"
    let lock = sampleLock ()
    write fs path lock
    match tryRead fs path with
    | None      -> Assert.Fail("Expected Some")
    | Some read ->
        Assert.Equal<string list>(
            lock.Repos |> List.map (fun (RepoName r) -> r),
            read.Repos |> List.map (fun (RepoName r) -> r))

[<Fact>]
let ``round-trip preserves issue number and URL`` () =
    let fs   = MockFileSystem()
    let path = "/work/job.yml"
    write fs path (sampleLock ())
    match tryRead fs path with
    | None      -> Assert.Fail("Expected Some")
    | Some read ->
        let issue = read.Issues |> List.head
        Assert.Equal(IssueNumber 7, issue.Number)
        Assert.Equal("https://github.com/myorg/repo-a/issues/7", issue.Url)

[<Fact>]
let ``round-trip preserves PR number, URL, and closesIssue`` () =
    let fs   = MockFileSystem()
    let path = "/work/job.yml"
    write fs path (sampleLock ())
    match tryRead fs path with
    | None      -> Assert.Fail("Expected Some")
    | Some read ->
        let pr = read.PullRequests |> List.head
        Assert.Equal(PrNumber 3, pr.Number)
        Assert.Equal("https://github.com/myorg/repo-a/pull/3", pr.Url)
        Assert.Equal(IssueNumber 7, pr.ClosesIssue)

[<Fact>]
let ``round-trip preserves project org, title, number, and URL`` () =
    let fs   = MockFileSystem()
    let path = "/work/job.yml"
    let lock = sampleLock ()
    write fs path lock
    match tryRead fs path with
    | None      -> Assert.Fail("Expected Some")
    | Some read ->
        Assert.Equal(lock.Project.Org,    read.Project.Org)
        Assert.Equal(lock.Project.Number, read.Project.Number)
        Assert.Equal(lock.Project.Title,  read.Project.Title)
        Assert.Equal(lock.Project.Url,    read.Project.Url)

[<Fact>]
let ``round-trip preserves yaml hash`` () =
    let fs   = MockFileSystem()
    let path = "/work/job.yml"
    let lock = { sampleLock () with YamlHash = "deadbeefcafe1234" }
    write fs path lock
    match tryRead fs path with
    | None      -> Assert.Fail("Expected Some")
    | Some read -> Assert.Equal("deadbeefcafe1234", read.YamlHash)
