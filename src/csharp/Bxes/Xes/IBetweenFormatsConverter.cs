using Bxes.Logging;

namespace Bxes.Xes;

public interface IBetweenFormatsConverter
{
  void Convert(string filePath, string outputPath, ILogger logger);
}