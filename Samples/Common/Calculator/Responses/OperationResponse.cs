using System.Collections.Generic;

namespace Common
{
  public class OperationResponse
  {
    public double Value { get; set; }
    public string Operation { get; set; }
    public IList<string> History { get; set; }
  }
}
