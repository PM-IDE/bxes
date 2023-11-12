namespace Bxes.Models;

public abstract class BxesValue
{
  public abstract byte TypeId { get; }
  public abstract void WriteTo(BinaryWriter bw);
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