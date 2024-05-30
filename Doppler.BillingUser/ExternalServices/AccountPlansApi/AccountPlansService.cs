using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public class AccountPlansService : IAccountPlansService
    {
        private readonly IOptions<AccountPlansSettings> _options;
        private readonly ILogger _logger;
        //private readonly IFlurlClient _flurlClient;
        private readonly ICurrentRequestApiTokenGetter _usersApiTokenGetter;
        private readonly ITimeCollector _timeCollector;

        public AccountPlansService(
            IOptions<AccountPlansSettings> options,
            ILogger<AccountPlansService> logger,
            //IFlurlClientFactory flurlClientFac,
            ICurrentRequestApiTokenGetter usersApiTokenGetter,
            ITimeCollector timeCollector)
        {
            _options = options;
            _logger = logger;
            //_flurlClient = flurlClientFac.Get(_options.Value.CalculateUrlTemplate);
            _usersApiTokenGetter = usersApiTokenGetter;
            _timeCollector = timeCollector;
        }

        public async Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation)
        {
            try
            {
                var planAmountDetails = await new UriTemplate(_options.Value.CalculateUrlTemplate)
                        .AddParameter("accountname", accountname)
                        .AddParameter("planId", agreementInformation.PlanId)
                        .AddParameter("discountId", agreementInformation.DiscountId)
                        .AddParameter("promocode", agreementInformation.Promocode)
                        .Resolve()
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return planAmountDetails.Total == agreementInformation.Total;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get total amount for user {accountname}.");
                throw;
            }
        }

        public async Task<Promotion> GetValidPromotionByCode(string promocode, int planId)
        {
            using var _ = _timeCollector.StartScope();
            try
            {
                // TODO: in the future, consider validating a signed token in place of request to the AccountPlanAPI
                var promotion = await new UriTemplate(_options.Value.GetPromoCodeTemplate)
                    .AddParameter("planId", planId)
                    .AddParameter("promocode", promocode)
                    .Resolve()
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<Promotion>();

                return promotion;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get promocode information {promocode}.");
                throw;
            }
        }

        public async Task<PlanAmountDetails> GetCalculateUpgrade(string accountName, AgreementInformation agreementInformation)
        {
            using var _ = _timeCollector.StartScope();
            try
            {
                var PlanAmountDetails = await new UriTemplate(_options.Value.CalculateUrlTemplate)
                    .AddParameter("accountname", accountName)
                    .AddParameter("planId", agreementInformation.PlanId)
                    .AddParameter("discountId", agreementInformation.DiscountId)
                    .AddParameter("promocode", agreementInformation.Promocode)
                    .Resolve()
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return PlanAmountDetails;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get total amount for user {accountName}.");
                throw;
            }
        }

        public async Task<PlanAmountDetails> GetCalculateLandingUpgrade(string accountName, IEnumerable<int> landingIds, IEnumerable<int> landingPacks)
        {
            try
            {
                var PlanAmountDetails = await new UriTemplate(_options.Value.CalculateLandingUrlTemplate)
                    .AddParameter("accountname", accountName)
                    .AddParameter("landingIds", string.Join<int>(",", landingIds))
                    .AddParameter("landingPacks", string.Join<int>(",", landingPacks))
                    .Resolve()
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return PlanAmountDetails;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get total landing amount for user {accountName}.");
                throw;
            }
        }
    }
}
