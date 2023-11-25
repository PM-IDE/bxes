namespace Bxes.Models;

public class BxesInt32Value(int value) : BxesValue<int>(value)
{
  public override byte TypeId => TypeIds.I32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesInt64Value(long value) : BxesValue<long>(value)
{
  public override byte TypeId => TypeIds.I64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesUint32Value(uint value) : BxesValue<uint>(value)
{
  public override byte TypeId => TypeIds.U32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesUint64Value(ulong value) : BxesValue<ulong>(value)
{
  public override byte TypeId => TypeIds.U64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesFloat32Value(float value) : BxesValue<float>(value)
{
  public override byte TypeId => TypeIds.F32;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesFloat64Value(double value) : BxesValue<double>(value)
{
  public override byte TypeId => TypeIds.F64;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesBoolValue(bool value) : BxesValue<bool>(value)
{
  public override byte TypeId => TypeIds.Bool;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
  }
}

public class BxesStringValue(string value) : BxesValue<string>(value)
{
  public override byte TypeId => TypeIds.String;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);

    var bytes = BxesConstants.BxesEncoding.GetBytes(value);
    bw.Write((ulong)bytes.Length);
    bw.Write(bytes);
  }
}

public class BxesTimeStampValue(long value) : BxesValue<long>(value)
{
  public override byte TypeId => TypeIds.Timestamp;

  public DateTime Timestamp { get; } = new(value, DateTimeKind.Utc);


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(value);
  }
}

public class BxesArtifactItem
{
  public required string Instance { get; init; }
  public required string Transition { get; init; }
}

public class BxesArtifactModelsListValue(List<BxesArtifactItem> items) : BxesValue<List<BxesArtifactItem>>(items)
{
  public override byte TypeId => TypeIds.Artifact;
}

public class BxesDriver
{
  public required double Amount { get; init; }
  public required string Name { get; init; }
  public required string Type { get; init; }
}

public class BxesDriversListValue(List<BxesDriver> drivers) : BxesValue<List<BxesDriver>>(drivers)
{
  public override byte TypeId => TypeIds.Drivers;
}

public class BxesGuidValue(Guid guid) : BxesValue<Guid>(guid)
{
  public override byte TypeId => TypeIds.Guid;
}

public enum SoftwareEventTypeValues
{ 
  Unspecified = 0,
  Call = 1,
  Return = 2,
  Throws = 3,
  Handle = 4,
  Calling = 5,
  Returning = 6,
}

public class BxesSoftwareEventTypeValue(SoftwareEventTypeValues values) : BxesValue<SoftwareEventTypeValues>(values)
{
  public static BxesSoftwareEventTypeValue Parse(byte value) => Enum.IsDefined(typeof(SoftwareEventTypeValues), value) switch
  {
    true => new BxesSoftwareEventTypeValue((SoftwareEventTypeValues)value),
    false => throw new IndexOutOfRangeException()
  };
  
  public override byte TypeId => TypeIds.SoftwareEventType;
}