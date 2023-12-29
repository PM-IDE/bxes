using Bxes.Logging;
using Bxes.Xes;
using Bxes.Xes.XesToBxes;

namespace Bxes.Console;

internal class XesToBxesCommandHandler(ILogger logger, bool preprocessValuesAndKeyValues) : ConvertCommandHandlerBase
{
  protected override IBetweenFormatsConverter CreateConverter() =>
    new XesToBxesConverter(logger, preprocessValuesAndKeyValues);
}