using Bxes.Xes;

namespace Bxes.Console;

internal class XesToBxesCommandHandler : ConvertCommandHandlerBase
{
  protected override IBetweenFormatsConverter CreateConverter() => new XesToBxesConverter();
}