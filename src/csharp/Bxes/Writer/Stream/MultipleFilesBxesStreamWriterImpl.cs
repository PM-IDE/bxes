using Bxes.Models;

namespace Bxes.Writer.Stream;

public class MultipleFilesBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent : IEvent
{
  private readonly uint myBxesVersion;
  private readonly BinaryWriter myMetadataWriter;
  private readonly BinaryWriter myValuesWriter;
  private readonly BinaryWriter myKeyValuesWriter;
  private readonly BinaryWriter myTracesWriter;

  private readonly BxesWriteContext myContext = new(null!);


  private uint myMetadataValuesCount;
  private uint myTracesVariantsCount;

  private uint? myLastTraceVariantEventCount;
  private long myLastTraceVariantCountPosition;


  public MultipleFilesBxesStreamWriterImpl(string savePath, uint bxesVersion)
  {
    if (!Directory.Exists(savePath)) throw new SavePathIsNotDirectoryException(savePath);

    BinaryWriter OpenWrite(string fileName)
    {
      var path = Path.Join(savePath, fileName);
      if (File.Exists(path))
      {
        File.Delete(path);
      }

      return new BinaryWriter(File.OpenWrite(path));
    }

    myBxesVersion = bxesVersion;
    myMetadataWriter = OpenWrite(BxesConstants.MetadataFileName);
    myValuesWriter = OpenWrite(BxesConstants.ValuesFileName);
    myKeyValuesWriter = OpenWrite(BxesConstants.KVPairsFileName);
    myTracesWriter = OpenWrite(BxesConstants.TracesFileName);

    WriteInitialInfo();
  }

  private void WriteInitialInfo()
  {
    myTracesWriter.Write(myBxesVersion);
    myMetadataWriter.Write(myBxesVersion);
    myKeyValuesWriter.Write(myBxesVersion);
    myValuesWriter.Write(myBxesVersion);

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
    BxesWriteUtils.WriteEventValues(@event.Event, myContext.WithWriter(myValuesWriter));
    BxesWriteUtils.WriteEventKeyValuePairs(@event.Event, myContext.WithWriter(myKeyValuesWriter));
    BxesWriteUtils.WriteEvent(@event.Event, myContext.WithWriter(myTracesWriter));

    ++myLastTraceVariantEventCount;
  }

  private void HandleMetadataEvent(BxesLogMetadataKeyValueEvent @event)
  {
    var valuesContext = myContext.WithWriter(myValuesWriter);
    BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Key, valuesContext);
    BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Value, valuesContext);

    BxesWriteUtils.WriteKeyValuePairIfNeeded(@event.MetadataKeyValue, myContext.WithWriter(myKeyValuesWriter));

    BxesWriteUtils.WriteKeyValueIndex(@event.MetadataKeyValue, myContext.WithWriter(myMetadataWriter));

    ++myMetadataValuesCount;
  }

  private void HandleTraceVariantStart(BxesTraceVariantStartEvent @event)
  {
    WriteLastTraceVariantCountIfNeeded();

    myLastTraceVariantEventCount = 0;
    myTracesWriter.Write(@event.TracesCount);
    myLastTraceVariantCountPosition = myTracesWriter.BaseStream.Position;
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
    WriteLastTraceVariantCountIfNeeded();

    const int CountPos = sizeof(uint);

    BxesWriteUtils.WriteCount(myTracesWriter, CountPos, myTracesVariantsCount);
    BxesWriteUtils.WriteCount(myMetadataWriter, CountPos, myMetadataValuesCount);
    BxesWriteUtils.WriteCount(myValuesWriter, CountPos, (uint)myContext.ValuesIndices.Count);
    BxesWriteUtils.WriteCount(myKeyValuesWriter, CountPos, (uint)myContext.KeyValueIndices.Count);
  }
}