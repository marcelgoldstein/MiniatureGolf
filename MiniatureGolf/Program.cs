using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MiniatureGolf;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("hosting.json", optional: false)
            .Build();

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                _ = webBuilder.UseConfiguration(config);
                _ = webBuilder.UseStartup<Startup>();
            });

        return builder;
    }
}
