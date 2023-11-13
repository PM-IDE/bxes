using Bxes.Models;

namespace Bxes.Writer;

public interface IBxesStreamWriter : IDisposable
{
  void HandleEvent(BxesStreamEvent @event);
}

public class SingleFileBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent : IEvent
{
  private readonly MultipleFilesBxesStreamWriterImpl<TEvent> myMultipleWriter;
  private readonly string mySaveDirectoryName;
  private readonly string mySavePath;


  public SingleFileBxesStreamWriterImpl(string savePath)
  {
    if (Path.GetDirectoryName(savePath) is not { } directoryName)
    {
      throw new DirectoryNotFoundException($"Failed to get parent directory for {savePath}");
    }

    mySavePath = savePath;
    mySaveDirectoryName = directoryName;
    myMultipleWriter = new MultipleFilesBxesStreamWriterImpl<TEvent>(directoryName);
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
    writer.Write(BxesConstants.BxesVersion);

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

public class MultipleFilesBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent : IEvent
{
  private readonly BinaryWriter myMetadataWriter;
  private readonly BinaryWriter myValuesWriter;
  private readonly BinaryWriter myKeyValuesWriter;
  private readonly BinaryWriter myTracesWriter;

  private readonly BxesWriteContext myContext = new();


  private uint myMetadataValuesCount;
  private uint myTracesVariantsCount;
  private uint myKeyValuePairsCount;
  private uint myValuesCount;

  private uint? myLastTraceVariantEventCount;
  private long myLastTraceVariantCountPosition;


  public MultipleFilesBxesStreamWriterImpl(string savePath)
  {
    if (!Directory.Exists(savePath)) throw new SavePathIsNotDirectoryException(savePath);

    BinaryWriter OpenWrite(string fileName) => new(File.OpenWrite(Path.Join(savePath, fileName)));

    myMetadataWriter = OpenWrite(BxesConstants.MetadataFileName);
    myValuesWriter = OpenWrite(BxesConstants.ValuesFileName);
    myKeyValuesWriter = OpenWrite(BxesConstants.KVPairsFileName);
    myTracesWriter = OpenWrite(BxesConstants.TracesFileName);

    WriteInitialInfo();
  }

  private void WriteInitialInfo()
  {
    myTracesWriter.Write(BxesConstants.BxesVersion);
    myMetadataWriter.Write(BxesConstants.BxesVersion);
    myKeyValuesWriter.Write(BxesConstants.BxesVersion);
    myValuesWriter.Write(BxesConstants.BxesVersion);

    myTracesWriter.Write((uint)0);
    myMetadataWriter.Write((uint)0);
    myKeyValuesWriter.Write((uint)0);
    myValuesWriter.Write((uint)0);
  }

  public void HandleEvent(BxesStreamEvent @event)
  {
    switch (@event)
    {
      case BxesEventEvent<TEvent> eventEvent:
        HandleEventEvent(eventEvent);
        break;
      case BxesLogMetadataKeyValueEvent metadataEvent:
        HandleMetadataEvent(metadataEvent);
        break;
      case BxesTraceVariantStartEvent variantStartEvent:
        HandleTraceVariantStart(variantStartEvent);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(@event));
    }
  }

  private void HandleEventEvent(BxesEventEvent<TEvent> @event)
  {
    myValuesCount += BxesWriteUtils.WriteEventValues(@event.Event, myContext.WithWriter(myValuesWriter));
    myKeyValuePairsCount += BxesWriteUtils.WriteEventKeyValuePairs(@event.Event, myContext.WithWriter(myKeyValuesWriter));
    BxesWriteUtils.WriteEvent(@event.Event, myContext.WithWriter(myTracesWriter));

    ++myLastTraceVariantEventCount;
  }

  private void HandleMetadataEvent(BxesLogMetadataKeyValueEvent @event)
  {
    var valuesContext = myContext.WithWriter(myValuesWriter);
    if (BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Key, valuesContext)) ++myValuesCount;
    if (BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Value, valuesContext)) ++myValuesCount;

    if (BxesWriteUtils.WriteKeyValuePairIfNeeded(@event.MetadataKeyValue, myContext.WithWriter(myKeyValuesWriter)))
      ++myKeyValuePairsCount;

    BxesWriteUtils.WriteKeyValueIndex(@event.MetadataKeyValue, myContext.WithWriter(myMetadataWriter));

    ++myMetadataValuesCount;
  }

  private void HandleTraceVariantStart(BxesTraceVariantStartEvent @event)
  {
    WriteLastTraceVariantCountIfNeeded();

    myLastTraceVariantCountPosition = myTracesWriter.BaseStream.Position;
    myTracesWriter.Write(@event.TracesCount);
    myTracesWriter.Write((uint)0);

    ++myTracesVariantsCount;
  }

  private void WriteLastTraceVariantCountIfNeeded()
  {
    if (myLastTraceVariantEventCount is null) return;

    BxesWriteUtils.WriteCount(myTracesWriter, myLastTraceVariantCountPosition, myLastTraceVariantEventCount.Value);
    myLastTraceVariantEventCount = null;
  }

  public void Dispose()
  {
    FlushInformation();

    myMetadataWriter.Dispose();
    myKeyValuesWriter.Dispose();
    myValuesWriter.Dispose();
    myTracesWriter.Dispose();
  }

  private void FlushInformation()
  {
    const int CountPos = sizeof(uint);

    BxesWriteUtils.WriteCount(myTracesWriter, CountPos, myTracesVariantsCount);
    BxesWriteUtils.WriteCount(myMetadataWriter, CountPos, myMetadataValuesCount);
    BxesWriteUtils.WriteCount(myValuesWriter, CountPos, myValuesCount);
    BxesWriteUtils.WriteCount(myKeyValuesWriter, CountPos, myKeyValuePairsCount);
  }
}

public abstract class BxesStreamEvent;

public sealed class BxesTraceVariantStartEvent(uint tracesCount) : BxesStreamEvent
{
  public uint TracesCount { get; } = tracesCount;
}

public sealed class BxesEventEvent<TEvent>(TEvent @event) : BxesStreamEvent
  where TEvent : IEvent
{
  public TEvent Event { get; set; } = @event;
}

public sealed class BxesLogMetadataKeyValueEvent(KeyValuePair<BXesStringValue, BxesValue> metadataKeyValue)
  : BxesStreamEvent
{
  public KeyValuePair<BXesStringValue, BxesValue> MetadataKeyValue { get; } = metadataKeyValue;
}