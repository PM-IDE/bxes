using Bxes.Models;
using Bxes.Utils;

namespace Bxes.Writer.Stream;

public class MultipleFilesBxesStreamWriterImpl<TEvent> : IBxesStreamWriter where TEvent : IEvent
{
  private readonly uint myBxesVersion;
  private readonly BinaryWriter myMetadataWriter;
  private readonly BinaryWriter myValuesWriter;
  private readonly BinaryWriter myKeyValuesWriter;
  private readonly BinaryWriter myTracesWriter;
  private readonly IEventLogMetadata myMetadata = new EventLogMetadata();

  private readonly BxesWriteContext myContext = new(null!);


  private uint myTracesVariantsCount;

  private uint? myLastTraceVariantEventCount;
  private long myLastTraceVariantCountPosition;


  public MultipleFilesBxesStreamWriterImpl(string savePath, uint bxesVersion)
  {
    if (!Directory.Exists(savePath)) throw new SavePathIsNotDirectoryException(savePath);

    BinaryWriter OpenWrite(string fileName)
    {
      var path = Path.Join(savePath, fileName);
      PathUtil.EnsureDeleted(path);

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
      case BxesKeyValueEvent metadataEvent:
        HandleKeyValueEvent(metadataEvent);
        break;
      case BxesValueEvent valueEvent:
        HandleValueEvent(valueEvent);
        break;
      case BxesLogMetadataGlobalEvent globalEvent:
        myMetadata.Globals.Add(globalEvent.Global);
        break;
      case BxesLogMetadataAttributeEvent attributeEvent:
        myMetadata.Metadata.Add(attributeEvent.Attribute);
        break;
      case BxesLogMetadataClassifierEvent classifierEvent:
        myMetadata.Classifiers.Add(classifierEvent.Classifier);
        break;
      case BxesLogMetadataExtensionEvent extensionEvent:
        myMetadata.Extensions.Add(extensionEvent.Extensions);
        break;
      case BxesLogMetadataPropertyEvent propertyEvent:
        myMetadata.Properties.Add(propertyEvent.Attribute);
        break;
      case BxesTraceVariantStartEvent variantStartEvent:
        HandleTraceVariantStart(variantStartEvent);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(@event));
    }
  }

  private void HandleValueEvent(BxesValueEvent valueEvent)
  {
    BxesWriteUtils.WriteValueIfNeeded(valueEvent.Value, myContext.WithWriter(myValuesWriter));
  }

  private void HandleEventEvent(BxesEventEvent<TEvent> @event)
  {
    BxesWriteUtils.WriteEventValues(@event.Event, myContext.WithWriter(myValuesWriter));
    BxesWriteUtils.WriteEventKeyValuePairs(@event.Event, myContext.WithWriter(myKeyValuesWriter));
    BxesWriteUtils.WriteEvent(@event.Event, myContext.WithWriter(myTracesWriter));

    ++myLastTraceVariantEventCount;
  }

  private void HandleKeyValueEvent(BxesKeyValueEvent @event)
  {
    var valuesContext = myContext.WithWriter(myValuesWriter);
    BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Key, valuesContext);
    BxesWriteUtils.WriteValueIfNeeded(@event.MetadataKeyValue.Value, valuesContext);

    BxesWriteUtils.WriteKeyValuePairIfNeeded(@event.MetadataKeyValue, myContext.WithWriter(myKeyValuesWriter));
  }

  private void HandleTraceVariantStart(BxesTraceVariantStartEvent @event)
  {
    WriteLastTraceVariantCountIfNeeded();

    myLastTraceVariantEventCount = 0;
    myTracesWriter.Write(@event.TracesCount);

    foreach (var pair in @event.Metadata)
    {
      BxesWriteUtils.WriteValueIfNeeded(pair.Key, myContext.WithWriter(myValuesWriter));
      BxesWriteUtils.WriteValueIfNeeded(pair.Value, myContext.WithWriter(myValuesWriter));
      
      BxesWriteUtils.WriteKeyValuePairIfNeeded(pair, myContext.WithWriter(myKeyValuesWriter));
    }

    BxesWriteUtils.WriteVariantMetadata(@event.Metadata, myContext.WithWriter(myTracesWriter));

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
    WriteMetadata();
    WriteLastTraceVariantCountIfNeeded();

    const int CountPos = sizeof(uint);

    BxesWriteUtils.WriteCount(myTracesWriter, CountPos, myTracesVariantsCount);
    BxesWriteUtils.WriteCount(myValuesWriter, CountPos, (uint)myContext.ValuesIndices.Count);
    BxesWriteUtils.WriteCount(myKeyValuesWriter, CountPos, (uint)myContext.KeyValueIndices.Count);
  }

  private void WriteMetadata()
  {
    foreach (var value in myMetadata.EnumerateValues())
    {
      BxesWriteUtils.WriteValueIfNeeded(value, myContext.WithWriter(myValuesWriter));
    }

    foreach (var kv in myMetadata.EnumerateKeyValuePairs())
    {
      BxesWriteUtils.WriteKeyValuePairIfNeeded(kv, myContext.WithWriter(myKeyValuesWriter));
    }

    BxesWriteUtils.WriteEventLogMetadata(myMetadata, myContext.WithWriter(myMetadataWriter));
  }
}