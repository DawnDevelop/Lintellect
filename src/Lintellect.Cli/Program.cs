using System.CommandLine;
using Lintellect.Cli.Commands;

Console.WriteLine("========================================");
Console.WriteLine("DevOps PR Analyzer CLI");
Console.WriteLine($"Version: {typeof(Program).Assembly.GetName().Version}");
Console.WriteLine("========================================");
Console.WriteLine();

var rootCommand = new RootCommand("DevOps PR Analyzer CLI");

var staticAnalysisCommand = new StaticAnalysisCommand();
var codeQLCommand = new CodeQLCommand();

rootCommand.Add(staticAnalysisCommand);
rootCommand.Add(codeQLCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync().ConfigureAwait(false);
