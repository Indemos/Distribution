using System;
using System.Reflection;

namespace Distribution.ModelSpace
{
  public interface IRouteModel
  {
    Delegate Creator { get; set; }
    Delegate Processor { get; set; }
    MethodInfo Descriptor { get; set; }
  }

  public class RouteModel : IRouteModel
  {
    public virtual Delegate Creator { get; set; }
    public virtual Delegate Processor { get; set; }
    public virtual MethodInfo Descriptor { get; set; }
  }
}
