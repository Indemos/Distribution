namespace Distribution.Stream.Models
{
  public struct ResponseModel<T>
  {
    public T Data { get; set; }
    public string Error { get; set; }

  }
}
