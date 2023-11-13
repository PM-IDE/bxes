using Bxes.Models;

namespace Bxes.Writer;

public interface IBxesStreamWriter : IDisposable
{
  void HandleEvent(BxesStreamEvent @event);
}

public class MultipleFilesBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent: IEvent
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
      case BXesEventEvent<TEvent> eventEvent:
        HandleEventEvent(eventEvent);
        break;
      case BxesLogMetadataKeyValueEvent metadataEvent:
        HandleMetadataEvent(metadataEvent);
        break;
      case BXesTraceVariantStartEvent variantStartEvent:
        HandleTraceVariantStart(variantStartEvent);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(@event));
    }
  }

  private void HandleEventEvent(BXesEventEvent<TEvent> @event)
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

  private void HandleTraceVariantStart(BXesTraceVariantStartEvent @event)
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

public sealed class BXesTraceVariantStartEvent(uint tracesCount) : BxesStreamEvent
{
  public uint TracesCount { get; } = tracesCount;
}

public sealed class BXesEventEvent<TEvent>(TEvent @event) : BxesStreamEvent
  where TEvent : IEvent
{
  public TEvent Event { get; set; } = @event;
}

public sealed class BxesLogMetadataKeyValueEvent(KeyValuePair<BXesStringValue, BxesValue> metadataKeyValue) : BxesStreamEvent
{
  public KeyValuePair<BXesStringValue, BxesValue> MetadataKeyValue { get; } = metadataKeyValue;
}