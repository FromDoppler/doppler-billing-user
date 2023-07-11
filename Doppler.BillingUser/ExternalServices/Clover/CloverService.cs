using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Clover.Errors;
using Doppler.BillingUser.ExternalServices.Clover.Requests;
using Doppler.BillingUser.ExternalServices.Clover.Responses;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.Clover
{
    public class CloverService : ICloverService
    {
        private readonly IOptions<CloverSettings> _options;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger<CloverService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly ISlackService _slackService;

        public CloverService(
            IOptions<CloverSettings> options,
            IJwtTokenGenerator jwtTokenGenerator,
            IFlurlClientFactory flurlClientFactory,
            ILogger<CloverService> logger,
            IEncryptionService encryptionService,
            IEmailTemplatesService emailTemplatesService,
            ISlackService slackService)
        {
            _options = options;
            _jwtTokenGenerator = jwtTokenGenerator;
            _flurlClient = flurlClientFactory.Get(_options.Value.BaseUrl);
            _logger = logger;
            _encryptionService = encryptionService;
            _emailTemplatesService = emailTemplatesService;
            _slackService = slackService;
        }

        public async Task<string> CreateCreditCardPayment(string accountname, decimal chargeTotal, CreditCard creditCard, int clientId, bool isFreeUser, bool isReprocessCall)
        {
            try
            {
                var paymentRequest = MapToPaymentRequest(chargeTotal, creditCard, clientId);
                return await PostCloverPayment(accountname, paymentRequest, isFreeUser);
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        public async Task<bool> IsValidCreditCard(string accountname, CreditCard creditCard, int clientId, bool isFreeUser)
        {
            try
            {
                var paymentRequest = MapToPaymentRequest(0, creditCard, clientId);
                var payment = await ValidateCreditCard(accountname, paymentRequest, isFreeUser);

                return payment;
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        public async Task<CustomerResponse> CreateCustomerAsync(string email, string name, CreditCard creditCard)
        {
            var customerRequest = MapToCustomerRequest(email, name, creditCard, string.Empty);
            var result = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/accounts/{email}/customer")
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PostJsonAsync(customerRequest)
                .ReceiveJson<CustomerResponse>();

            return result;
        }

        public async Task<CustomerResponse> UpdateCustomerAsync(string email, string name, CreditCard creditCard, string cloverCustomerId)
        {
            var customerRequest = MapToCustomerRequest(email, name, creditCard, cloverCustomerId);
            var result = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/accounts/{email}/customer")
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PutJsonAsync(customerRequest)
                .ReceiveJson<CustomerResponse>();

            return result;
        }

        public async Task<CustomerResponse> GetCustomerAsync(string email)
        {

            return await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/accounts/{email}/customer")
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .GetJsonAsync<CustomerResponse>();
        }

        private async Task<bool> ValidateCreditCard(string accountname, PaymentRequest paymentRequest, bool isFreeUser)
        {
            try
            {
                var result = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/accounts/{accountname}/creditcard/validate")
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PostJsonAsync(paymentRequest)
                .ReceiveJson<bool>();

                return result;
            }
            catch (FlurlHttpException ex)
            {
                var errorReponseBody = await ex.GetResponseJsonAsync<ApiError>();
                await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(int.Parse(paymentRequest.ClientId), errorReponseBody.Error.Code, errorReponseBody.Error.Message, string.Empty, string.Empty, PaymentMethodEnum.CC, isFreeUser, paymentRequest.CreditCard.CardHolderName, paymentRequest.CreditCard.CardNumber[^4..]);
                _logger.LogError(ex, "Unexpected error");

                var messageError = $"Failed to validate the credit card for user {accountname} {errorReponseBody.Error.Code}: {errorReponseBody.Error.Message}.";
                await _slackService.SendNotification(messageError);

                throw new DopplerApplicationException(PaymentErrorCode.DeclinedPaymentTransaction, $"{errorReponseBody.Error.Code}", ex);
            }
        }

        private async Task<string> PostCloverPayment(string accountname, PaymentRequest paymentRequest, bool isFreeUser)
        {
            try
            {
                var payment = await _flurlClient.Request(new UriTemplate(_options.Value.BaseUrl + $"/accounts/{accountname}/payment")
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                    .PostJsonAsync(paymentRequest)
                    .ReceiveString();

                return payment;
            }
            catch (FlurlHttpException ex)
            {
                var errorReponseBody = await ex.GetResponseJsonAsync<ApiError>();
                await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(int.Parse(paymentRequest.ClientId), errorReponseBody.Error.Code, errorReponseBody.Error.Message, string.Empty, string.Empty, PaymentMethodEnum.CC, isFreeUser, paymentRequest.CreditCard.CardHolderName, paymentRequest.CreditCard.CardHolderName[^4..]);

                _logger.LogError(ex, "Unexpected error");

                var messageError = $"Failed to create the payment for user {accountname} {errorReponseBody.Error.Code}: {errorReponseBody.Error.Message}.";
                await _slackService.SendNotification(messageError);

                throw new DopplerApplicationException(PaymentErrorCode.DeclinedPaymentTransaction, errorReponseBody.Error.Code, ex);
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        private string MapCardType(CardTypeEnum creditCardType)
        {
            switch (creditCardType)
            {
                case CardTypeEnum.Visa:
                    return "VISA";
                case CardTypeEnum.Mastercard:
                    return "MC";
                case CardTypeEnum.Amex:
                    return "AMEX";
                case CardTypeEnum.Unknown:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        private PaymentRequest MapToPaymentRequest(decimal chargeTotal, CreditCard creditCard, int clientId)
        {
            return new PaymentRequest
            {
                ChargeTotal = chargeTotal,
                ClientId = clientId.ToString(),
                CreditCard = new Entities.CreditCard
                {
                    CardExpMonth = creditCard.ExpirationMonth.ToString(),
                    CardExpYear = creditCard.ExpirationYear.ToString(),
                    CardHolderName = _encryptionService.DecryptAES256(creditCard.HolderName),
                    CardNumber = _encryptionService.DecryptAES256(creditCard.Number),
                    CardType = MapCardType(creditCard.CardType),
                    SecurityCode = _encryptionService.DecryptAES256(creditCard.Code)
                }
            };
        }

        private CustomerRequest MapToCustomerRequest(string email, string name, CreditCard creditCard, string cloverCustomerId)
        {
            return new CustomerRequest
            {
                CloverCustomerId = cloverCustomerId,
                Email = email,
                Name = name,
                CreditCard = new Entities.CreditCard
                {
                    CardExpMonth = creditCard.ExpirationMonth.ToString(),
                    CardExpYear = creditCard.ExpirationYear.ToString(),
                    CardHolderName = _encryptionService.DecryptAES256(creditCard.HolderName),
                    CardNumber = _encryptionService.DecryptAES256(creditCard.Number),
                    CardType = MapCardType(creditCard.CardType),
                    SecurityCode = _encryptionService.DecryptAES256(creditCard.Code)
                }
            };
        }
    }
}
