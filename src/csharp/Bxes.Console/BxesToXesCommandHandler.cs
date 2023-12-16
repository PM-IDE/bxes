using Bxes.Xes;

namespace Bxes.Console;

internal class BxesToXesCommandHandler : ConvertCommandHandlerBase
{
  protected override IBetweenFormatsConverter CreateConverter() => new BxesToXesConverter();
}