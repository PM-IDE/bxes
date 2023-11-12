using Bxes.Models;

namespace Bxes.Writer;

public class SingleFileBxesWriter : IBxesWriter
{
  public Task WriteAsync(IEventLog log, string savePath)
  {
    return BxesWriteUtils.ExecuteWithFile(savePath, writer =>
    {
      var context = new BxesWriteContext(writer);

      BxesWriteUtils.WriteBxesVersion(writer);
      BxesWriteUtils.WriteValues(log, context);
      BxesWriteUtils.WriteKeyValuePairs(log, context);
      BxesWriteUtils.WriteEventLogMetadata(log, context);
      BxesWriteUtils.WriteTracesVariants(log, context);
    });
  }
}