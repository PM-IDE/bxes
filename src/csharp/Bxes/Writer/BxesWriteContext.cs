using Bxes.Models;

namespace Bxes.Writer;

internal readonly struct BxesWriteContext(BinaryWriter binaryWriter)
{
  public BinaryWriter Writer { get; } = binaryWriter;
  public Dictionary<BxesValue, uint> ValuesIndices { get; } = new();
  public Dictionary<KeyValuePair<BXesStringValue, BxesValue>, uint> KeyValueIndices { get; } = new();


  private BxesWriteContext(
    BinaryWriter writer,
    Dictionary<BxesValue, uint> valuesIndices,
    Dictionary<KeyValuePair<BXesStringValue, BxesValue>, uint> keyValueIndices) : this(writer)
  {
    ValuesIndices = valuesIndices;
    KeyValueIndices = keyValueIndices;
  }


  public BxesWriteContext WithWriter(BinaryWriter writer) => new(writer, ValuesIndices, KeyValueIndices);
}