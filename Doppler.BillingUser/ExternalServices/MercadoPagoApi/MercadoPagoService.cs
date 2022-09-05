using Doppler.BillingUser.Authorization;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly IOptions<MercadoPagoSettings> _options;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IFlurlClient _flurlClient;
        private readonly ILogger<MercadoPagoService> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailTemplatesService _emailTemplatesService;

        const string TransactionDescription = "Doppler Email Marketing";
        const string Description = "MERPAGO*DOPPLER";
        const string Master = "master";

        public MercadoPagoService(
            IOptions<MercadoPagoSettings> options,
            IJwtTokenGenerator jwtTokenGenerator,
            IFlurlClientFactory flurlClientFactory,
            ILogger<MercadoPagoService> logger,
            IEncryptionService encryptionService,
            IEmailTemplatesService emailTemplatesService)
        {
            _options = options;
            _jwtTokenGenerator = jwtTokenGenerator;
            _flurlClient = flurlClientFactory.Get(_options.Value.MercadoPagoApiUrlTemplate);
            _logger = logger;
            _encryptionService = encryptionService;
            _emailTemplatesService = emailTemplatesService;
        }

        public async Task<MercadoPagoPayment> GetPaymentById(long id, string accountname)
        {
            try
            {
                var payment = await _flurlClient.Request(new UriTemplate(_options.Value.MercadoPagoApiUrlTemplate)
                    .AddParameter("accountname", accountname)
                    .AddParameter("id", id)
                    .Resolve())
                    .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                    .GetJsonAsync<MercadoPagoPayment>();
                return payment;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error to get payment for user: {accountname} with payment ID: {id}");
                throw;
            }
        }

        public async Task<MercadoPagoPayment> CreatePayment(string accountname, int clientId, decimal total, CreditCard creditCard, bool isFreeUser)
        {
            try
            {
                var paymentRequestDto = CreatePaymentRequestDto(total, creditCard);
                var payment = await PostMercadoPagoPayment(accountname, paymentRequestDto);

                if (payment.Status is MercadoPagoPaymentStatusEnum.Rejected or
                    MercadoPagoPaymentStatusEnum.Cancelled or
                    MercadoPagoPaymentStatusEnum.Refunded or
                    MercadoPagoPaymentStatusEnum.Charged_Back)
                {
                    var errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                    var errorMessage = payment.StatusDetail;

                    _logger.LogError(String.Format("Mercadopago payment Declined with Accountname:{0}, ErrorCode:{1}, ErrorMessage: {2}", accountname, errorCode, errorMessage));

                    await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(clientId, errorCode.ToString(), errorMessage, string.Empty, String.Empty, PaymentMethodEnum.MP, isFreeUser);

                    throw new DopplerApplicationException(errorCode, errorMessage);
                }

                if (payment.Status == MercadoPagoPaymentStatusEnum.In_Process)
                {
                    var errorCode = payment.Status.ToString();
                    var errorMessage = string.Format("payment is in process, MercadopagoStatus: {0}, MercadopagoStatusDetail:{1}", payment.Status, payment.StatusDetail);
                    await _emailTemplatesService.SendNotificationForMercadoPagoPaymentInProcess(clientId, accountname, errorCode, errorMessage, isFreeUser);
                }

                return payment;
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        private async Task<MercadoPagoPayment> PostMercadoPagoPayment(string accountname, PaymentRequestDto paymentRequestDto)
        {
            var payment = await _flurlClient.Request(new UriTemplate(_options.Value.MercadoPagoApiUrlTemplate)
                .AddParameter("accountname", accountname)
                .Resolve())
                .WithHeader("Authorization", $"Bearer {_jwtTokenGenerator.GenerateSuperUserJwtToken()}")
                .PostJsonAsync(paymentRequestDto)
                .ReceiveJson<MercadoPagoPayment>();

            return payment;
        }

        private PaymentRequestDto CreatePaymentRequestDto(decimal total, CreditCard creditCard)
        {
            var paymentRequestDto = new PaymentRequestDto
            {
                TransactionAmount = total,
                Installments = 1,
                TransactionDescription = TransactionDescription,
                Description = Description,
                PaymentMethodId = creditCard.CardType == CardTypeEnum.Mastercard ? Master : creditCard.CardType.ToString().ToLower(),
                Card = new CardDto
                {
                    Cardholder = new PaymentCardholder
                    {
                        Identification = new Identification
                        {
                            Number = creditCard.IdentificationNumber,
                            Type = creditCard.IdentificationType
                        },
                        Name = _encryptionService.DecryptAES256(creditCard.HolderName)
                    },
                    CardNumber = _encryptionService.DecryptAES256(creditCard.Number),
                    SecurityCode = _encryptionService.DecryptAES256(creditCard.Code),
                    ExpirationMonth = creditCard.ExpirationMonth.ToString(),
                    ExpirationYear = creditCard.ExpirationYear.ToString()
                }
            };

            return paymentRequestDto;
        }
    }
}
