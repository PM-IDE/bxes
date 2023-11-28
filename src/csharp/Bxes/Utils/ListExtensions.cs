namespace Bxes.Utils;

public static class ListExtensions
{
  public static int CalculateHashCode<T>(this List<T> list)
  {
    const int Seed = 487;
    const int Modifier = 31;

    unchecked
    {
      return list.Aggregate(Seed, (current, item) => current * Modifier + item.GetHashCode());
    }
  }
}