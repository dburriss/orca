module Orca.Core.Tests.YamlConfigTests

open System.IO
open Xunit
open Orca.Core.YamlConfig
open Orca.Core.Domain

// ---------------------------------------------------------------------------
// Unit tests for YAML config parsing and hash computation.
// ---------------------------------------------------------------------------

[<Fact>]
let ``parseFile returns error for missing file`` () =
    let result = parseFile "/nonexistent/path/job.yml"
    Assert.True(Result.isError result)

/// Write a temp YAML file plus an issue template and return the YAML path.
let private writeTempYaml (yaml: string) (templateContent: string) : string =
    let dir  = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let templatePath = Path.Combine(dir, "template.md")
    File.WriteAllText(templatePath, templateContent)
    let resolvedYaml = yaml.Replace("TEMPLATE_PLACEHOLDER", "./template.md")
    let yamlPath = Path.Combine(dir, "job.yml")
    File.WriteAllText(yamlPath, resolvedYaml)
    yamlPath

let private validYaml =
    "job:\n" +
    "  title: \"Add AGENTS.md\"\n" +
    "  org: \"myorg\"\n" +
    "repos:\n" +
    "  - \"repo-a\"\n" +
    "  - \"repo-b\"\n" +
    "issue:\n" +
    "  template: \"TEMPLATE_PLACEHOLDER\"\n" +
    "  labels: [\"documentation\"]\n"

[<Fact>]
let ``parseFile parses valid YAML into JobConfig`` () =
    let yamlPath = writeTempYaml validYaml "# Issue body"
    let result   = parseFile yamlPath
    match result with
    | Error e -> Assert.True(false, $"Expected Ok but got Error: {e}")
    | Ok cfg  ->
        Assert.Equal(OrgName "myorg", cfg.Org)
        Assert.Equal("Add AGENTS.md", cfg.ProjectTitle)
        Assert.Equal("Add AGENTS.md", cfg.IssueTitle)
        Assert.Equal(2, cfg.Repos.Length)
        Assert.Contains(RepoName "myorg/repo-a", cfg.Repos)
        Assert.Contains(RepoName "myorg/repo-b", cfg.Repos)
        Assert.Equal("# Issue body", cfg.IssueBody)

[<Fact>]
let ``parseFile prefixes repos with org`` () =
    let yamlPath = writeTempYaml validYaml "body"
    match parseFile yamlPath with
    | Error e -> Assert.True(false, $"Expected Ok but got Error: {e}")
    | Ok cfg  ->
        for repo in cfg.Repos do
            let (RepoName r) = repo
            Assert.StartsWith("myorg/", r)

[<Fact>]
let ``parseFile returns error when template file is missing`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    let content =
        "job:\n" +
        "  title: \"T\"\n" +
        "  org: \"o\"\n" +
        "repos:\n" +
        "  - \"r\"\n" +
        "issue:\n" +
        "  template: \"./missing.md\"\n"
    File.WriteAllText(yamlPath, content)
    let result = parseFile yamlPath
    Assert.True(Result.isError result)

[<Fact>]
let ``parseFile returns error when job section is missing`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    File.WriteAllText(yamlPath, "repos:\n  - r\n")
    let result = parseFile yamlPath
    Assert.True(Result.isError result)

[<Fact>]
let ``computeHash returns consistent hex string for same content`` () =
    let dir      = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let yamlPath = Path.Combine(dir, "job.yml")
    File.WriteAllText(yamlPath, "content: hello")
    let hash1 = computeHash yamlPath
    let hash2 = computeHash yamlPath
    Assert.Equal(hash1, hash2)
    Assert.Equal(64, hash1.Length) // SHA-256 hex = 32 bytes = 64 chars

[<Fact>]
let ``computeHash returns different hashes for different content`` () =
    let dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(dir) |> ignore
    let p1 = Path.Combine(dir, "a.yml")
    let p2 = Path.Combine(dir, "b.yml")
    File.WriteAllText(p1, "content: hello")
    File.WriteAllText(p2, "content: world")
    let h1 = computeHash p1
    let h2 = computeHash p2
    Assert.NotEqual<string>(h1, h2)
