using System;
using System.Reflection;

namespace Distribution.Models
{
  public class ActorModel 
  {
    public Delegate Action { get; set; }
    public MethodInfo Descriptor { get; set; }
  }
}
