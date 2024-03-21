using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Doppler.BillingUser.TimeCollector
{
    public static class TimeCollectorServiceCollectionExtensions
    {
        public static IServiceCollection AddTimeCollector(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TimeCollectorLogWritterSettings>(configuration.GetSection(nameof(TimeCollectorLogWritterSettings)));
            services.AddOptions();

            var serviceProvider = services.BuildServiceProvider();

            var timeCollector = new TimeCollector();

            var timeCollectorLogWritter = new TimeCollectorLogWritter(
                logger: serviceProvider.GetRequiredService<ILogger<TimeCollectorLogWritter>>(),
                options: serviceProvider.GetRequiredService<IOptions<TimeCollectorLogWritterSettings>>(),
                timeCollector: timeCollector
                );

            services.AddSingleton<ITimeCollector>(timeCollector);
            services.AddSingleton(timeCollectorLogWritter);

            return services;
        }
    }
}
