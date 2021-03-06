#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console&version=2.2.0"

#addin "Cake.FileHelpers"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var solution        = "./src/Aranea.HttpCommandHandling.sln";
GitVersion versionInfo = null;



Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
});

Task("SetVersionInfo")
    .IsDependentOn("Clean")
    .Does(() =>
{
    versionInfo = GitVersion(new GitVersionSettings {
        RepositoryPath = "."
    });
});

Task("RestorePackages")
    .IsDependentOn("SetVersionInfo")
    .Does(() =>
{
    NuGetRestore(solution);
    DotNetCoreRestore(solution);
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
     {
         Framework = "netcoreapp1.1",
         Configuration = configuration,
         ArgumentCustomization = args => args.Append("/p:SemVer=" + versionInfo.NuGetVersionV2)
     };

    DotNetCoreBuild(solution, buildSettings);
});


Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
        var settings =  new DotNetCoreTestSettings 
        { 
            
        };
        DotNetCoreTest("./src/Aranea.HttpCommandHandling.Tests/Aranea.HttpCommandHandling.Tests.csproj", settings);
});

Task("MovePackages")
    .IsDependentOn("Build")
    .Does(() =>
{
    var files = GetFiles("./src/**/*.nupkg");
    MoveFiles(files, "./artifacts");

});

Task("NuGetPublish")
    .IsDependentOn("CopyPackages")
    .Does(() =>
    {
         
        var APIKey = EnvironmentVariable("NUGETAPIKEY");
        var packages = GetFiles("./artifacts/*.nupkg");
        NuGetPush(packages, new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = APIKey
        });
    });

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("NuGetPublish");

RunTarget(target);