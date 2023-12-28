using System.CommandLine;
using System.CommandLine.Parsing;

namespace Bxes.Console;

internal static class Options
{
  public static Option<string> PathOption { get; } =
    new("-path", "The path to the target file") { IsRequired = true };

  public static Option<string> OutputPathOption { get; } =
    new("-output-path", "The output path for converted file") { IsRequired = true };


  public static T GetValueOrThrow<T>(this ParseResult parseResult, Option<T> option) =>
    parseResult.GetValueForOption(option) ?? throw new MissingRequiredOptionException(option.Name);
}