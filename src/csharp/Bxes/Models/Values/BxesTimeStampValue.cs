using Bxes.Writer;

namespace Bxes.Models.Values;

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