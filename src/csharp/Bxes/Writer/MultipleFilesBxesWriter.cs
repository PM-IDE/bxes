using Bxes.Models;

namespace Bxes.Writer;

public class MultipleFilesBxesWriter : IBxesWriter
{
  public async Task WriteAsync(IEventLog log, string savePath)
  {
    if (!Directory.Exists(savePath))
    {
      //todo: exceptions
      return;
    }

    var context = new BxesWriteContext();

    await ExecuteWithFile(savePath, BxesConstants.ValuesFileName, bw => BxesWriteUtils.WriteValues(log, context.WithWriter(bw)));
    await ExecuteWithFile(savePath, BxesConstants.KeyValuePairsFileName, bw => BxesWriteUtils.WriteKeyValuePairs(log, context.WithWriter(bw)));
    await ExecuteWithFile(savePath, BxesConstants.LogMetadataFileName, bw => BxesWriteUtils.WriteEventLogMetadata(log, context.WithWriter(bw)));
    await ExecuteWithFile(savePath, BxesConstants.TracesFileName, bw => BxesWriteUtils.WriteTracesVariants(log, context.WithWriter(bw)));
  }

  private static Task ExecuteWithFile(string saveDirectory, string fileName, Action<BinaryWriter> writeAction)
    => BxesWriteUtils.ExecuteWithFile(Path.Combine(saveDirectory, fileName), writer =>
    {
      BxesWriteUtils.WriteBxesVersion(writer);
      writeAction(writer);
    });
}