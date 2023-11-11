using System.Text;

namespace Bxes;

using IndexType = uint;

public interface IBxesWriter
{
  Task WriteAsync(IEventLog log, string savePath);
}

public static class BxesConstants
{
  public const IndexType BxesVersion = 1;
}

internal readonly struct SingleFileBxesWriteContext(BinaryWriter binaryWriter)
{
  public BinaryWriter Writer { get; } = binaryWriter;
  public Dictionary<BxesValue, long> ValuesIndices { get; } = new();
  public Dictionary<(BXesStringValue, BxesValue), long> KeyValueIndices { get; } = new();
}

public class SingleFileBxesWriter : IBxesWriter
{
  public async Task WriteAsync(IEventLog log, string savePath)
  {
    await using var fs = File.OpenWrite(savePath);
    await using var bw = new BinaryWriter(fs, Encoding.UTF8);

    var context = new SingleFileBxesWriteContext(bw);
    
    WriteBxesVersion(bw);
    WriteValues(log, context);
    WriteKeyValuePairs(log, context);
  }

  private void WriteBxesVersion(BinaryWriter bw)
  {
    bw.Write(BxesConstants.BxesVersion);
  }

  private void WriteValues(IEventLog log, SingleFileBxesWriteContext context)
  {
    var valuesCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);
    
    IndexType count = 0;
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

  private void WriteEventValues(IEvent @event, SingleFileBxesWriteContext context, ref IndexType count)
  {
    var nameValue = new BXesStringValue(@event.Name);
    if (!context.ValuesIndices.ContainsKey(nameValue))
    {
      context.ValuesIndices[nameValue] = context.Writer.BaseStream.Position;
      context.Writer.Write(@event.Name);
      ++count;
    }

    //todo: lifecycle
    foreach (var (key, value) in @event.Attributes)
    {
      if (!context.ValuesIndices.ContainsKey(key))
      {
        context.ValuesIndices[key] = context.Writer.BaseStream.Position;
        context.Writer.Write(key.Value);
        ++count;
      }

      if (!context.ValuesIndices.ContainsKey(value))
      {
        context.ValuesIndices[value] = context.Writer.BaseStream.Position;
        value.WriteTo(context.Writer);
        ++count;
      }
    }
  }

  private void WriteKeyValuePairs(IEventLog log, SingleFileBxesWriteContext context)
  {
    var keyValueCountPosition = context.Writer.BaseStream.Position;
    context.Writer.Write((IndexType)0);

    IndexType count = 0;
    foreach (var trace in log.Traces)
    {
      foreach (var @event in trace.Events)
      {
        WriteEventKeyValuePair(@event, context, ref count);
      }
    }

    WriteCount(context.Writer, keyValueCountPosition, count);
  }

  private void WriteEventKeyValuePair(IEvent @event, SingleFileBxesWriteContext context, ref IndexType count)
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