using System;
using System.Reflection;

namespace Distribution.ModelSpace
{
  public struct ActorModel 
  {
    public Delegate Action { get; set; }
    public MethodInfo Descriptor { get; set; }
  }
}
