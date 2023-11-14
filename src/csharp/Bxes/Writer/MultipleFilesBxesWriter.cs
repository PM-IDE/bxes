using Bxes.Models;

namespace Bxes.Writer;

public class MultipleFilesBxesWriter : IBxesWriter
{
  public async Task WriteAsync(IEventLog log, string savePath)
  {
    if (!Directory.Exists(savePath))
    {
      throw new SavePathIsNotDirectoryException(savePath);
    }

    var context = new BxesWriteContext();

    void Write(BinaryWriter writer, Action<IEventLog, BxesWriteContext> writeAction) =>
      writeAction(log, context.WithWriter(writer));

    var version = log.Version;
    await ExecuteWithFile(savePath, BxesConstants.ValuesFileName, version, bw => Write(bw, BxesWriteUtils.WriteValues));
    await ExecuteWithFile(savePath, BxesConstants.KVPairsFileName, version, bw => Write(bw, BxesWriteUtils.WriteKeyValuePairs));
    await ExecuteWithFile(savePath, BxesConstants.MetadataFileName, version, bw => Write(bw, BxesWriteUtils.WriteEventLogMetadata));
    await ExecuteWithFile(savePath, BxesConstants.TracesFileName, version, bw => Write(bw, BxesWriteUtils.WriteTracesVariants));
  }

  private static Task ExecuteWithFile(string saveDirectory, string fileName, uint version, Action<BinaryWriter> writeAction) =>
    BxesWriteUtils.ExecuteWithFile(Path.Combine(saveDirectory, fileName), writer =>
    {
      BxesWriteUtils.WriteBxesVersion(writer, version);
      writeAction(writer);
    });
}

public class SavePathIsNotDirectoryException(string savePath) : BxesException
{
  public override string Message { get; } = $"The {savePath} is not a directory or it does not exist";
}