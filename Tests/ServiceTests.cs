using Distribution.Service;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
  public class ServiceSchedulerTests
  {
    private class Demo
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    [Fact]
    public async Task SendSuccess()
    {
      var clientStub = new Mock<HttpClient>();
      var messageStub = new Mock<HttpRequestMessage>();
      var expectation = new Demo
      {
        Id = 5,
        Name = nameof(Demo)
      };

      clientStub
        .Setup(o => o.SendAsync(
          It.IsAny<HttpRequestMessage>(),
          It.IsAny<CancellationToken>()
        ))
        .ReturnsAsync(new HttpResponseMessage()
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(JsonSerializer.Serialize(expectation)),
        }, TimeSpan.FromSeconds(1))
        .Verifiable();

      var service = new Service
      {
        Client = clientStub.Object
      };

      var res = await service.Send<Demo>(messageStub.Object);

      Assert.Null(res.Error);
      Assert.Equal(JsonSerializer.Serialize(expectation), JsonSerializer.Serialize(res.Data));
    }

    [Fact]
    public async Task Send()
    {
      var e = new Exception("Demo");
      var clientStub = new Mock<HttpClient>();
      var messageStub = new Mock<HttpRequestMessage>();
      var expectation = new Demo
      {
        Id = 5,
        Name = nameof(Demo)
      };

      clientStub
        .Setup(o => o.SendAsync(
          It.IsAny<HttpRequestMessage>(),
          It.IsAny<CancellationToken>()
        ))
        .ThrowsAsync(e, TimeSpan.FromMicroseconds(1))
        .Verifiable();

      var service = new Service
      {
        Client = clientStub.Object
      };

      await Assert.ThrowsAsync<Exception>(async () => await service.Send<Demo>(messageStub.Object));
    }
  }
}
