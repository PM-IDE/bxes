using Bxes.Models;

namespace Bxes.Writer;

public interface IBxesWriter
{
  Task WriteAsync(IEventLog log, string savePath);
}