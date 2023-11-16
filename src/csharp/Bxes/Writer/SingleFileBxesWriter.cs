using Bxes.Models;
using Bxes.Utils;

namespace Bxes.Writer;

public class SingleFileBxesWriter : IBxesWriter
{
  public Task WriteAsync(IEventLog log, string savePath)
  {
    PathUtil.EnsureDeleted(savePath);

    return BxesWriteUtils.ExecuteWithFile(savePath, writer =>
    {
      var context = new BxesWriteContext(writer);

      BxesWriteUtils.WriteBxesVersion(writer, log.Version);
      BxesWriteUtils.WriteValues(log, context);
      BxesWriteUtils.WriteKeyValuePairs(log, context);
      BxesWriteUtils.WriteEventLogMetadata(log, context);
      BxesWriteUtils.WriteTracesVariants(log, context);
    });
  }
}