using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TelegramBotHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.Development.json",
                    optional: true,
                    reloadOnChange: true);
                config.AddJsonFile("appsettings.json",
                    optional: true,
                    reloadOnChange: true)
                .AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults((webBuilder) =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
