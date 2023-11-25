using Bxes.Writer;

namespace Bxes.Models;

public class BxesInt32Value(int value) : BxesValue<int>(value)
{
  public override TypeIds TypeId => TypeIds.I32;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesInt64Value(long value) : BxesValue<long>(value)
{
  public override TypeIds TypeId => TypeIds.I64;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesUint32Value(uint value) : BxesValue<uint>(value)
{
  public override TypeIds TypeId => TypeIds.U32;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesUint64Value(ulong value) : BxesValue<ulong>(value)
{
  public override TypeIds TypeId => TypeIds.U64;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesFloat32Value(float value) : BxesValue<float>(value)
{
  public override TypeIds TypeId => TypeIds.F32;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesFloat64Value(double value) : BxesValue<double>(value)
{
  public override TypeIds TypeId => TypeIds.F64;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesBoolValue(bool value) : BxesValue<bool>(value)
{
  public override TypeIds TypeId => TypeIds.Bool;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesStringValue(string value) : BxesValue<string>(value)
{
  public override TypeIds TypeId => TypeIds.String;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);

    var bytes = BxesConstants.BxesEncoding.GetBytes(value);
    context.Writer.Write((ulong)bytes.Length);
    context.Writer.Write(bytes);
  }
}

public class BxesTimeStampValue(long value) : BxesValue<long>(value)
{
  public override TypeIds TypeId => TypeIds.Timestamp;

  public DateTime Timestamp { get; } = new(value, DateTimeKind.Utc);


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write(Value);
  }
}

public class BxesArtifactItem
{
  public required string Instance { get; init; }
  public required string Transition { get; init; }
}

public class BxesArtifactModelsListValue(List<BxesArtifactItem> items) : BxesValue<List<BxesArtifactItem>>(items)
{
  public override TypeIds TypeId => TypeIds.Artifact;

  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);

    context.Writer.Write((uint)items.Count);
    foreach (var item in items)
    {
      var instanceIndex = context.GetOrWriteValueIndex(new BxesStringValue(item.Instance));
      var transitionIndex = context.GetOrWriteValueIndex(new BxesStringValue(item.Transition));
      
      context.Writer.Write(instanceIndex);
      context.Writer.Write(transitionIndex);
    }
  }
}

public class BxesDriver
{
  public required double Amount { get; init; }
  public required string Name { get; init; }
  public required string Type { get; init; }
}

public class BxesDriversListValue(List<BxesDriver> drivers) : BxesValue<List<BxesDriver>>(drivers)
{
  public override TypeIds TypeId => TypeIds.Drivers;


  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write((uint)drivers.Count);
    
    foreach (var driver in drivers)
    {
      context.Writer.Write(driver.Amount);

      var nameIndex = context.GetOrWriteValueIndex(new BxesStringValue(driver.Name));
      context.Writer.Write(nameIndex);

      var typeIndex = context.GetOrWriteValueIndex(new BxesStringValue(driver.Type));
      context.Writer.Write(typeIndex);
    }
  }
}

public class BxesGuidValue(Guid guid) : BxesValue<Guid>(guid)
{
  public override TypeIds TypeId => TypeIds.Guid;


  public override unsafe void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);

    Span<byte> guidBytes = stackalloc byte[16];
    Value.TryWriteBytes(guidBytes);
    
    context.Writer.Write(guidBytes);
  }
}

public enum SoftwareEventTypeValues : byte
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
  
  public override TypeIds TypeId => TypeIds.SoftwareEventType;

  public override void WriteTo(BxesWriteContext context)
  {
    base.WriteTo(context);
    context.Writer.Write((byte)Value);
  }
}