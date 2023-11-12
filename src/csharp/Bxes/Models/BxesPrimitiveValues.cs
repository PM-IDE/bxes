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

public class BXesStringValue(string value) : BxesValue<string>(value)
{
  public override byte TypeId => TypeIds.String;


  public override void WriteTo(BinaryWriter bw)
  {
    base.WriteTo(bw);
    bw.Write(Value);
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