using System;

namespace Distribution.ModelSpace
{
  public interface IBoxModel
  {
    string Address { get; set; }
    DateTime? Time { get; set; }
  }

  public class BoxModel : IBoxModel
  {
    public virtual string Address { get; set; }
    public virtual DateTime? Time { get; set; }
  }
}
