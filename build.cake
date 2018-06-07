#tool "nuget:?package=NUnit.Runners&version=2.6.4"
#tool "nuget:?package=GitVersion.CommandLine"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS AND ENVIRONMENT VARIABLES
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var nugetOrgApiKey = EnvironmentVariable("NuGetOrgApiKey");
var isTaggedBuild = Convert.ToBoolean(EnvironmentVariable("APPVEYOR_REPO_TAG"));
var tag = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME") ?? "<no tag>";

var isAppVeyor = BuildSystem.IsRunningOnAppVeyor;
var isTravis = BuildSystem.IsRunningOnTravisCI;

//////////////////////////////////////////////////////////////////////
// VERSION
//////////////////////////////////////////////////////////////////////

// GitVersion doesn't work on Linux: https://github.com/cake-build/cake/issues/2126
var gitVersion = !IsRunningOnWindows() ? null : GitVersion(new GitVersionSettings
{
	OutputType          = GitVersionOutput.Json,
	UpdateAssemblyInfo  = false
});

var version = "0.0.1";
var branchName = "unknown";

if (gitVersion != null)
{
    version = gitVersion.NuGetVersion;
    branchName = gitVersion.BranchName.Trim();
}

//////////////////////////////////////////////////////////////////////
// CONSTS
//////////////////////////////////////////////////////////////////////

var artifactsDir = Directory("./artifacts");
var outputDir = Directory("./build");

var dotnetFrameworks = IsRunningOnWindows() ? new [] { "net471", "net45", "net40", "netstandard2.0" } : new string[] { };
// net35 can't be build by dotnet - https://github.com/Microsoft/msbuild/issues/1333
var msBuildFrameworks = IsRunningOnWindows() ? new [] { "net35" } : new [] { "net471", "net45", "net40", "net35", "netstandard2.0" };

var frameworks = dotnetFrameworks.Union(msBuildFrameworks).ToList();

var solution = "src/SharpRaven.sln";
var packages = new []
{
    "src/app/SharpRaven/SharpRaven.csproj",
    "src/app/SharpRaven.Nancy/SharpRaven.Nancy.csproj",
};

//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information("Building version {0} ({1}@{2}) of SharpRaven.",
                version, branchName, tag);

    if (isAppVeyor)
    {
        // If AppVeyor is building master with no tag, it should
        // not update the build version, since it will be duplicate
        // with the one for the tagged build
        if (branchName == "master" && !isTaggedBuild)
        {
            return;
        }

        Information("Updating AppVeyor build version:" + gitVersion.FullBuildMetaData);
        AppVeyor.UpdateBuildVersion(gitVersion.FullBuildMetaData);
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Deletes all files in the artifact and output directories")
    .Does(() =>
	{
		CleanDirectory(artifactsDir);
		CleanDirectory(outputDir);
	});

Task("RestorePackages")
    .Description("Restores packages from nuget using 'dotnet'")
    .Does(() =>
    {
        DotNetCoreRestore(solution);
    });

Task("UpdateAssemblyInformation")
    .Description("Update assembly information using GitVersion")
    .WithCriteria(isAppVeyor)
    .Does(() =>
    {
        GitVersion(new GitVersionSettings
        {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = "src/CommonAssemblyInfo.cs",
        });

        Information("AssemblyVersion -> {0}", gitVersion.AssemblySemVer);
        Information("AssemblyFileVersion -> {0}.0", gitVersion.MajorMinorPatch);
        Information("AssemblyInformationalVersion -> {0}", gitVersion.InformationalVersion);
    });

Task("Build")
    .Description("Builds all versions")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAssemblyInformation")
    .Does(() =>
    {
        EnsureDirectoryExists(outputDir);

        foreach (var framework in msBuildFrameworks)
        {
            var settings =  new MSBuildSettings
            {
                Configuration = configuration + "-" + framework,
                ToolVersion = MSBuildToolVersion.VS2017
            };
            
            settings.WithProperty("TargetFramework", new string[] { framework })
                    .WithProperty("Optimize", new string[] { "true" });

            MSBuild(solution, settings);
        }

        foreach (var framework in dotnetFrameworks)
        {
            var settings = new DotNetCoreBuildSettings
            {
                Framework = framework,
                Configuration = configuration + "-" + framework,
                MSBuildSettings = new DotNetCoreMSBuildSettings()
                    .WithProperty("Optimize", new string[] { "true" })
            };

            DotNetCoreBuild(solution, settings);
        }
    });

Task("Test")
    .Description("Runs all the tests on all the versions")
    .IsDependentOn("Build")
    .Does(() =>
    {
        EnsureDirectoryExists(artifactsDir);

        foreach (var framework in frameworks.Where(x => x != "netstandard2.0"))
        {
            var assemblies = GetFiles((outputDir + Directory(configuration) + Directory(framework)).ToString() + "/*.UnitTests.dll");
            if (!assemblies.Any())
            {
                throw new FileNotFoundException("Could not find any test assemblies in: '" + configuration + "-" + framework + "'.");
            }

            var resultPath = artifactsDir + File(configuration + "-" + framework + "-tests.xml");
            NUnit(assemblies, new NUnitSettings
            {
                ResultsFile = resultPath,
                Exclude = IsRunningOnWindows() ? null : "NuGet,NoMono",
            });

            if (isAppVeyor)
            {
                AppVeyor.UploadTestResults(resultPath, AppVeyorTestResultsType.NUnit);
            }
        }
    });

Task("Package")
    .Description("Create NuGet packages")
    .IsDependentOn("Test")
    .Does(() =>
    {
        EnsureDirectoryExists(artifactsDir);

        foreach (var package in packages)
        {
            MSBuild(package, c => c
                .SetConfiguration("Release")
                .SetVerbosity(Verbosity.Minimal)
                .UseToolVersion(MSBuildToolVersion.VS2017)
                .WithProperty("NoBuild", "true")
                .WithProperty("Version", gitVersion.NuGetVersion)
                .WithTarget("Pack"));
        }

        MoveFiles((outputDir + Directory(configuration)).ToString() + "/*.nupkg", artifactsDir);
    });

Task("UploadAppVeyorArtifacts")
    .Description("Uploads artifacts to AppVeyor")
    .IsDependentOn("Package")
    .Does(() =>
    {
        foreach (var zip in System.IO.Directory.GetFiles(artifactsDir, "*.nupkg"))
        {
            AppVeyor.UploadArtifact(zip);
        }
    });

Task("PublishNuGetPackages")
    .Description("Publishes .nupkg files to nuget.org")
    .IsDependentOn("Package")
    .WithCriteria(isAppVeyor && (isTaggedBuild || branchName == "develop"))
    .Does(() =>
    {
        if (String.IsNullOrEmpty(nugetOrgApiKey))
        {
            throw new ArgumentNullException("nugetOrgApiKey");
        }

        var nugetFiles = GetFiles(artifactsDir.ToString() + "/*.nupkg");
        NuGetPush(nugetFiles, new NuGetPushSettings
        {
            ApiKey = nugetOrgApiKey,
            Source = "https://api.nuget.org/v3/index.json"
        });
    });

//////////////////////////////////////////////////////////////////////
// META TASKS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .Description("Rebuilds all versions")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Appveyor")
    .Description("Builds, tests and publishes packages on AppVeyor")
    .IsDependentOn("UploadAppVeyorArtifacts")
    .IsDependentOn("PublishNuGetPackages");

Task("Travis")
    .Description("Builds and tests on Travis")
    .IsDependentOn("Test");

Task("Default")
    .Description("Builds all versions")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
