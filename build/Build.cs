using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    // Console application entry point. Also defines the default target.
    public static int Main() => Execute<Build>(x => x.CreateService);

    const string PunchCardService = "PunchCardService";
    // Auto-injection fields:

    // [GitVersion] readonly GitVersion GitVersion;
    // Semantic versioning. Must have 'GitVersion.CommandLine' referenced.

    // [GitRepository] readonly GitRepository GitRepository;
    // Parses origin, branch name and head from git config.

    // [Parameter] readonly string MyGetApiKey;
    // Returns command-line arguments and environment variables.

    // [Solution] readonly Solution Solution;
    // Provides access to the structure of the solution.

    Target Clean => _ => _
            .Executes(() =>
            {
                ProcessTasks.StartProcess("sc", $"stop {PunchCardService}");
            });

    Target Restore => _ => _
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore(s => DefaultDotNetRestore);
            });

    Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(s => DefaultDotNetBuild);
            });

    Target Publish => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                DotNetPublish(s => DefaultDotNetPublish.SetRuntime("win10-x64"));
            });

    Target CreateService => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            var path = $@"{SolutionDirectory}\PunchCard\bin\Release\netcoreapp2.0\win10-x64\PunchCard.exe";

            ProcessTasks.StartProcess("sc", $"create {PunchCardService} binPath= {path} ");
            ProcessTasks.StartProcess("sc", $"start {PunchCardService}");
        });

    Target SetupBat => _ => _
       //.DependsOn(CreateService)
       .Executes(() =>
       {
           FileSystemTasks.CopyRecursively($@"{SolutionDirectory}\build\Scripts\", $@"C:\Windows\System32\GroupPolicy\User\Scripts\", FileSystemTasks.FileExistsPolicy.Overwrite);
       });
}