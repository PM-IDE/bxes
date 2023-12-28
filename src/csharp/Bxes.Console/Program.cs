﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Globalization;
using Bxes.Console;

Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
var rootCommand = new Command("bxes");
var builder = new CommandLineBuilder(rootCommand);

rootCommand.AddCommand(CreateCommand("xes-to-bxes", "Convert XES event log to bxes format",
  new XesToBxesCommandHandler()));
rootCommand.AddCommand(CreateCommand("bxes-to-xes", "Convert bxes event log into XES format",
  new BxesToXesCommandHandler()));

builder.UseDefaults();

return builder.Build().Invoke(args);

Command CreateCommand(string name, string description, ConvertCommandHandlerBase handler)
{
  var command = new Command(name, description);
  command.AddOption(Options.PathOption);
  command.AddOption(Options.OutputPathOption);
  command.Handler = handler;

  return command;
}