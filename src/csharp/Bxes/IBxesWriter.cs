using System.Text;

namespace Bxes;

public interface IBxesWriter
{
  Task WriteAsync(IEventLog log, string savePath);
}

public static class BxesConstants
{
  public const uint BxesVersion = 1;
}

internal struct SingleFileBxesWriteContext
{
  public BinaryWriter Writer { get; }
  public Dictionary<BxesValue, long> ValuesIndices { get; } = new();
  public Dictionary<(BXesStringValue, BxesValue), long> KeyValueIndices { get; } = new();


  public SingleFileBxesWriteContext(BinaryWriter binaryWriter)
  {
    Writer = binaryWriter;
  }
}

public class SingleFileBxesWriter : IBxesWriter
{
  public async Task WriteAsync(IEventLog log, string savePath)
  {
    await using var fs = File.OpenWrite(savePath);
    await using var bw = new BinaryWriter(fs, Encoding.UTF8);

    var context = new SingleFileBxesWriteContext(bw);
    
    WriteBxesVersion(bw, context);
    WriteValues(log, context);
    WriteKeyValuePairs(log, context);
    
  }

  private void WriteBxesVersion(BinaryWriter bw, SingleFileBxesWriteContext context)
  {
    bw.Write(BxesConstants.BxesVersion);
  }

  private void WriteValues(IEventLog log, SingleFileBxesWriteContext context)
  {
    var valuesCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((ulong)0);
    
    ulong count = 0;
    foreach (var trace in log.Traces)
    {
      foreach (var @event in trace.Events)
      {
        WriteEventValues(@event, context, ref count);
      }
    }
    
    WriteCount(context.Writer, valuesCountPosition, count);
  }
  
  private void WriteCount(BinaryWriter writer, long countPos, ulong count)
  {
    var currentPosition = writer.BaseStream.Position;
    
    writer.BaseStream.Seek(countPos, SeekOrigin.Begin);
    writer.Write(count);
    
    writer.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
  }

  private void WriteEventValues(IEvent @event, SingleFileBxesWriteContext context, ref ulong count)
  {
    var nameValue = new BXesStringValue(@event.Name);
    if (!context.ValuesIndices.ContainsKey(nameValue))
    {
      context.ValuesIndices[nameValue] = context.Writer.BaseStream.Position;
      context.Writer.Write(@event.Name);
    }

    var timestampValue = new BxesInt64Value(@event.Timestamp);
    if (!context.ValuesIndices.ContainsKey(timestampValue))
    {
      context.ValuesIndices[timestampValue] = context.Writer.BaseStream.Position;
      context.Writer.Write(timestampValue.Value);
    }

    //todo: lifecycle
    foreach (var (key, value) in @event.Attributes)
    {
      if (!context.ValuesIndices.ContainsKey(key))
      {
        context.ValuesIndices[key] = context.Writer.BaseStream.Position;
        context.Writer.Write(key.Value);
      }

      if (!context.ValuesIndices.ContainsKey(value))
      {
        context.ValuesIndices[value] = context.Writer.BaseStream.Position;
        value.WriteTo(context.Writer);
      }
    }
  }

  private void WriteKeyValuePairs(IEventLog log, SingleFileBxesWriteContext context)
  {
    var keyValueCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((ulong)0);

    ulong count = 0;
    foreach (var trace in log.Traces)
    {
      foreach (var @event in trace.Events)
      {
        WriteEventKeyValuePair(@event, context, ref count);
      }
    }

    WriteCount(context.Writer, keyValueCountPosition, count);
  }

  private void WriteEventKeyValuePair(IEvent @event, SingleFileBxesWriteContext context, ref ulong count)
  {
    foreach (var (key, value) in @event.Attributes)
    {
      var tuple = (key, value);
      if (!context.KeyValueIndices.ContainsKey(tuple))
      {
        context.KeyValueIndices[tuple] = context.Writer.BaseStream.Position;
        context.Writer.Write(context.ValuesIndices[key]);
        context.Writer.Write(context.ValuesIndices[value]);
        ++count;
      }
    }
  }
}

public class MultipleFilesBxesWriter : IBxesWriter
{
  public Task WriteAsync(IEventLog log, string savePath)
  {
    throw new NotImplementedException();
  }
}