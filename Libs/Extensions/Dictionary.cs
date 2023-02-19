using System.Collections.Generic;

namespace Distribution.ExtensionSpace
{
  public static class DictionaryExtensions
  {
    public static V Get<K, V>(this IDictionary<K, V> input, K index)
    {
      return input.TryGetValue(index, out var value) ? value : default;
    }
  }
}
