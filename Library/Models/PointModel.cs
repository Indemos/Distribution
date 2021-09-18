namespace Distribution.ModelSpace
{
  public interface IPointModel
  {
    int Port { get; set; }
    string Name { get; set; }
    string Address { get; set; }
    DateTime? Time { get; set; }
  }

  public class PointModel : IPointModel
  {
    public int Port { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public DateTime? Time { get; set; }
  }
}