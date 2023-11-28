using Bxes.Writer;

namespace Bxes.Models.Values.Lifecycle;

public interface IEventLifecycle
{
  public static IEventLifecycle Parse(string value)
  {
    if (Enum.TryParse<StandardLifecycleValues>(value, out var standardLifecycle))
    {
      return new StandardXesLifecycle(standardLifecycle);
    }

    if (Enum.TryParse<BrafLifecycleValues>(value, out var brafLifecycleValues))
    {
      return new BrafLifecycle(brafLifecycleValues);
    }

    return new BrafLifecycle(BrafLifecycleValues.Unspecified);
  }
  
  void WriteTo(BxesWriteContext context);
}