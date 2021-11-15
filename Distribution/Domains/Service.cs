using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.CookiePolicy;
using Distribution.DomainSpace;
using Distribution.CommunicatorSpace;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Net;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Distribution.DomainSpace
{
  public interface IService : IDisposable
  {
    /// <summary>
    /// Port for communication
    /// </summary>
    int Port { get; set; }

    /// <summary>
    /// Route intercepting incoming queries
    /// </summary>
    string Route { get; set; }

    /// <summary>
    /// Communication protocol
    /// </summary>
    ICommunicator Communicator { get; set; }

    /// <summary>
    /// Service endpoint and port data
    /// </summary>
    IEnumerable<UriBuilder> Addresses { get; set; }

    /// <summary>
    /// Start the server
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    Task Run();
  }

  public class Service : IService
  {
    /// <summary>
    /// Port for communication
    /// </summary>
    public virtual int Port { get; set; }

    /// <summary>
    /// Route for communication
    /// </summary>
    public virtual string Route { get; set; }

    /// <summary>
    /// Communication protocol
    /// </summary>
    public virtual ICommunicator Communicator { get; set; }

    /// <summary>
    /// Service endpoint and port data
    /// </summary>
    public virtual IEnumerable<UriBuilder> Addresses { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Service()
    {
      Port = 0;
      Route = "/messages";
    }

    /// <summary>
    /// Start the server
    /// </summary>
    /// <returns></returns>
    public virtual Task Run()
    {
      var configuration = new ConfigurationBuilder().Build();

      var urls = new[]
      {
        $"http://0.0.0.0:{ Port }"
      };

      var environment = Host
        .CreateDefaultBuilder(new string[0])
        .ConfigureWebHostDefaults(options =>
        {
          options
            .UseConfiguration(configuration)
            .UseUrls(urls)
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseKestrel()
            .Configure(o => ConfigureApplication(o));
        })
        .ConfigureServices(o => ConfigureServices(o));

      return environment.Build().RunAsync();
    }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
      Communicator?.Dispose();
    }

    /// <summary>
    /// Configure services
    /// </summary>
    /// <param name="services"></param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
      services.AddDistributedMemoryCache();

      services
        .AddCors(o => o.AddDefaultPolicy(builder => builder
          .AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod()));

      services.AddSession(o =>
      {
        o.IdleTimeout = TimeSpan.FromSeconds(10);
        o.Cookie.IsEssential = true;
        o.Cookie.HttpOnly = true;
      });

      services.AddControllers(o => o.RespectBrowserAcceptHeader = true);
    }

    /// <summary>
    /// Configure application
    /// </summary>
    /// <param name="app"></param>
    protected virtual void ConfigureApplication(IApplicationBuilder app)
    {
      app.UseDeveloperExceptionPage();

      app
        .UseCors(o => o
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());

      app.UseSession();
      app.UseCookiePolicy(new CookiePolicyOptions
      {
        MinimumSameSitePolicy = SameSiteMode.Strict,
        Secure = CookieSecurePolicy.Always,
        HttpOnly = HttpOnlyPolicy.Always
      });

      Communicator.Connect();
      Communicator.Subscribe(app, Route);

      app.UseStaticFiles();
      app.UseRouting();
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });

      app
        .ApplicationServices
        .GetService<IHostApplicationLifetime>()
        .ApplicationStarted.Register(() =>
        {
          Addresses = app
            .ServerFeatures
            .Get<IServerAddressesFeature>()
            .Addresses
            .Select(o => new UriBuilder(o, Dns.GetHostName()));
        });
    }
  }
}
