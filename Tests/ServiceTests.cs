using Distribution.Stream;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
  public class Sample1
  {
    public bool Bool1 { get; set; }
    public bool Bool2 { get; set; }
    public bool Bool3 { get; set; }
    public bool Bool4 { get; set; }
    public bool Bool5 { get; set; }
    public bool Bool6 { get; set; }
    public int Int1 { get; set; }
    public int Int2 { get; set; }
    public int Int3 { get; set; }
    public int Int4 { get; set; }
    public int Int5 { get; set; }
    public int Int6 { get; set; }
    public int Int7 { get; set; }
    public double Double1 { get; set; }
    public double Double2 { get; set; }
    public double Double3 { get; set; }
    public double Double4 { get; set; }
    public double Double5 { get; set; }
    public double Double6 { get; set; }
    public double Double7 { get; set; }
    public string String1 { get; set; }
    public string String2 { get; set; }
    public string String3 { get; set; }
    public string String4 { get; set; }
    public List<int> List1 { get; set; }
    public List<double> List2 { get; set; }
    public int[] Array1 { get; set; }
    public double[] Array2 { get; set; }
    public DateTime Date1 { get; set; }
    public DateTime Date2 { get; set; }
    public DateTime Date3 { get; set; }
    public DateTime Date4 { get; set; }
    public DateTime Date5 { get; set; }
    public UserDataSample UserConfigurations { get; set; }
  }

  public class Sample2
  {
    public bool? Bool1 { get; set; }
    public bool? Bool2 { get; set; }
    public bool? Bool3 { get; set; }
    public bool? Bool4 { get; set; }
    public bool? Bool5 { get; set; }
    public bool? Bool6 { get; set; }
    public int? Int1 { get; set; }
    public int? Int2 { get; set; }
    public int? Int3 { get; set; }
    public int? Int4 { get; set; }
    public int? Int5 { get; set; }
    public int? Int6 { get; set; }
    public int? Int7 { get; set; }
    public double? Double1 { get; set; }
    public double? Double2 { get; set; }
    public double? Double3 { get; set; }
    public double? Double4 { get; set; }
    public double? Double5 { get; set; }
    public double? Double6 { get; set; }
    public double? Double7 { get; set; }
    public string String1 { get; set; }
    public string String2 { get; set; }
    public string String3 { get; set; }
    public string String4 { get; set; }
    public List<int?> List1 { get; set; }
    public List<double?> List2 { get; set; }
    public int?[] Array1 { get; set; }
    public double?[] Array2 { get; set; }
    public DateTime? Date1 { get; set; }
    public DateTime? Date2 { get; set; }
    public DateTime? Date3 { get; set; }
    public DateTime? Date4 { get; set; }
    public DateTime? Date5 { get; set; }
    public UserDataSample UserConfigurations { get; set; }
  }

  public class UserDataSample
  {
    public bool Bool1 { get; set; }
    public int Int1 { get; set; }
    public string String1 { get; set; }
    public string String2 { get; set; }
    public double Double1 { get; set; }
  }

  public class ServiceSchedulerTests
  {
    const string responseSample = """
    {
      "user_configurations": {
        "int1": "1",
        "bool1": true,
        "double1": "4.5",
        "string1": "none",
        "string2": 55.535
          },
      "bool1": true,
      "bool2": false, 
      "bool3": "true",
      "bool4": "false", 
      "bool5": null, 
      "bool6": "none", 
      "int1": "5",
      "int2": 5,
      "int3": "-5",
      "int4": -5,
      "int5": "",
      "int6": "none",
      "int7": null,
      "double1": "5.555",
      "double2": 5.555,
      "double3": "-5.555",
      "double4": -5.555,
      "double5": "",
      "double6": "none",
      "double7": null,
      "string1": 5,
      "string2": 5.5,
      "string3": true,
      "string4": null,
      "date1": "2020-04-15T08:26:42.566072Z",
      "date2": "2020-04-15",
      "date3": "",
      "date4": "none",
      "date5": null,
      "list1": [5, "5", -5, "-5", "none", null],
      "list2": [5.555, "5.555", -5.555, "-5.555", "none", null],
      "array1": [5, "5", -5, "-5", "none", null],
      "array2": [5.555, "5.555", -5.555, "-5.555", "none", null]
    }
    """;

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
    public void Cross()
    {
      var service = new Service();

      var x1 = JsonSerializer.Deserialize<Sample1>(responseSample, service.Options);
      var x2 = JsonSerializer.Deserialize<Sample2>(responseSample, service.Options);
      var x3 = JsonSerializer.Serialize(x1);
      var x4 = JsonSerializer.Serialize(x2);
      var x5 = JsonSerializer.Deserialize<Sample1>(x3, service.Options);
      var x6 = JsonSerializer.Deserialize<Sample1>(x4, service.Options);
      var x7 = JsonSerializer.Deserialize<Sample2>(x3, service.Options);
      var x8 = JsonSerializer.Deserialize<Sample2>(x4, service.Options);
    }

    [Fact]
    public async Task Deserialize()
    {
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

      var res = await service.Send<Sample1>(messageStub.Object, service.Options);

      Assert.Null(res.Error);

      Assert.True(res.Data.UserConfigurations.Bool1);
      Assert.Equal(1, res.Data.UserConfigurations.Int1);
      Assert.Equal(4.5, res.Data.UserConfigurations.Double1);
      Assert.Equal("none", res.Data.UserConfigurations.String1);
      Assert.Equal("55.535", res.Data.UserConfigurations.String2);

      Assert.True(res.Data.Bool1);
      Assert.False(res.Data.Bool2);
      Assert.True(res.Data.Bool3);
      Assert.False(res.Data.Bool4);
      Assert.False(res.Data.Bool5);
      Assert.False(res.Data.Bool6);

      Assert.Equal(5, res.Data.Int1);
      Assert.Equal(5, res.Data.Int2);
      Assert.Equal(-5, res.Data.Int3);
      Assert.Equal(-5, res.Data.Int4);
      Assert.Equal(0, res.Data.Int5);
      Assert.Equal(0, res.Data.Int6);
      Assert.Equal(0, res.Data.Int7);

      Assert.Equal(5.555, res.Data.Double1);
      Assert.Equal(5.555, res.Data.Double2);
      Assert.Equal(-5.555, res.Data.Double3);
      Assert.Equal(-5.555, res.Data.Double4);
      Assert.Equal(0, res.Data.Double5);
      Assert.Equal(0, res.Data.Double6);
      Assert.Equal(0, res.Data.Double7);

      Assert.Equal(DateTime.Parse("2020-04-15T08:26:42.566072Z") + "", res.Data.Date1 + "");
      Assert.Equal(DateTime.Parse("2020-04-15") + "", res.Data.Date2 + "");
      Assert.Equal(default(DateTime) + "", res.Data.Date3 + "");
      Assert.Equal(default(DateTime) + "", res.Data.Date4 + "");
      Assert.Equal(default(DateTime) + "", res.Data.Date5 + "");

      Assert.Equal(string.Join(",", new int[] { 5, 5, -5, -5, 0, 0 }), string.Join(",", res.Data.List1));
      Assert.Equal(string.Join(",", new double[] { 5.555, 5.555, -5.555, -5.555, 0, 0 }), string.Join(",", res.Data.List2));

      Assert.Equal(string.Join(",", new int[] { 5, 5, -5, -5, 0, 0 }), string.Join(",", res.Data.Array1));
      Assert.Equal(string.Join(",", new double[] { 5.555, 5.555, -5.555, -5.555, 0, 0 }), string.Join(",", res.Data.Array2));
    }

    [Fact]
    public async Task DeserializeNulls()
    {
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

      var res = await service.Send<Sample2>(messageStub.Object, service.Options);

      Assert.Null(res.Error);

      Assert.True(res.Data.UserConfigurations.Bool1);
      Assert.Equal(1, res.Data.UserConfigurations.Int1);
      Assert.Equal(4.5, res.Data.UserConfigurations.Double1);
      Assert.Equal("none", res.Data.UserConfigurations.String1);

      Assert.Equal("5", res.Data.String1);
      Assert.Equal("5.5", res.Data.String2);
      Assert.Equal("True", res.Data.String3);
      Assert.Null(res.Data.String4);

      Assert.True(res.Data.Bool1);
      Assert.False(res.Data.Bool2);
      Assert.True(res.Data.Bool3);
      Assert.False(res.Data.Bool4);
      Assert.Null(res.Data.Bool5);
      Assert.False(res.Data.Bool6);

      Assert.Equal(5, res.Data.Int1);
      Assert.Equal(5, res.Data.Int2);
      Assert.Equal(-5, res.Data.Int3);
      Assert.Equal(-5, res.Data.Int4);
      Assert.Equal(0, res.Data.Int5);
      Assert.Equal(0, res.Data.Int6);
      Assert.Null(res.Data.Int7);

      Assert.Equal(5.555, res.Data.Double1);
      Assert.Equal(5.555, res.Data.Double2);
      Assert.Equal(-5.555, res.Data.Double3);
      Assert.Equal(-5.555, res.Data.Double4);
      Assert.Equal(0, res.Data.Double5);
      Assert.Equal(0, res.Data.Double6);
      Assert.Null(res.Data.Double7);

      Assert.Equal(DateTime.Parse("2020-04-15T08:26:42.566072Z") + "", res.Data.Date1 + "");
      Assert.Equal(DateTime.Parse("2020-04-15") + "", res.Data.Date2 + "");
      Assert.Equal(default(DateTime) + "", res.Data.Date3 + "");
      Assert.Equal(default(DateTime) + "", res.Data.Date4 + "");
      Assert.Null(res.Data.Date5);

      Assert.Equal(string.Join(",", new int?[] { 5, 5, -5, -5, 0, null }), string.Join(",", res.Data.List1));
      Assert.Equal(string.Join(",", new double?[] { 5.555, 5.555, -5.555, -5.555, 0, null }), string.Join(",", res.Data.List2));

      Assert.Equal(string.Join(",", new int?[] { 5, 5, -5, -5, 0, null }), string.Join(",", res.Data.Array1));
      Assert.Equal(string.Join(",", new double?[] { 5.555, 5.555, -5.555, -5.555, 0, null }), string.Join(",", res.Data.Array2));
    }
  }
}
