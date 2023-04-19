using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.E4;
using Doppler.BillingUser.Services;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.FirstData
{
    public class PaymentGateway : IPaymentGateway
    {
        private const string Type = "application/xml";
        private const string Uri = "/transaction/v29";

        private readonly string _gatewayId;
        private readonly string _password;
        private readonly string _hmac;
        private readonly string _keyId;
        private readonly bool _isDemo;
        private readonly int _amountToValidateCreditCard;
        private readonly string _endpoint;
        private readonly HashSet<string> _doNotHonorCodes = new HashSet<string>(new[] { "530", "606", "303" });
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger _logger;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly IFlurlClient _flurlClient = new FlurlClient();

        public PaymentGateway(IEncryptionService encryptionService,
            IFirstDataService config,
            ILogger<PaymentGateway> logger,
            IEmailTemplatesService emailTemplatesService)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _encryptionService = encryptionService;

            _gatewayId = config.GetUsername();
            _password = config.GetPassword();
            _hmac = config.GetHmac();
            _keyId = config.GetKeyId();
            _isDemo = config.GetIsDemo();
            _amountToValidateCreditCard = config.GetAmountToValidate();
            _endpoint = _isDemo ? config.GetFirstDataServiceSoapDemo() : config.GetFirstDataServiceSoap();
            _emailTemplatesService = emailTemplatesService;
            _logger = logger;
        }

        private string GetHmacSignature(string hashedContent, string timeString)
        {
            var hashData = $"POST\n{Type}\n{hashedContent}\n{timeString}\n{Uri}";

            var hmac_sha1 = new HMACSHA1(Encoding.ASCII.GetBytes(_hmac));
            var hmac_data = hmac_sha1.ComputeHash(Encoding.ASCII.GetBytes(hashData));

            return Convert.ToBase64String(hmac_data);
        }

        private string GetHashedContent(string payload)
        {
            var payloadBytes = Encoding.ASCII.GetBytes(payload);
            var sha1_crypto = new SHA1CryptoServiceProvider();
            var hash = BitConverter.ToString(sha1_crypto.ComputeHash(payloadBytes)).Replace("-", "");
            return hash.ToLower();
        }


        private async Task<string> PostRequest(Transaction txn, int clientId, bool isFreeUser, bool isReprocessCall = false)
        {
            PaymentErrorCode errorCode;
            try
            {
                txn.ExactID = _gatewayId;
                txn.Password = _password;

                var xmlContent = Utils.XmlSerializer.ToXmlString(txn);
                var timeString = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var contentDigest = GetHashedContent(xmlContent);

                var content = new StringContent(xmlContent, Encoding.ASCII);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(Type);
                content.Headers.ContentLength = xmlContent.Length;

                var base64HmacSignature = GetHmacSignature(contentDigest, timeString);
                var xmlResponse = await _flurlClient
                    .Request(_endpoint)
                    .WithHeader("Accept", "*/*")
                    .WithHeader("x-gge4-date", timeString)
                    .WithHeader("x-gge4-content-sha1", contentDigest)
                    .WithHeader("Authorization", $"GGE4_API {_keyId}:{base64HmacSignature}")
                    .PostAsync(content)
                    .ReceiveString();

                var apiResponse = Utils.XmlSerializer.FromXmlString<TransactionResult>(xmlResponse);

                string authNumber = apiResponse.Authorization_Num;
                if (!apiResponse.Transaction_Approved)
                {
                    string errorMessage = "";
                    string approvalCode = apiResponse.EXact_Resp_Code;
                    if (approvalCode != ResponseTypes.NORMAL_TRANSACTION)
                    {
                        errorMessage = apiResponse.EXact_Message;
                        switch (approvalCode)
                        {
                            case ResponseTypes.DUPLICATE:
                                errorCode = PaymentErrorCode.DuplicatedPaymentTransaction;
                                break;
                            default:
                                errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                                break;
                        }
                    }
                    else if (apiResponse.Bank_Resp_Code != null && _doNotHonorCodes.Contains(apiResponse.Bank_Resp_Code))
                    {
                        errorMessage = apiResponse.Bank_Message + " [Bank]";
                        errorCode = PaymentErrorCode.DoNotHonorPaymentResponse;
                    }
                    else
                    {
                        errorMessage = apiResponse.Bank_Message + " [Bank]";
                        errorCode = PaymentErrorCode.DeclinedPaymentTransaction;
                    }

                    _logger.LogError(String.Format("First Data Error: Client Id: {0}, CVDCode: {1}, CVD_Presence_Ind: {2}", clientId, txn.CVDCode, txn.CVD_Presence_Ind));
                    _logger.LogError(String.Format("Response: CVV: {0}, ErrorCode:{1}, ErrorMessage: {2}", apiResponse.CVV2, errorCode, errorMessage));

                    if (txn.Transaction_Type != TransactionTypes.REFUND && !isReprocessCall)
                    {
                        await _emailTemplatesService.SendNotificationForPaymentFailedTransaction(clientId, errorCode.ToString(), errorMessage, apiResponse.CTR, apiResponse.Bank_Message, PaymentMethodEnum.CC, isFreeUser);
                    }

                    throw new DopplerApplicationException(errorCode, errorMessage);
                }
                return authNumber;
            }
            catch (Exception ex) when (ex is not DopplerApplicationException)
            {
                _logger.LogError(ex, "Unexpected error");
                throw new DopplerApplicationException(PaymentErrorCode.ClientPaymentTransactionError, ex.Message, ex);
            }
        }

        private Transaction CreateDirectPaymentRequest(string type, decimal chargeTotal, CreditCard creditCard, int clientId)
        {
            Transaction txn = new Transaction
            {
                Transaction_Type = type,
                Customer_Ref = clientId.ToString(),
                CardHoldersName = _encryptionService.DecryptAES256(creditCard.HolderName),
                Card_Number = _encryptionService.DecryptAES256(creditCard.Number),
                Expiry_Date = String.Format("{0:00}{1:00}", creditCard.ExpirationMonth, creditCard.ExpirationYear % 100),
                DollarAmount = chargeTotal.ToString(CultureInfo.InvariantCulture),
                Reference_No = "Doppler Email Marketing"
            };

            if (creditCard.Code != null)
            {
                txn.CVD_Presence_Ind = "1";
                txn.CVDCode = _encryptionService.DecryptAES256(creditCard.Code);
            }

            return txn;
        }

        public async Task<bool> IsValidCreditCard(CreditCard creditCard, int clientId, bool isFreeUser)
        {
            try
            {
                var paymentRequest = CreateDirectPaymentRequest(TransactionTypes.PRE_AUTH, _amountToValidateCreditCard, creditCard, clientId);
                await PostRequest(paymentRequest, clientId, isFreeUser);
                return true;
            }
            catch (DopplerApplicationException ex)
            {
                switch (ex.ErrorCode)
                {
                    case PaymentErrorCode.DeclinedPaymentTransaction:
                    case PaymentErrorCode.DuplicatedPaymentTransaction:
                    case PaymentErrorCode.FraudPaymentTransaction:
                    case PaymentErrorCode.DoNotHonorPaymentResponse:
                        throw ex;
                    default:
                        throw;
                }
            }
        }

        public async Task<string> CreateCreditCardPayment(decimal chargeTotal, CreditCard creditCard, int clientId, bool isFreeUser, bool isReprocessCall)
        {
            var paymentRequest = CreateDirectPaymentRequest(TransactionTypes.PURCHASE, chargeTotal, creditCard, clientId);
            return await PostRequest(paymentRequest, clientId, isFreeUser, isReprocessCall);
        }
    }
}
