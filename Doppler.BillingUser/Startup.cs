using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.ExternalServices.Slack;
using Flurl.Http.Configuration;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Doppler.BillingUser.Settings;
using Doppler.BillingUser.ExternalServices.StaticDataCllient;
using Doppler.BillingUser.ExternalServices.Aws;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.TimeCollector;
using Doppler.BillingUser.ExternalServices.BeplicApi;

namespace Doppler.BillingUser
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<DopplerDatabaseSettings>(Configuration.GetSection(nameof(DopplerDatabaseSettings)));
            services.Configure<RelayEmailSenderSettings>(Configuration.GetSection(nameof(RelayEmailSenderSettings)));
            services.Configure<EmailNotificationsConfiguration>(Configuration.GetSection(nameof(EmailNotificationsConfiguration)));
            services.AddDopplerSecurity();
            services.AddRepositories();
            services.AddControllers();
            services.AddCors();
            services.AddSingleton<Weather.WeatherForecastService>();
            services.AddSingleton<Weather.DataService>();
            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter the token into field as 'Bearer {token}'",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme },
                            },
                            Array.Empty<string>()
                        }
                    });

                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Doppler.BillingUser", Version = "v1" });

                var baseUrl = Configuration.GetValue<string>("BaseURL");
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    c.AddServer(new OpenApiServer() { Url = baseUrl });
                }
            });

            services.Configure<EncryptionSettings>(Configuration.GetSection(nameof(EncryptionSettings)));
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.Configure<FirstDataSettings>(Configuration.GetSection(nameof(FirstDataSettings)));
            services.AddScoped<IFirstDataService, FirstDataService>();
            services.AddScoped<IPaymentGateway, PaymentGateway>();
            services.AddScoped<IValidator<BillingInformation>, BillingInformationValidator>();
            services.AddScoped<IValidator<AgreementInformation>, AgreementInformationValidator>();
            services.Configure<SapSettings>(Configuration.GetSection(nameof(SapSettings)));
            services.AddScoped<ISapService, SapService>();
            services.AddTransient<JwtSecurityTokenHandler>();
            services.Configure<JwtOptions>(Configuration.GetSection(nameof(JwtOptions)));
            services.AddJwtToken();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddHttpClient();
            services.AddScoped<IAccountPlansService, AccountPlansService>();
            services.Configure<AccountPlansSettings>(Configuration.GetSection(nameof(AccountPlansSettings)));
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentRequestApiTokenGetter, CurrentRequestApiTokenGetter>();
            services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
            services.AddScoped<IPromotionRepository, PromotionRepository>();
            services.Configure<SlackSettings>(Configuration.GetSection(nameof(SlackSettings)));
            services.AddTransient<IEmailSender, RelayEmailSender>();
            services.AddScoped<ISlackService, SlackService>();
            services.Configure<ZohoSettings>(Configuration.GetSection(nameof(ZohoSettings)));
            services.AddScoped<IZohoService, ZohoService>();
            services.AddScoped<IEmailTemplatesService, EmailTemplatesService>();
            services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            services.Configure<MercadoPagoSettings>(Configuration.GetSection(nameof(MercadoPagoSettings)));
            services.AddScoped<IMercadoPagoService, MercadoPagoService>();
            services.AddScoped<IPaymentAmountHelper, PaymentAmounthelper>();
            services.AddScoped<IUserPaymentHistoryRepository, UserPaymentHistoryRepository>();
            services.Configure<AttemptsToUpdateSettings>(Configuration.GetSection(nameof(AttemptsToUpdateSettings)));
            services.Configure<StaticDataClientSettings>(Configuration.GetSection(nameof(StaticDataClient)));
            services.AddScoped<IStaticDataClient, StaticDataClient>();
            services.Configure<CloverSettings>(Configuration.GetSection(nameof(CloverSettings)));
            services.AddScoped<ICloverService, CloverService>();
            services.AddS3Client(Configuration);
            services.AddTimeCollector(Configuration);
            services.AddScoped<ILandingPlanUserRepository, LandingPlanUserRepository>();
            services.AddScoped<IUserAddOnRepository, UserAddOnRepository>();
            services.AddScoped<ILandingPlanRepository, LandingPlanRepository>();
            services.Configure<BeplicSettings>(Configuration.GetSection(nameof(BeplicSettings)));
            services.AddScoped<IBeplicService, BeplicService>();
            services.AddScoped<IChatPlanRepository, ChatPlanRepository>();
            services.AddScoped<IChatPlanUserRepository, ChatPlanUserRepository>();
            services.AddScoped<IClientManagerRepository, ClientManagerRepository>();
            services.AddScoped<IOnSitePlanUserRepository, OnSitePlanUserRepository>();
            services.AddScoped<IOnSitePlanRepository, OnSitePlanRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Doppler.BillingUser v1"));

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(policy => policy
                .SetIsOriginAllowed(isOriginAllowed: _ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
