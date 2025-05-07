using System;

namespace Distribution.Models
{
  public class ActionModel 
  {
    public Action Dismiss { get; set; }
    public Action Run { get; set; }
    public bool Removable { get; set; }
  }
}
