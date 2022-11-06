using System.Collections.Generic;

namespace Common
{
  public struct OperationResponse
  {
    public double Value { get; set; }
    public string Operation { get; set; }
    public IList<string> History { get; set; }
  }
}
