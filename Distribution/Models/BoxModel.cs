using System;

namespace Distribution.ModelSpace
{
  public interface IBoxModel
  {
    int Port { get; set; }
    string Name { get; set; }
    string Address { get; set; }
    DateTime? Time { get; set; }
  }

  public class BoxModel : IBoxModel
  {
    public virtual int Port { get; set; }
    public virtual string Name { get; set; }
    public virtual string Address { get; set; }
    public virtual DateTime? Time { get; set; }
  }
}
