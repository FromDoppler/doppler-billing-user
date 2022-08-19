using System;
using System.Threading.Tasks;
using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.AccountPlansApi
{
    public partial class AccountPlansService : IAccountPlansService
    {
        private readonly IOptions<AccountPlansSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly ICurrentRequestApiTokenGetter _usersApiTokenGetter;

        [LoggerMessage(0, LogLevel.Error, "Error to get total amount for user {accountname}.")]
        partial void LogErrorGetTotalAmountForuser(string accountname);

        [LoggerMessage(1, LogLevel.Error, "Error to get promocode information {promocode}.")]
        partial void LogErrorGetPromocodeInformation(string promocode);


        public AccountPlansService(
            IOptions<AccountPlansSettings> options,
            ILogger<AccountPlansService> logger,
            IFlurlClientFactory flurlClientFac,
            ICurrentRequestApiTokenGetter usersApiTokenGetter)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.CalculateUrlTemplate);
            _usersApiTokenGetter = usersApiTokenGetter;
        }

        public async Task<bool> IsValidTotal(string accountname, AgreementInformation agreementInformation)
        {
            try
            {
                var planAmountDetails = await _flurlClient.Request(new UriTemplate(_options.Value.CalculateUrlTemplate)
                        .AddParameter("accountname", accountname)
                        .AddParameter("planId", agreementInformation.PlanId)
                        .AddParameter("discountId", agreementInformation.DiscountId)
                        .AddParameter("promocode", agreementInformation.Promocode)
                        .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return planAmountDetails.Total == agreementInformation.Total;
            }
            catch (Exception)
            {
                LogErrorGetTotalAmountForuser(accountname);
                throw;
            }
        }

        public async Task<Promotion> GetValidPromotionByCode(string promocode, int planId)
        {
            try
            {
                // TODO: in the future, consider validating a signed token in place of request to the AccountPlanAPI
                var promotion = await _flurlClient.Request(new UriTemplate(_options.Value.GetPromoCodeTemplate)
                    .AddParameter("planId", planId)
                    .AddParameter("promocode", promocode)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<Promotion>();

                return promotion;
            }
            catch (Exception)
            {
                LogErrorGetPromocodeInformation(promocode);
                throw;
            }
        }

        public async Task<PlanAmountDetails> GetCalculateUpgrade(string accountName, AgreementInformation agreementInformation)
        {
            try
            {
                var planAmountDetails = await _flurlClient.Request(new UriTemplate(_options.Value.CalculateUrlTemplate)
                    .AddParameter("accountname", accountName)
                    .AddParameter("planId", agreementInformation.PlanId)
                    .AddParameter("discountId", agreementInformation.DiscountId)
                    .AddParameter("promocode", agreementInformation.Promocode)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {await _usersApiTokenGetter.GetTokenAsync()}")
                    .GetJsonAsync<PlanAmountDetails>();

                return planAmountDetails;
            }
            catch (Exception)
            {
                LogErrorGetTotalAmountForuser(accountName);
                throw;
            }
        }
    }
}
