using Bxes.Logging;
using Bxes.Xes;

namespace Bxes.Console;

internal class XesToBxesCommandHandler(ILogger logger, bool preprocessValuesAndKeyValues) : ConvertCommandHandlerBase
{
  protected override IBetweenFormatsConverter CreateConverter() =>
    new XesToBxesConverter(logger, preprocessValuesAndKeyValues);
}