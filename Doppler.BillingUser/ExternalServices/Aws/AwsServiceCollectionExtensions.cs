using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Doppler.BillingUser.ExternalServices.Aws
{
    public static class AwsServiceCollectionExtensions
    {
        public static IServiceCollection AddS3Client(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DopplerAwsSettings>(configuration.GetSection(nameof(DopplerAwsSettings)));

            services.AddScoped<IFileStorage, AwsS3Service>();

            return services;
        }
    }
}
