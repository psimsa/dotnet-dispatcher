using System.Linq;
using NuGet.Versioning;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "Continuous build",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    OnPushBranchesIgnore = new[] { "main" },
    InvokedTargets = new[] { nameof(Clean), nameof(Compile), nameof(Test), nameof(Pack) },
    EnableGitHubToken = true
)]
[GitHubActions(
    "Manual publish to Github Nuget",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    AutoGenerate = false,
    InvokedTargets = new[] { nameof(Pack), nameof(PublishToGitHubNuget) },
    EnableGitHubToken = true
    )]
[GitHubActions(
    "Build main and publish to nuget",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    AutoGenerate = false,
    InvokedTargets = new[]
        { nameof(Clean), nameof(Compile), nameof(Pack), nameof(PublishToGitHubNuget), nameof(Publish) },
    ImportSecrets = new[] { nameof(NuGetApiKey) },
    EnableGitHubToken = true
    )]
class Build : NukeBuild
{
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [LatestNuGetVersion(
        "DotnetDispatcher",
        IncludePrerelease = false)]
    readonly NuGetVersion DotnetDispatcherVersion;

    [Parameter][Secret] readonly string NuGetApiKey;

    [GitRepository] readonly GitRepository Repository;

    [Solution(GenerateProjects = true)] readonly Solution Solution;
    GitHubActions GitHubActions => GitHubActions.Instance;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution)
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
            );
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoBuild(true)
                .SetNoRestore(true)
                .SetVerbosity(DotNetVerbosity.normal)
            );
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .DependsOn(Clean)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            var newMajor = 0;
            var newMinor = 8;
            var newPatch = DotnetDispatcherVersion?.Patch ?? 0 + 1;

            if (DotnetDispatcherVersion != null)
            {
                if (newMajor > DotnetDispatcherVersion.Major)
                {
                    newMinor = 0;
                    newPatch = 0;
                }
                else if (newMinor > DotnetDispatcherVersion.Minor)
                {
                    newPatch = 0;
                }
            }

            var newVersion = new NuGetVersion(newMajor, newMinor, newPatch,
                Repository.IsOnMainOrMasterBranch() ? null : $"preview{GitHubActions?.RunNumber ?? 0}");

            DotNetPack(_ => _
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetNoBuild(true)
                .SetNoRestore(true)
                .SetVersion(newVersion.ToString())
                .SetVerbosity(DotNetVerbosity.normal)
                .SetProject(Solution.src.DotnetDispatcher)
            );
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetApiKey)
            );
        });

    Target PublishToGitHubNuget => _ => _
        .DependsOn(Pack)
        .Consumes(Pack)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .SetSource("https://nuget.pkg.github.com/psimsa/index.json")
                .SetApiKey(GitHubActions.Token)
            );
        });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);
}