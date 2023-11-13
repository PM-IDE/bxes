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

    await ExecuteWithFile(savePath, BxesConstants.ValuesFileName, bw => Write(bw, BxesWriteUtils.WriteValues));
    await ExecuteWithFile(savePath, BxesConstants.KVPairsFileName, bw => Write(bw, BxesWriteUtils.WriteKeyValuePairs));
    await ExecuteWithFile(savePath, BxesConstants.MetadataFileName, bw => Write(bw, BxesWriteUtils.WriteEventLogMetadata));
    await ExecuteWithFile(savePath, BxesConstants.TracesFileName, bw => Write(bw, BxesWriteUtils.WriteTracesVariants));
  }

  private static Task ExecuteWithFile(string saveDirectory, string fileName, Action<BinaryWriter> writeAction) => 
    BxesWriteUtils.ExecuteWithFile(Path.Combine(saveDirectory, fileName), writer =>
    {
      BxesWriteUtils.WriteBxesVersion(writer);
      writeAction(writer);
    });
}

public class SavePathIsNotDirectoryException(string savePath) : BxesException
{
  public override string Message { get; } = $"The {savePath} is not a directory or it does not exist";
}