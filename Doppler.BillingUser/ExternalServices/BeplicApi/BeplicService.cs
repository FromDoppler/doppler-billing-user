using Doppler.BillingUser.Authorization;
using Flurl.Http.Configuration;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Tavis.UriTemplates;
using Doppler.BillingUser.ExternalServices.BeplicApi.Responses;
using System.Linq;

namespace Doppler.BillingUser.ExternalServices.BeplicApi
{
    public class BeplicService : IBeplicService
    {
        private readonly IOptions<BeplicSettings> _options;
        private readonly ILogger<BeplicService> _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public BeplicService(
            IOptions<BeplicSettings> options,
            ILogger<BeplicService> logger,
            IFlurlClientFactory flurlClientFactory,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task AssignPlanToUser(int userId, string planName)
        {
            try
            {
                var plan = await GetBeplicPlanByName(planName) ?? throw new Exception($"Beplic plan '{planName}' not found");

                await AssignBeplicPlanToCustomer(userId, plan.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                throw;
            }
        }

        public async Task UnassignPlanToUser(int userId)
        {
            try
            {
                await UnassignBeplicPlanToCustomer(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                throw;
            }
        }

        private async Task<PlanResponse> GetBeplicPlanByName(string planName)
        {
            var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/plan")
                .Resolve())
                .AllowHttpStatus("4xx")
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .GetAsync();

            if (response.ResponseMessage.IsSuccessStatusCode)
            {
                var plans = await response.GetJsonAsync<List<PlanResponse>>();

                return plans.Where(x => x.Name.Equals(planName)).FirstOrDefault();
            }

            string message = await response.ResponseMessage.Content.ReadAsStringAsync();
            throw new Exception(!string.IsNullOrWhiteSpace(message) ? message : "Error getting plans");
        }

        private async Task AssignBeplicPlanToCustomer(int userId, int idBeplicPlan)
        {
            var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + "/plan/customer")
                .Resolve())
                .AllowHttpStatus("4xx")
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PostJsonAsync(new { idExternal = userId.ToString(), idPlan = idBeplicPlan.ToString() })
                .ReceiveJson<PlanAssignResponse>();

            if (!response.Success)
            {
                throw new Exception(!string.IsNullOrWhiteSpace(response?.Error) ? response.Error : "Error assigning the plan to the user");
            }
        }

        private async Task UnassignBeplicPlanToCustomer(int userId)
        {
            var response = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/plan/customer/{userId}/cancellation")
                .Resolve())
                .AllowHttpStatus("4xx")
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PutAsync()
                .ReceiveJson<PlanUnassignResponse>();

            if (!response.Success)
            {
                throw new Exception(!string.IsNullOrWhiteSpace(response?.Error) ? response.Error : "Error assigning the plan to the user");
            }
        }
    }
}
