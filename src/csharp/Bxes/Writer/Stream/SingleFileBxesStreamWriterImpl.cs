using Bxes.Models;

namespace Bxes.Writer.Stream;

public class SingleFileBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent : IEvent
{
  private readonly MultipleFilesBxesStreamWriterImpl<TEvent> myMultipleWriter;
  private readonly string mySaveDirectoryName;
  private readonly string mySavePath;
  private readonly uint myBxesVersion;


  public SingleFileBxesStreamWriterImpl(string savePath, uint bxesVersion)
  {
    if (Path.GetDirectoryName(savePath) is not { } directoryName)
    {
      throw new DirectoryNotFoundException($"Failed to get parent directory for {savePath}");
    }

    mySavePath = savePath;
    myBxesVersion = bxesVersion;
    mySaveDirectoryName = directoryName;
    myMultipleWriter = new MultipleFilesBxesStreamWriterImpl<TEvent>(directoryName, bxesVersion);
  }

  public void HandleEvent(BxesStreamEvent @event) => myMultipleWriter.HandleEvent(@event);


  public void Dispose()
  {
    myMultipleWriter.Dispose();

    MergeFilesIntoOne();
  }

  private void MergeFilesIntoOne()
  {
    using var writer = new BinaryWriter(File.OpenWrite(mySavePath));
    writer.Write(myBxesVersion);

    BinaryReader OpenRead(string fileName) => new(File.OpenRead(Path.Join(mySaveDirectoryName, fileName)));

    SkipVersionAndCopyContents(OpenRead(BxesConstants.ValuesFileName), writer);
    SkipVersionAndCopyContents(OpenRead(BxesConstants.KVPairsFileName), writer);
    SkipVersionAndCopyContents(OpenRead(BxesConstants.MetadataFileName), writer);
    SkipVersionAndCopyContents(OpenRead(BxesConstants.TracesFileName), writer);
  }

  private static void SkipVersionAndCopyContents(BinaryReader reader, BinaryWriter writer)
  {
    try
    {
      const int VersionSize = sizeof(int);
      reader.BaseStream.Seek(VersionSize, SeekOrigin.Begin);

      WriteFromReaderToWriter(reader, writer);
    }
    finally
    {
      reader.Dispose();
    }
  }

  private static void WriteFromReaderToWriter(BinaryReader reader, BinaryWriter writer)
  {
    var buffer = new byte[1024];

    while (true)
    {
      var readCount = reader.Read(buffer);
      if (readCount == 0)
      {
        break;
      }

      writer.Write(buffer, 0, readCount);
    }
  }
}