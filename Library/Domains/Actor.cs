namespace Distribution.DomainSpace
{
  public interface IActor : IDisposable
  {
    IScene Galaxy { get; set; }
  }

  public abstract class Actor : IActor
  {
    public virtual IScene Galaxy { get; set; }

    public virtual void Dispose()
    {
    }
  }
}