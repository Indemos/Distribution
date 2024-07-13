using System.Collections;
using System.Web;

namespace Distribution.Stream.Extensions
{
  public static class DictionaryExtensions
  {
    public static string Query(this IDictionary input)
    {
      var inputs = HttpUtility.ParseQueryString(string.Empty);

      if (input is not null)
      {
        foreach (DictionaryEntry item in input)
        {
          inputs[$"{item.Key}"] = $"{item.Value}";
        }
      }

      return $"{inputs}";
    }
  }
}
