using Distribution.Stream;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
  public class Sample
  {
    public string Id { get; set; }
    public UserDataSample UserConfigurations { get; set; }
    public string AccountNumber { get; set; }
    public string Status { get; set; }
    public string Currency { get; set; }
    public double Cash { get; set; }
    public double? AccruedFees { get; set; }
    public bool AccountBlocked { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int Multiplier { get; set; }
    public bool ShortingEnabled { get; set; }
    public double Equity { get; set; }
    public double Balance { get; set; }
    public DateTime? BalanceAsof { get; set; }
  }

  public class UserDataSample
  {
    public string DtbpCheck { get; set; }
    public bool FractionalTrading { get; set; }
    public double MaxMarginMultiplier { get; set; }
    public string TradeConfirmEmail { get; set; }
  }

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

      var res = await service.Send<Demo>(messageStub.Object);

      Assert.Null(res.Data);
      Assert.Equal(e.Message, res.Error);
    }

    [Fact]
    public async Task Deserialize()
    {
      var responseSample = """
      {
        "id": "f703b2e9",
        "demo": 5, 
        "user_configurations": {
          "dtbp_check": "both",
          "fractional_trading": true,
          "max_margin_multiplier": "4",
          "trade_confirm_email": "none"
        },
        "account_number": "555",
        "status": "ACTIVE",
        "currency": "USD",
        "cash": "15.77",
        "accrued_fees": "0",
        "account_blocked": false,
        "created_at": "2020-04-15T08:26:42.566072Z",
        "multiplier": "1",
        "shorting_enabled": false,
        "equity": "259.59",
        "balance": null,
        "balance_asof": "2024-04-15"
      }
      """;

      var clientStub = new Mock<HttpClient>();
      var messageStub = new Mock<HttpRequestMessage>();

      clientStub
        .Setup(o => o.SendAsync(
          It.IsAny<HttpRequestMessage>(),
          It.IsAny<CancellationToken>()
        ))
        .ReturnsAsync(new HttpResponseMessage()
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(responseSample),
        }, TimeSpan.FromSeconds(1))
        .Verifiable();

      var service = new Service
      {
        Client = clientStub.Object
      };

      var res = await service.Send<Sample>(messageStub.Object, service.Options);

      Assert.Null(res.Error);
      Assert.True(res.Data.UserConfigurations.FractionalTrading);
      Assert.Equal(15.77, res.Data.Cash);
      Assert.Equal(0, res.Data.Balance);
      Assert.Equal(DateTime.Parse("2020-04-15T08:26:42.566072Z") + "", res.Data.CreatedAt.Value + "");
    }
  }
}
