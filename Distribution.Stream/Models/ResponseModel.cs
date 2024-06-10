using System.Net.Http;

namespace Distribution.Stream.Models
{
  public struct ResponseModel<T>
  {
    public T Data { get; set; }
    public string Error { get; set; }
    public HttpResponseMessage Message { get; set; }
  }
}
