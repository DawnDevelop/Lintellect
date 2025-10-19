using devops_pr_analyzer.cli.Commands;
using System.CommandLine;

var rootCommand = new RootCommand("DevOps PR Analyzer CLI");

var staticAnalysisCommand = new StaticAnalysisCommand();
rootCommand.Add(staticAnalysisCommand);

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync().ConfigureAwait(false);