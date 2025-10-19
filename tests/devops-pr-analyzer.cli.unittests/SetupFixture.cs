using System;
using System.Collections.Generic;
using System.Text;

namespace devops_pr_analyzer.cli.integrationtests;

[SetUpFixture]
internal class SetupFixture
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Extract the SimpleRepo.zip before any tests run
        TestHelpers.UnZipSampleRepo("SimpleRepo.zip");
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Clean up Fixtures directory in the output directory
        var assembly = typeof(SetupFixture).Assembly;
        var assemblyLocation = assembly.Location;
        var outputDir = Path.GetDirectoryName(assemblyLocation)!;
        var fixturePath = Path.Combine(outputDir, "Fixtures");
        
        if (Directory.Exists(fixturePath))
        {
            Directory.Delete(fixturePath, true);
        }
    }
}
