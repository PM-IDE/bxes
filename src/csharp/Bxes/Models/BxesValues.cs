using System.Text;

namespace Bxes.Models;

public abstract class BxesValue
{
  public abstract byte TypeId { get; }
  public abstract void WriteTo(BinaryWriter bw);

  public static BxesValue Parse(BinaryReader reader)
  {
    var valuesOffset = reader.BaseStream.Position;

    var typeId = reader.ReadByte();

    switch (typeId)
    {
      case TypeIds.Bool:
        var value = reader.ReadByte() switch
        {
          0 => false,
          1 => true,
          var other => throw new ParseException(valuesOffset, $"Failed to parse bool, expected 1 or 0, got {other}")
        };

        return new BxesBoolValue(value);
      case TypeIds.I32:
        return new BxesInt32Value(reader.ReadInt32());
      case TypeIds.I64:
        return new BxesInt64Value(reader.ReadInt64());
      case TypeIds.U32:
        return new BxesUint32Value(reader.ReadUInt32());
      case TypeIds.U64:
        return new BxesUint64Value(reader.ReadUInt64());
      case TypeIds.F64:
        return new BxesFloat32Value(reader.ReadSingle());
      case TypeIds.Timestamp:
        return new BxesTimeStampValue(reader.ReadInt64());
      case TypeIds.String:
        var length = reader.ReadUInt64();
        var bytes = new byte[length];
        var read = reader.Read(bytes);
        if (read != bytes.Length)
        {
          var message = $"The string has not enough content byte, expected {length} got {read}";
          throw new ParseException(valuesOffset, message);
        }

        return new BXesStringValue(BxesConstants.BxesEncoding.GetString(bytes));
      case TypeIds.BrafLifecycle:
        return BrafLifecycle.Parse(reader.ReadByte());
      case TypeIds.StandardLifecycle:
        return StandardXesLifecycle.Parse(reader.ReadByte());
    }

    throw new ParseException(valuesOffset, $"Failed to find type for type id {typeId}");
  }
}

public class ParseException(long offset, string message) : BxesException
{
  public override string Message { get; } = $"Failed to parse file at {offset}, {message}";
}

public abstract class BxesValue<TValue>(TValue value) : BxesValue
{
  public TValue Value { get; } = value;


  public override void WriteTo(BinaryWriter bw)
  {
    bw.Write(TypeId);
  }

  public override bool Equals(object? obj) => obj is BxesValue<TValue> otherValue && otherValue.Value.Equals(Value);

  public override int GetHashCode() => Value.GetHashCode();
}