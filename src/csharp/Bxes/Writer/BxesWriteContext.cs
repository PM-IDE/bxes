using Bxes.Models;

namespace Bxes.Writer;

public record AttributeKeyValue(BxesStringValue Key, BxesValue Value);

public readonly struct BxesWriteContext(BinaryWriter binaryWriter)
{
  public BinaryWriter Writer { get; } = binaryWriter;
  public Dictionary<BxesValue, uint> ValuesIndices { get; } = new();
  public Dictionary<AttributeKeyValue, uint> KeyValueIndices { get; } = new();


  private BxesWriteContext(
    BinaryWriter writer,
    Dictionary<BxesValue, uint> valuesIndices,
    Dictionary<AttributeKeyValue, uint> keyValueIndices) : this(writer)
  {
    ValuesIndices = valuesIndices;
    KeyValueIndices = keyValueIndices;
  }


  public BxesWriteContext WithWriter(BinaryWriter writer) => new(writer, ValuesIndices, KeyValueIndices);

  public uint GetOrWriteValueIndex(BxesValue value)
  {
    BxesWriteUtils.WriteValueIfNeeded(value, this);
    return ValuesIndices[value];
  }
}