using System.Reflection;
using Lintellect.Cli.Services.Analyzers.CodeQL;
using Lintellect.Shared.Models;
using Shouldly;

namespace Lintellect.Cli.UnitTests.Tests.CodeQL;

public abstract class CodeQLTestBase
{
    protected string GetFixturePath(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(assemblyDir, "Fixtures", "CodeQL", fileName);
    }

    protected string LoadSarifFixture(string fileName)
    {
        var path = GetFixturePath(fileName);
        return File.ReadAllText(path);
    }

    protected T InvokePrivateMethod<T>(object instance, string methodName, params object[] parameters)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found");
        }

        var result = method.Invoke(instance, parameters);
        return (T)result!;
    }

    protected static T InvokePrivateStaticMethod<T>(Type type, string methodName, params object[] parameters)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException($"Static method '{methodName}' not found on type {type.Name}");
        }

        var result = method.Invoke(null, parameters);
        return (T)result!;
    }

    internal void AssertAnalyzerProperties(CodeQLAnalyzerBase analyzer, EProgrammingLanguage expectedLanguage, string expectedIdentifier, string expectedSkipDirs)
    {
        analyzer.Language.ShouldBe(expectedLanguage);

        var identifierMethod = analyzer.GetType().GetMethod("GetLanguageIdentifier", BindingFlags.NonPublic | BindingFlags.Instance);
        var identifier = identifierMethod?.Invoke(analyzer, null)?.ToString();
        identifier.ShouldBe(expectedIdentifier);

        var skipDirsMethod = analyzer.GetType().GetMethod("GetSkipDirectories", BindingFlags.NonPublic | BindingFlags.Instance);
        var skipDirs = skipDirsMethod?.Invoke(analyzer, null)?.ToString();
        skipDirs.ShouldBe(expectedSkipDirs);
    }
}
