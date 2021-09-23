namespace Distribution.ModelSpace
{
  public interface IMessageModel
  {
    string Name { get; set; }
    string Descriptor { get; set; }
    object Message { get; set; }
  }

  public class MessageModel : IMessageModel
  {
    public virtual string Name { get; set; }
    public virtual string Descriptor { get; set; }
    public virtual object Message { get; set; }
  }
}
