using System.CommandLine.Parsing;
using Bxes.Xes;

namespace Bxes.Console;

internal class BxesToXesCommandHandler : ConvertCommandHandlerBase
{
  protected override IBetweenFormatsConverter CreateConverter(ParseResult result) => new BxesToXesConverter();
}