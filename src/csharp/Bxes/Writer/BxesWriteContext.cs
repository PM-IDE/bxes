using Bxes.Models;

namespace Bxes.Writer;

internal readonly struct BxesWriteContext(BinaryWriter binaryWriter)
{
  public BinaryWriter Writer { get; } = binaryWriter;
  public Dictionary<BxesValue, long> ValuesIndices { get; } = new();
  public Dictionary<KeyValuePair<BXesStringValue, BxesValue>, long> KeyValueIndices { get; } = new();


  private BxesWriteContext(
    BinaryWriter writer,
    Dictionary<BxesValue, long> valuesIndices,
    Dictionary<KeyValuePair<BXesStringValue, BxesValue>, long> keyValueIndices) : this(writer)
  {
    ValuesIndices = valuesIndices;
    KeyValueIndices = keyValueIndices;
  }


  public BxesWriteContext WithWriter(BinaryWriter writer) => new(writer, ValuesIndices, KeyValueIndices);
}