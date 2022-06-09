namespace Distribution.ModelSpace
{
  public interface IEnvelopeModel
  {
    string Name { get; set; }
    string Descriptor { get; set; }
    object Message { get; set; }
  }

  public class EnvelopeModel : IEnvelopeModel
  {
    public virtual string Name { get; set; }
    public virtual string Descriptor { get; set; }
    public virtual object Message { get; set; }
  }
}
