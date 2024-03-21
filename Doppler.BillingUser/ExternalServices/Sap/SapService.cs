using Doppler.BillingUser.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Doppler.BillingUser.TimeCollector;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public class SapService : ISapService
    {
        private readonly IOptions<SapSettings> _options;
        private readonly ILogger _logger;
        private readonly IFlurlClient _flurlClient;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ITimeCollector _timeCollector;

        public SapService(
            IOptions<SapSettings> options,
            ILogger<SapService> logger,
            IFlurlClientFactory flurlClientFac,
            IJwtTokenGenerator jwtTokenGenerator,
            ITimeCollector timeCollector)
        {
            _options = options;
            _logger = logger;
            _flurlClient = flurlClientFac.Get(_options.Value.SapCreateBusinessPartnerEndpoint);
            _jwtTokenGenerator = jwtTokenGenerator;
            _timeCollector = timeCollector;
        }

        public async Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null)
        {
            using var _ = _timeCollector.StartScope();
            if (!SapHelper.IsMakingSenseAccount(sapBusinessPartner.Email))
            {
                try
                {
                    await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateBusinessPartnerEndpoint)
                        .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                        .PostJsonAsync(sapBusinessPartner);

                    _logger.LogInformation($"User data successfully sent to DopplerSap. Iduser: {sapBusinessPartner.Id} - ClientManager: {sapBusinessPartner.IsClientManager}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending data to DopplerSap");
                }
            }
        }
        public async Task SendBillingToSap(SapBillingDto sapBilling, string email)
        {
            using var _ = _timeCollector.StartScope();
            if (!SapHelper.IsMakingSenseAccount(email))
            {
                try
                {
                    await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateBillingRequestEndpoint)
                        .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                        .PostJsonAsync(new List<SapBillingDto>() { sapBilling });

                    _logger.LogInformation($"User billing data successfully sent to Sap. User: {email}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending invoice data to Sap");
                }
            }
        }

        public async Task SendCreditNoteToSapAsync(string accountName, SapCreditNoteDto sapCreditNoteDto)
        {
            using var _ = _timeCollector.StartScope();
            if (!SapHelper.IsMakingSenseAccount(accountName))
            {
                try
                {
                    await _flurlClient.Request(_options.Value.SapBaseUrl + _options.Value.SapCreateCreditNoteEndpoint)
                        .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                        .PostJsonAsync(sapCreditNoteDto);

                    _logger.LogInformation($"Credit Note succesfully sent to DopplerSap. CreditNoteId: {sapCreditNoteDto.CreditNoteId}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error sending credit note data to Sap");
                }
            }
        }
    }
}
