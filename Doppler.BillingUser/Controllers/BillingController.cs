using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.DopplerSecurity;
using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using Doppler.BillingUser.ExternalServices.Aws;
using Doppler.BillingUser.ExternalServices.BeplicApi;
using Doppler.BillingUser.ExternalServices.Clover;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.ExternalServices.StaticDataCllient;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.BillingCredit;
using Doppler.BillingUser.Mappers.ConversationPlan;
using Doppler.BillingUser.Mappers.PaymentMethod;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Settings;
using Doppler.BillingUser.TimeCollector;
using Doppler.BillingUser.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Controllers
{
    [Authorize]
    [ApiController]
    public class BillingController
    {
        private readonly ILogger _logger;
        private readonly IBillingRepository _billingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IValidator<BillingInformation> _billingInformationValidator;
        private readonly IAccountPlansService _accountPlansService;
        private readonly IValidator<AgreementInformation> _agreementInformationValidator;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly ISapService _sapService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOptions<SapSettings> _sapSettings;
        private readonly IPromotionRepository _promotionRepository;
        private readonly ISlackService _slackService;
        private readonly IOptions<ZohoSettings> _zohoSettings;
        private readonly IZohoService _zohoService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly IOptions<AttemptsToUpdateSettings> _attemptsToUpdateSettings;
        private readonly IStaticDataClient _staticDataClient;
        private readonly IOptions<CloverSettings> _cloverSettings;
        private readonly ICloverService _cloverService;
        private readonly ITimeCollector _timeCollector;
        private readonly ILandingPlanUserRepository _landingPlanUserRepository;
        private readonly IUserAddOnRepository _userAddOnRepository;
        private readonly ILandingPlanRepository _landingPlanRepository;
        private readonly IChatPlanRepository _chatPlanRepository;
        private readonly IBeplicService _beplicService;
        private readonly IChatPlanUserRepository _chatPlanUserRepository;
        private readonly IClientManagerRepository _clientManagerRepository;

        private readonly IFileStorage _fileStorage;
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static readonly List<UserTypeEnum> AllowedPlanTypesForBilling = new List<UserTypeEnum>
        {
            UserTypeEnum.INDIVIDUAL,
            UserTypeEnum.MONTHLY,
            UserTypeEnum.SUBSCRIBERS
        };
        private static readonly List<PaymentMethodEnum> AllowedPaymentMethodsForBilling = new List<PaymentMethodEnum>
        {
            PaymentMethodEnum.CC,
            PaymentMethodEnum.TRANSF,
            PaymentMethodEnum.MP,
            PaymentMethodEnum.DA
        };

        private static readonly List<CountryEnum> AllowedCountriesForTransfer = new List<CountryEnum>
        {
            CountryEnum.Colombia,
            CountryEnum.Mexico,
            CountryEnum.Argentina
        };

        private static readonly List<UserTypeEnum> AllowedUpdatePlanTypesForBilling = new List<UserTypeEnum>
        {
            UserTypeEnum.MONTHLY,
            UserTypeEnum.SUBSCRIBERS,
            UserTypeEnum.INDIVIDUAL
        };

        private const string Source = "Checkout";
        private const string CancelatedObservation = "Phishing User. AyC";

        public BillingController(
            ILogger<BillingController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IValidator<BillingInformation> billingInformationValidator,
            IValidator<AgreementInformation> agreementInformationValidator,
            IAccountPlansService accountPlansService,
            IPaymentGateway paymentGateway,
            ISapService sapService,
            IEncryptionService encryptionService,
            IOptions<SapSettings> sapSettings,
            IPromotionRepository promotionRepository,
            ISlackService slackService,
            IEmailSender emailSender,
            IOptions<EmailNotificationsConfiguration> emailSettings,
            IOptions<ZohoSettings> zohoSettings,
            IZohoService zohoService,
            IEmailTemplatesService emailTemplatesService,
            IMercadoPagoService mercadopagoService,
            IPaymentAmountHelper paymentAmountService,
            IUserPaymentHistoryRepository userPaymentHistoryRepository,
            IOptions<AttemptsToUpdateSettings> attemptsToUpdateSettings,
            IStaticDataClient staticDataClient,
            IOptions<CloverSettings> cloverSettings,
            ICloverService cloverService,
            IFileStorage fileStorage,
            ITimeCollector timeCollector,
            ILandingPlanUserRepository landingPlanUserRepository,
            IUserAddOnRepository userAddOnRepository,
            ILandingPlanRepository landingPlanRepository,
            IChatPlanRepository chatPlanRepository,
            IBeplicService beplicService,
            IChatPlanUserRepository chatPlanUserRepository,
            IClientManagerRepository clientManagerRepository)
        {
            _logger = logger;
            _billingRepository = billingRepository;
            _userRepository = userRepository;
            _billingInformationValidator = billingInformationValidator;
            _agreementInformationValidator = agreementInformationValidator;
            _accountPlansService = accountPlansService;
            _paymentGateway = paymentGateway;
            _sapService = sapService;
            _emailSender = emailSender;
            _emailSettings = emailSettings;
            _encryptionService = encryptionService;
            _sapSettings = sapSettings;
            _promotionRepository = promotionRepository;
            _slackService = slackService;
            _zohoSettings = zohoSettings;
            _zohoService = zohoService;
            _emailTemplatesService = emailTemplatesService;
            _mercadoPagoService = mercadopagoService;
            _paymentAmountService = paymentAmountService;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
            _attemptsToUpdateSettings = attemptsToUpdateSettings;
            _staticDataClient = staticDataClient;
            _cloverSettings = cloverSettings;
            _cloverService = cloverService;
            _fileStorage = fileStorage;
            _timeCollector = timeCollector;
            _landingPlanUserRepository = landingPlanUserRepository;
            _userAddOnRepository = userAddOnRepository;
            _landingPlanRepository = landingPlanRepository;
            _chatPlanRepository = chatPlanRepository;
            _beplicService = beplicService;
            _chatPlanUserRepository = chatPlanUserRepository;
            _clientManagerRepository = clientManagerRepository;
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpGet("/accounts/{accountName}/billing-information")]
        public async Task<IActionResult> GetBillingInformation(string accountName)
        {
            var billingInformation = await _billingRepository.GetBillingInformation(accountName);

            if (billingInformation == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(billingInformation);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information")]
        public async Task<IActionResult> UpdateBillingInformation(string accountname, [FromBody] BillingInformation billingInformation)
        {
            var results = await _billingInformationValidator.ValidateAsync(billingInformation);
            if (!results.IsValid)
            {
                return new BadRequestObjectResult(results.ToString("-"));
            }

            var currentBillingInformation = await _billingRepository.GetBillingInformation(accountname);

            await _billingRepository.UpdateBillingInformation(accountname, billingInformation);

            if (currentBillingInformation != null && currentBillingInformation.Country.ToLower() != billingInformation.Country.ToLower())
            {
                var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

                if (currentPaymentMethod != null && (currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.TRANSF.ToString() ||
                    currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.DA.ToString()))
                {
                    var userInformation = await _userRepository.GetUserInformation(accountname);
                    await _billingRepository.SetEmptyPaymentMethod(userInformation.IdUser);
                }
            }

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> GetInvoiceRecipients(string accountname)
        {
            var result = await _billingRepository.GetInvoiceRecipients(accountname);

            if (result == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(result);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/billing-information/invoice-recipients")]
        public async Task<IActionResult> UpdateInvoiceRecipients(string accountname, [FromBody] InvoiceRecipients invoiceRecipients)
        {
            await _billingRepository.UpdateInvoiceRecipients(accountname, invoiceRecipients.Recipients, invoiceRecipients.PlanId);

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpGet("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> GetCurrentPaymentMethod(string accountname)
        {
            _logger.LogDebug("Get current payment method.");

            var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

            if (currentPaymentMethod == null)
            {
                return new NotFoundResult();
            }

            var getCurrentPaymentMethod = currentPaymentMethod.MapFromPaymentMethodToGetPaymentMethodResult();

            return new OkObjectResult(getCurrentPaymentMethod);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpPut("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> UpdateCurrentPaymentMethod(string accountname, [FromForm] PaymentMethod paymentMethod)
        {
            using var _ = _timeCollector.StartScope();

            _logger.LogDebug("Update current payment method.");

            User userInformation = await _userRepository.GetUserInformation(accountname);

            try
            {
                if (userInformation == null)
                {
                    return new BadRequestObjectResult("The user does not exist");
                }

                if (userInformation.IsCancelated)
                {
                    return new BadRequestObjectResult("UserCanceled");
                }

                paymentMethod.TaxCertificateUrl = await PutTaxCertificateUrl(paymentMethod, accountname);

                var isSuccess = await _billingRepository.UpdateCurrentPaymentMethod(userInformation, paymentMethod);

                if (!isSuccess)
                {
                    var cardNumberDetails = paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? "with credit card's last 4 digits: " + paymentMethod.CCNumber[^4..] : "";
                    var messageError = $"Failed at updating payment method for user {accountname} {cardNumberDetails}.";

                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);

                    return new BadRequestObjectResult("Failed at updating payment");
                }

                /* Create or update the customer in clover if it not exists or change the credit card */
                if (_cloverSettings.Value.UseCloverApi)
                {
                    if (paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString())
                    {
                        var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                        var validCc = Enum.Parse<CardTypeEnum>(paymentMethod.CCType) != CardTypeEnum.Unknown && await _cloverService.IsValidCreditCard(accountname, encryptedCreditCard, userInformation.IdUser, true);
                        if (validCc)
                        {
                            var customer = await _cloverService.GetCustomerAsync(accountname);
                            if (customer == null)
                            {
                                await _cloverService.CreateCustomerAsync(accountname, _encryptionService.DecryptAES256(encryptedCreditCard.HolderName), encryptedCreditCard);
                            }
                            else
                            {
                                var creditCardNumber = _encryptionService.DecryptAES256(encryptedCreditCard.Number);
                                var first6 = creditCardNumber[0..6];
                                var last4 = creditCardNumber[^4..];

                                var currentCreditCard = customer.Cards.Elements.FirstOrDefault();

                                if (currentCreditCard.First6 != first6 || currentCreditCard.Last4 != last4)
                                {
                                    await _cloverService.UpdateCustomerAsync(accountname, _encryptionService.DecryptAES256(encryptedCreditCard.HolderName), encryptedCreditCard, customer.Id);
                                }
                            }
                        }
                    }
                }

                var userBillingInfo = await _userRepository.GetUserBillingInformation(accountname);

                if (userBillingInfo.IdCurrentBillingCredit.HasValue && userBillingInfo.IdCurrentBillingCredit.Value != 0)
                {
                    BillingCreditPaymentInfo billingCreditPaymentInfo = null;

                    switch (userBillingInfo.PaymentMethod)
                    {
                        case PaymentMethodEnum.CC:
                        case PaymentMethodEnum.MP:
                            billingCreditPaymentInfo = new BillingCreditPaymentInfo()
                            {
                                CCNumber = _encryptionService.EncryptAES256(paymentMethod.CCNumber.Replace(" ", "")),
                                CCExpMonth = int.Parse(paymentMethod.CCExpMonth),
                                CCExpYear = int.Parse(paymentMethod.CCExpYear),
                                CCVerification = _encryptionService.EncryptAES256(paymentMethod.CCVerification),
                                CCHolderFullName = _encryptionService.EncryptAES256(paymentMethod.CCHolderFullName),
                                CCType = paymentMethod.CCType,
                                Cuit = paymentMethod.IdentificationNumber,
                                IdentificationNumber = CreditCardHelper.ObfuscateNumber(paymentMethod.CCNumber),
                                PaymentMethodName = paymentMethod.PaymentMethodName,
                                ResponsabileBilling = userBillingInfo.PaymentMethod == PaymentMethodEnum.CC ? ResponsabileBillingEnum.QBL : ResponsabileBillingEnum.Mercadopago
                            };
                            break;
                        case PaymentMethodEnum.TRANSF:

                            if (userBillingInfo.IdBillingCountry == (int)CountryEnum.Argentina)
                            {
                                billingCreditPaymentInfo = new BillingCreditPaymentInfo()
                                {
                                    CCNumber = string.Empty,
                                    CCExpMonth = null,
                                    CCExpYear = null,
                                    CCVerification = string.Empty,
                                    CCHolderFullName = string.Empty,
                                    CCType = string.Empty,
                                    IdConsumerType = paymentMethod.IdConsumerType,
                                    Cuit = paymentMethod.IdentificationNumber,
                                    IdentificationNumber = string.Empty,
                                    PaymentMethodName = paymentMethod.PaymentMethodName,
                                    ResponsabileBilling = ResponsabileBillingEnum.GBBISIDE
                                };
                            }

                            break;
                        case PaymentMethodEnum.DA:
                            if (userBillingInfo.IdBillingCountry == (int)CountryEnum.Argentina)
                            {
                                billingCreditPaymentInfo = new BillingCreditPaymentInfo()
                                {
                                    CCNumber = string.Empty,
                                    CCExpMonth = null,
                                    CCExpYear = null,
                                    CCVerification = string.Empty,
                                    CCHolderFullName = string.Empty,
                                    CCType = string.Empty,
                                    IdConsumerType = paymentMethod.IdConsumerType,
                                    Cuit = paymentMethod.IdentificationNumber,
                                    IdentificationNumber = string.Empty,
                                    PaymentMethodName = paymentMethod.PaymentMethodName,
                                    Cbu = paymentMethod.Cbu,
                                    ResponsabileBilling = ResponsabileBillingEnum.GBBISIDE
                                };
                            }
                            break;
                    }

                    if (billingCreditPaymentInfo != null)
                    {
                        await _billingRepository.UpdateBillingCreditAsync(userBillingInfo.IdCurrentBillingCredit.Value, billingCreditPaymentInfo);
                    }
                }

                return new OkObjectResult("Successfully");
            }
            catch (DopplerApplicationException e)
            {
                var creditCardNumber = paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? paymentMethod.CCNumber.Replace(" ", "") : string.Empty;
                var lasFourDigits = !string.IsNullOrEmpty(creditCardNumber) ? creditCardNumber[^4..] : string.Empty;
                await CreateUserPaymentHistory(userInformation.IdUser, (int)Enum.Parse<PaymentMethodEnum>(paymentMethod.PaymentMethodName), 0, PaymentStatusEnum.DeclinedPaymentTransaction.ToDescription(), 0, e.Message, "PaymentMethod", lasFourDigits);

                var cardNumberDetails = paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? "with credit card's last 4 digits: " + paymentMethod.CCNumber[^4..] : "";
                var messageError = $"Failed at updating payment method for user {accountname} {cardNumberDetails}. Exception {e.Message}.";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);

                await CheckattemptsToCancelUser(userInformation.IdUser, accountname);

                return new BadRequestObjectResult(e.Message);
            }
        }
        [Authorize(Policies.PROVISORY_USER_OR_SUPER_USER)]
        [HttpPost("accounts/{accountname}/payments/reprocess/send-contact-information-notification")]
        public async Task<IActionResult> SendContactInformationForTransfer(string accountname, ReprocessByTransferUserData userData)
        {
            var user = await _userRepository.GetUserInformation(accountname);

            if (user == null)
            {
                return new NotFoundObjectResult("The user does not exist");
            }

            await _emailTemplatesService.SendContactInformationForTransferNotification(user.IdUser, userData.UserName, userData.UserLastname, userData.UserEmail, userData.PhoneNumber);
            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpGet("/accounts/{accountname}/invoices")]
        public async Task<IActionResult> GetInvoices(string accountname, [FromQuery] PaymentStatusApiEnum[] withStatus)
        {
            var user = await _userRepository.GetUserInformation(accountname);

            if (user == null)
            {
                return new NotFoundObjectResult("The user does not exist");
            }
            var mappedStatus = withStatus.Select(x => x.MapToPaymentStatusEnum()).ToArray();

            var invoices = await _billingRepository.GetInvoices(user.IdUser, mappedStatus);

            var invoicesData = invoices.Select(invoice => new InvoiceData()
            {
                Date = invoice.Date,
                InvoiceNumber = invoice.InvoiceNumber,
                Amount = invoice.Amount,
                Error = (invoice.Status == PaymentStatusEnum.Pending) ? "Pending" : invoice.ErrorMessage,
                Status = invoice.Status.MapToPaymentStatusApiEnum(),
            })
            .ToList();
            var totalPending = invoices.Where(x => x.Status != PaymentStatusEnum.Approved).Sum(x => x.Amount);
            return new OkObjectResult(new GetInvoicesResult()
            {
                Invoices = invoicesData
            });
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpPut("/accounts/{accountname}/payments/reprocess")]
        public async Task<IActionResult> Reprocess(string accountname)
        {
            using var _ = _timeCollector.StartScope();

            try
            {
                var userBillingInfo = await _userRepository.GetUserBillingInformation(accountname);

                var user = await _userRepository.GetUserInformation(accountname);

                var invoices = await _billingRepository.GetInvoices(user.IdUser,
                    PaymentStatusEnum.DeclinedPaymentTransaction,
                    PaymentStatusEnum.ClientPaymentTransactionError,
                    PaymentStatusEnum.FailedPaymentTransaction,
                    PaymentStatusEnum.DoNotHonorPaymentResponse,
                    PaymentStatusEnum.MercadopagoCardException);

                if (invoices.Count == 0)
                {
                    _logger.LogError("Invoices with accountname: {accountname} were not found.", accountname);
                    return new BadRequestObjectResult("No invoice found with status declined");
                }

                var invoicesPaymentResults = new List<ReprocessInvoicePaymentResult>();

                foreach (var invoice in invoices)
                {
                    ReprocessInvoicePaymentResult reprocessResult;

                    if ((userBillingInfo.PaymentMethod == PaymentMethodEnum.MP && invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.QBL) ||
                        (userBillingInfo.PaymentMethod == PaymentMethodEnum.CC && invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.MERCADOPAGO) ||
                        (userBillingInfo.PaymentMethod == PaymentMethodEnum.TRANSF && (invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.QBL || invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.MERCADOPAGO)) ||
                        (userBillingInfo.PaymentMethod == PaymentMethodEnum.DA && (invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.QBL || invoice.IdInvoiceBillingType == (int)InvoiceBillingTypeEnum.MERCADOPAGO)))
                    {
                        await GenerateWithoutRefundAsync(accountname, invoice);

                        var cardNumber = string.Empty;
                        var holderName = string.Empty;
                        CreditCardPayment payment = null;
                        AccountingEntry paymentEntry = null;

                        if (userBillingInfo.PaymentMethod == PaymentMethodEnum.MP || userBillingInfo.PaymentMethod == PaymentMethodEnum.CC)
                        {
                            var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                            if (encryptedCreditCard == null)
                            {
                                var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                                _logger.LogError(messageError);
                                await _slackService.SendNotification(messageError);
                                return new ObjectResult("User credit card missing")
                                {
                                    StatusCode = 500
                                };
                            }

                            payment = await CreateCreditCardPayment(invoice.Amount, user.IdUser, accountname, userBillingInfo.PaymentMethod, false, false, encryptedCreditCard);

                            var accountEntyMapper = GetAccountingEntryMapper(userBillingInfo.PaymentMethod);
                            AccountingEntry invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(invoice.Amount, userBillingInfo.IdUser, invoice.Source, payment, AccountTypeEnum.User);

                            if (payment.Status == PaymentStatusEnum.Approved)
                            {
                                paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                            }

                            await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);

                            if (userBillingInfo.PaymentMethod == PaymentMethodEnum.CC ||
                                userBillingInfo.PaymentMethod == PaymentMethodEnum.MP)
                            {
                                cardNumber = _encryptionService.DecryptAES256(encryptedCreditCard.Number);
                                holderName = _encryptionService.DecryptAES256(encryptedCreditCard.HolderName);
                            }
                        }
                        else
                        {
                            payment = new CreditCardPayment { Status = PaymentStatusEnum.Approved, AuthorizationNumber = string.Empty };
                        }

                        var billingCredit = await _billingRepository.GetBillingCredit(userBillingInfo.IdCurrentBillingCredit ?? 0);
                        var importedBillingDetail = await _billingRepository.GetImportedBillingDetailAsync(invoice.IdBillingSource ?? 0);

                        if (billingCredit != null)
                        {
                            await _sapService.SendBillingToSap(
                                BillingHelper.MapBillingToSapToReprocessAsync(_sapSettings.Value,
                                    importedBillingDetail,
                                    billingCredit,
                                    cardNumber,
                                    holderName,
                                    payment != null ? payment.AuthorizationNumber : string.Empty,
                                    invoice,
                                    paymentEntry != null ? paymentEntry.Date : null),
                                accountname);
                        }
                        else
                        {
                            var slackMessage = $"Could not send invoice to SAP because the BillingCredit is null, User: {accountname} ";
                            await _slackService.SendNotification(slackMessage);
                        }


                        reprocessResult = payment.Status switch
                        {
                            PaymentStatusEnum.Pending => new ReprocessInvoicePaymentResult() { Result = payment.Status, Amount = invoice.Amount },
                            PaymentStatusEnum.Approved => new ReprocessInvoicePaymentResult() { Result = payment.Status },
                            _ => new ReprocessInvoicePaymentResult() { Result = PaymentStatusEnum.DeclinedPaymentTransaction, PaymentError = payment.StatusDetails, Amount = invoice.Amount, InvoiceNumber = invoice.InvoiceNumber },
                        };

                        invoicesPaymentResults.Add(reprocessResult);
                    }
                    else
                    {
                        reprocessResult = await ReprocessInvoicePayment(invoice, accountname, user, userBillingInfo);
                        invoicesPaymentResults.Add(reprocessResult);
                    }
                }

                var invoicesResults = invoicesPaymentResults.Select(x => x.Result);

                if (invoicesResults.All(x => x.Equals(PaymentStatusEnum.DeclinedPaymentTransaction)))
                {
                    await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Fallido", invoices.Sum(x => x.Amount), userBillingInfo.PaymentMethod.ToDescription());
                    return new BadRequestObjectResult("No invoice was processed succesfully");
                }

                _userRepository.UnblockAccountNotPayed(accountname);

                if (invoicesResults.Any(x => x == PaymentStatusEnum.Pending))
                {
                    var failedAndPendingInvoicesAmount = invoicesPaymentResults.Where(x => x.Result != PaymentStatusEnum.Approved).Sum(x => x.Amount);

                    await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Pendiente", failedAndPendingInvoicesAmount, userBillingInfo.PaymentMethod.ToDescription());
                    return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = false, anyPendingInvoices = true });
                }

                if (invoicesResults.All(x => x.Equals(PaymentStatusEnum.Approved)))
                {
                    await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Exitoso", 0.0M, userBillingInfo.PaymentMethod.ToDescription());
                    return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = true, anyPendingInvoices = false });
                }
                else
                {
                    var failedInvoicesAmount = invoicesPaymentResults.Where(x => x.Result == PaymentStatusEnum.DeclinedPaymentTransaction).Sum(x => x.Amount);

                    await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Parcialmente exitoso", failedInvoicesAmount, userBillingInfo.PaymentMethod.ToDescription());
                    return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = false, anyPendingInvoices = false });
                }
            }
            catch (Exception e)
            {
                var messageError = $"Failed ro reprocess for user {accountname}. Exception {e.Message}.";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);

                return new BadRequestObjectResult(e.Message);
            }
        }

        private async Task<ReprocessInvoicePaymentResult> ReprocessInvoicePayment(AccountingEntry invoice, string accountname, User user, UserBillingInformation userBillingInfo)
        {
            var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);

            if (encryptedCreditCard == null)
            {
                var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
                return new ReprocessInvoicePaymentResult() { Result = PaymentStatusEnum.DeclinedPaymentTransaction, PaymentError = "Missing credit card information", InvoiceNumber = invoice.InvoiceNumber, Amount = invoice.Amount };
            }

            if (invoice.Amount <= 0)
            {
                return new ReprocessInvoicePaymentResult() { Result = PaymentStatusEnum.DeclinedPaymentTransaction, PaymentError = "Invoice amount was less or equal than 0", InvoiceNumber = invoice.InvoiceNumber, Amount = invoice.Amount };
            }

            var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);

            var payment = await CreateCreditCardPayment(invoice.Amount, user.IdUser, accountname, userBillingInfo.PaymentMethod, currentPlan == null, true, encryptedCreditCard);

            if (payment.Status == PaymentStatusEnum.DeclinedPaymentTransaction)
            {
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, payment.Status, payment.StatusDetails, invoice.AuthorizationNumber);
                return new ReprocessInvoicePaymentResult() { Result = PaymentStatusEnum.DeclinedPaymentTransaction, PaymentError = payment.StatusDetails, Amount = invoice.Amount, InvoiceNumber = invoice.InvoiceNumber };
            }

            if (payment.Status == PaymentStatusEnum.Pending)
            {
                return new ReprocessInvoicePaymentResult() { Result = payment.Status, Amount = invoice.Amount };
            }

            if (payment.Status == PaymentStatusEnum.Approved)
            {
                var accountingEntryMapper = GetAccountingEntryMapper(userBillingInfo.PaymentMethod);
                invoice.AuthorizationNumber = payment.AuthorizationNumber;
                var paymentEntry = await accountingEntryMapper.MapToPaymentAccountingEntry(invoice, encryptedCreditCard);
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, payment.Status, payment.StatusDetails, invoice.AuthorizationNumber);
                await _billingRepository.CreatePaymentEntryAsync(invoice.IdAccountingEntry, paymentEntry);

                return new ReprocessInvoicePaymentResult() { Result = payment.Status };
            }
            return new ReprocessInvoicePaymentResult() { Result = PaymentStatusEnum.DeclinedPaymentTransaction, PaymentError = payment.StatusDetails, Amount = invoice.Amount, InvoiceNumber = invoice.InvoiceNumber };
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/plans/current")]
        public async Task<IActionResult> GetCurrentPlan(string accountname)
        {
            _logger.LogDebug("Get current plan.");

            var currentPlan = await _billingRepository.GetCurrentPlan(accountname);

            if (currentPlan == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPlan);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpGet("/accounts/{accountname}/plans/{planType}/current")]
        public async Task<IActionResult> GetCurrentPlanByType(string accountname, [FromRoute] int planType)
        {
            _logger.LogDebug("Get current plan.");

            CurrentPlan currentPlan = null;
            switch (planType)
            {
                case (int)PlanTypeEnum.Marketing:
                    currentPlan = await _billingRepository.GetCurrentPlan(accountname);
                    break;
                case (int)PlanTypeEnum.Chat:
                    currentPlan = await _chatPlanUserRepository.GetCurrentPlan(accountname);
                    break;
            }

            if (currentPlan == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(currentPlan);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/agreements")]
        public async Task<IActionResult> CreateAgreement([FromRoute] string accountname, [FromBody] AgreementInformation agreementInformation)
        {
            using var _ = _timeCollector.StartScope();

            var user = await _userRepository.GetUserInformation(accountname);

            if (!user.IdClientManager.HasValue)
            {
                var userBillingInformation = await _userRepository.GetUserBillingInformation(accountname);

                return await CreateAgreementToUser(accountname, agreementInformation, userBillingInformation);
            }
            else
            {
                var userBillingInformation = await _clientManagerRepository.GetUserBillingInformation(user.IdClientManager.Value);
                return await CreateAgreementToCM(accountname, agreementInformation, userBillingInformation);
            }
        }

        private async Task<IActionResult> CreateAgreementToUser(string accountname, AgreementInformation agreementInformation, UserBillingInformation user)
        {
            try
            {
                var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
                if (!results.IsValid)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Validation error {results.ToString("-")}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult(results.ToString("-"));
                }

                if (user == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid user");
                }

                if (user.IdBillingCountry == 0)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid country";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid country");
                }

                if (user.IsCancelated)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Canceled user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("UserCanceled");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == user.PaymentMethod))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid payment method {user.PaymentMethod}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (user.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == user.IdBillingCountry))
                {
                    var messageErrorTransference = $"Failed at creating new agreement for user {accountname}, payment method {user.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var currentPlan = await _userRepository.GetUserCurrentTypePlan(user.IdUser);
                if (currentPlan != null && !AllowedUpdatePlanTypesForBilling.Any(p => p == currentPlan.IdUserType))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user type (only free users or upgrade between 'Montly' and 'Contacts' plans) {currentPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid user type (only free users or upgrade between 'Montly' plans)");
                }

                var newPlan = await _userRepository.GetUserNewTypePlan(agreementInformation.PlanId);
                if (newPlan == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan");
                }

                if (currentPlan != null && currentPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                {
                    if (currentPlan.SubscribersQty > newPlan.SubscribersQty)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}. Only supports upselling.";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new BadRequestObjectResult("Invalid selected plan. Only supports upselling.");
                    }
                }

                if (currentPlan != null && currentPlan.IdUserType == UserTypeEnum.MONTHLY)
                {
                    if (currentPlan.EmailQty > newPlan.EmailQty)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}. Only supports upselling.";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new BadRequestObjectResult("Invalid selected plan. Only supports upselling.");
                    }
                }

                if (!AllowedPlanTypesForBilling.Any(p => p == newPlan.IdUserType))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, invalid selected plan type {newPlan.IdUserType}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid selected plan type");
                }

                var currentChatPlan = await _chatPlanUserRepository.GetCurrentPlan(user.Email);

                //if (agreementInformation.AdditionalServices != null && agreementInformation.AdditionalServices.Select(s => s.Type).Contains(AdditionalServiceTypeEnum.Chat))
                //{
                //    var additionalServiceChatPlan = agreementInformation.AdditionalServices.FirstOrDefault(s => s.Type == AdditionalServiceTypeEnum.Chat);
                //    var chatPlan = await _chatPlanRepository.GetById(additionalServiceChatPlan.PlanId ?? 0);
                //    var currentChatPlan = await _chatPlanUserRepository.GetCurrentPlan(user.Email);

                //    if (currentChatPlan != null && currentChatPlan.ConversationQty > chatPlan.ConversationQty)
                //    {
                //        var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected chat plan {chatPlan.IdChatPlan}. Only supports upselling.";
                //        _logger.LogError(messageError);
                //        await _slackService.SendNotification(messageError);
                //        return new BadRequestObjectResult("Invalid selected chat plan. Only supports upselling.");
                //    }
                //}

                //TODO: Check the current error
                //var isValidTotal = await _accountPlansService.IsValidTotal(accountname, agreementInformation);
                //if (!isValidTotal)
                //{
                //    var messageError = $"Failed at creating new agreement for user {accountname}, Total of agreement is not valid";
                //    _logger.LogError(messageError);
                //    await _slackService.SendNotification(messageError);
                //    return new BadRequestObjectResult("Total of agreement is not valid");
                //}

                Promotion promotion = null;
                if (!string.IsNullOrEmpty(agreementInformation.Promocode))
                {
                    try
                    {
                        promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error to validate the promocode from de API {agreementInformation.Promocode}.");

                        var encryptedCode = _encryptionService.EncryptAES256(agreementInformation.Promocode);
                        promotion = await _promotionRepository.GetPromotionByCode(encryptedCode, (int)newPlan.IdUserType, agreementInformation.PlanId);
                    }
                }

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                CreditCardPayment payment = null;

                agreementInformation.AdditionalServices ??= [];

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    (user.PaymentMethod == PaymentMethodEnum.CC || user.PaymentMethod == PaymentMethodEnum.MP))
                {
                    encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);

                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    payment = await CreateCreditCardPayment(agreementInformation.Total.Value, user.IdUser, accountname, user.PaymentMethod, currentPlan == null, false, encryptedCreditCard);

                    var accountEntyMapper = GetAccountingEntryMapper(user.PaymentMethod);
                    AccountingEntry invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(agreementInformation.Total.Value, user, newPlan, payment, AccountTypeEnum.User);
                    AccountingEntry paymentEntry = null;
                    authorizationNumber = payment.AuthorizationNumber;

                    if (payment.Status == PaymentStatusEnum.Approved)
                    {
                        paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                    }

                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);
                }

                var marketingBillingCreditId = 0;
                var partialBalance = 0;

                if (currentPlan == null)
                {
                    var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                    var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, null, BillingCreditTypeEnum.UpgradeRequest);
                    marketingBillingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);
                    user.IdCurrentBillingCredit = marketingBillingCreditId;
                    user.OriginInbound = agreementInformation.OriginInbound;
                    user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);
                    user.UTCFirstPayment = user.UpgradePending.HasValue && !user.UpgradePending.Value ? DateTime.UtcNow : null;
                    user.UTCUpgrade = user.UTCFirstPayment;

                    if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS && newPlan.SubscribersQty.HasValue && user.UpgradePending.HasValue && !user.UpgradePending.Value)
                    {
                        user.MaxSubscribers = newPlan.SubscribersQty.Value;
                    }

                    await _userRepository.UpdateUserBillingCredit(user);

                    partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);

                    if (user.UpgradePending.HasValue && !user.UpgradePending.Value)
                    {
                        if (newPlan.IdUserType != UserTypeEnum.SUBSCRIBERS)
                        {
                            await _billingRepository.CreateMovementCreditAsync(marketingBillingCreditId, partialBalance, user, newPlan);
                        }
                        else
                        {
                            await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                        }

                        User userInformation = await _userRepository.GetUserInformation(accountname);
                        var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                        if (activatedStandByAmount > 0)
                        {
                            var lang = userInformation.Language ?? "en";
                            await _emailTemplatesService.SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                        }
                    }

                    if (promotion != null)
                        await _promotionRepository.IncrementUsedTimes(promotion);

                    var status = user.UpgradePending.HasValue && !user.UpgradePending.Value ? PaymentStatusEnum.Approved.ToDescription() : PaymentStatusEnum.Pending.ToDescription();
                    await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, marketingBillingCreditId, string.Empty, Source);

                    //Send notifications
                    SendNotifications(accountname, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.UpgradeRequest, currentPlan, null);
                }
                else
                {
                    if (newPlan.IdUserType == UserTypeEnum.MONTHLY)
                    {
                        if (currentPlan.IdUserTypePlan != newPlan.IdUserTypePlan && currentPlan.IdUserType == UserTypeEnum.MONTHLY)
                        {
                            marketingBillingCreditId = await ChangeBetweenMonthlyPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                        else
                        {
                            if (currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                            {
                                marketingBillingCreditId = await ChangeToMonthly(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                            }
                        }
                    }
                    else
                    {
                        if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                        {
                            if (currentPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                            {
                                marketingBillingCreditId = await ChangeBetweenSubscribersPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                            }
                            else
                            {
                                marketingBillingCreditId = await ChangeToSubscribers(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                            }
                        }
                        else if (currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL && newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                        {
                            marketingBillingCreditId = await BuyCredits(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                    }
                }

                if (agreementInformation.AdditionalServices.Select(s => s.Type).Contains(AdditionalServiceTypeEnum.Chat))
                {
                    if (marketingBillingCreditId == 0)
                    {
                        var marketingBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
                        marketingBillingCreditId = marketingBillingCredit.IdBillingCredit;
                    }

                    var chatPlanBillingCreditId = await BuyChatPlan(user, agreementInformation, payment, currentChatPlan, AccountTypeEnum.User);
                }
                else
                {
                    if (currentChatPlan != null)
                    {
                        await CancelChatPlan(user);
                    }
                }

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    ((user.PaymentMethod == PaymentMethodEnum.CC) ||
                    (user.PaymentMethod == PaymentMethodEnum.MP) ||
                    (user.PaymentMethod == PaymentMethodEnum.TRANSF && user.IdBillingCountry == (int)CountryEnum.Argentina) ||
                    (user.PaymentMethod == PaymentMethodEnum.DA)))
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(marketingBillingCreditId);
                    var cardNumber = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number) : "";
                    var holderName = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.HolderName) : "";
                    var sapAdditionalServices = await GenerateAdditionalServcies(agreementInformation, user, billingCredit, currentChatPlan);

                    var totalChatPlan = sapAdditionalServices.Sum(c => c.Charge);
                    var total = agreementInformation.Total - (decimal)totalChatPlan;
                    if (billingCredit != null)
                    {
                        await _sapService.SendBillingToSap(
                            BillingHelper.MapBillingToSapAsync(_sapSettings.Value,
                                cardNumber,
                                holderName,
                                billingCredit,
                                currentPlan,
                                newPlan,
                                authorizationNumber,
                                invoiceId,
                                total,
                                sapAdditionalServices),
                            accountname);
                    }
                    else
                    {
                        var slackMessage = $"Could not send invoice to SAP because the BillingCredit is null, User: {accountname} ";
                        await _slackService.SendNotification(slackMessage);
                    }
                }

                var userType = currentPlan == null ? "Free user" : "Update plan";
                var message = $"Successful at creating a new agreement for: User: {accountname} - Plan: {agreementInformation.PlanId} - {userType}";
                await _slackService.SendNotification(message + (!string.IsNullOrEmpty(agreementInformation.Promocode) ? $" - Promocode {agreementInformation.Promocode}" : string.Empty));

                if (currentPlan == null)
                {
                    if (_zohoSettings.Value.UseZoho)
                    {
                        ZohoDTO zohoDto = new ZohoDTO()
                        {
                            Email = user.Email,
                            Doppler = newPlan.IdUserType.ToDescription(),
                            BillingSystem = user.PaymentMethod.ToString(),
                            OriginInbound = agreementInformation.OriginInbound
                        };

                        if (user.UpgradePending.HasValue && !user.UpgradePending.Value)
                        {
                            zohoDto.UpgradeDate = DateTime.UtcNow;
                            zohoDto.FirstPaymentDate = DateTime.UtcNow;
                        }

                        if (promotion != null)
                        {
                            zohoDto.PromoCodo = agreementInformation.Promocode;
                            if (promotion.ExtraCredits.HasValue && promotion.ExtraCredits.Value != 0)
                                zohoDto.DiscountType = ZohoDopplerValues.Credits;
                            else if (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value != 0)
                                zohoDto.DiscountType = ZohoDopplerValues.Discount;
                        }

                        try
                        {
                            await _zohoService.RefreshTokenAsync();
                            var contact = await _zohoService.SearchZohoEntityAsync<ZohoEntityContact>("Contacts", string.Format("Email:equals:{0}", zohoDto.Email));
                            if (contact == null)
                            {
                                var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityLead>>("Leads", string.Format("Email:equals:{0}", zohoDto.Email));
                                if (response != null)
                                {
                                    var lead = response.Data.FirstOrDefault();
                                    BillingHelper.MapForUpgrade(lead, zohoDto);
                                    var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityLead> { Data = new List<ZohoEntityLead> { lead } }, settings);
                                    await _zohoService.UpdateZohoEntityAsync(body, lead.Id, "Leads");
                                }
                            }
                            else
                            {
                                if (contact.AccountName != null && !string.IsNullOrEmpty(contact.AccountName.Name))
                                {
                                    var response = await _zohoService.SearchZohoEntityAsync<ZohoResponse<ZohoEntityAccount>>("Accounts", string.Format("Account_Name:equals:{0}", contact.AccountName.Name));
                                    if (response != null)
                                    {
                                        var account = response.Data.FirstOrDefault();
                                        BillingHelper.MapForUpgrade(account, zohoDto);
                                        var body = JsonConvert.SerializeObject(new ZohoUpdateModel<ZohoEntityAccount> { Data = new List<ZohoEntityAccount> { account } }, settings);
                                        await _zohoService.UpdateZohoEntityAsync(body, account.Id, "Accounts");
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            var messageError = $"Failed at updating lead from zoho {accountname} with exception {e.Message}";
                            _logger.LogError(e, messageError);
                            await _slackService.SendNotification(messageError);
                        }
                    }
                }

                return new OkObjectResult("Successfully");
            }
            catch (Exception e)
            {
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, PaymentStatusEnum.DeclinedPaymentTransaction.ToDescription(), 0, e.Message, Source);

                var cardNumber = string.Empty;
                if (user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    CreditCard encryptedCreditCard = encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    cardNumber = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number)[^4..] : "";
                }

                var cardNumberDetails = !string.IsNullOrEmpty(cardNumber) ? "with credit card's last 4 digits: " + cardNumber : "";
                var messageError = $"Failed at creating new agreement for user {accountname} {cardNumberDetails}. Exception {e.Message}";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new ObjectResult("Failed at creating new agreement")
                {
                    StatusCode = 500,
                    Value = e.Message,
                };
            }
        }

        private async Task<IActionResult> CreateAgreementToCM(string accountname, AgreementInformation agreementInformation, UserBillingInformation clientManager)
        {
            try
            {
                var results = await _agreementInformationValidator.ValidateAsync(agreementInformation);
                if (!results.IsValid)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Validation error {results.ToString("-")}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult(results.ToString("-"));
                }

                if (clientManager == null)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid user");
                }

                if (clientManager.IdBillingCountry == 0)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid country";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid country");
                }

                if (clientManager.IsCancelated)
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Canceled user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("UserCanceled");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == clientManager.PaymentMethod))
                {
                    var messageError = $"Failed at creating new agreement for user {accountname}, Invalid payment method {clientManager.PaymentMethod}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (clientManager.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == clientManager.IdBillingCountry))
                {
                    var messageErrorTransference = $"Failed at creating new agreement for user {accountname}, payment method {clientManager.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var userFromCM = await _userRepository.GetUserBillingInformation(accountname);
                var currentChatPlan = await _chatPlanUserRepository.GetCurrentPlan(userFromCM.Email);

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                CreditCardPayment payment = null;

                agreementInformation.AdditionalServices ??= [];

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    (clientManager.PaymentMethod == PaymentMethodEnum.CC || clientManager.PaymentMethod == PaymentMethodEnum.MP))
                {
                    encryptedCreditCard = await _clientManagerRepository.GetEncryptedCreditCard(clientManager.IdUser);

                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, missing credit card information";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    payment = await CreateCreditCardPayment(agreementInformation.Total.Value, clientManager.IdUser, accountname, clientManager.PaymentMethod, false, false, encryptedCreditCard);

                    var accountEntyMapper = GetAccountingEntryMapper(clientManager.PaymentMethod);
                    AccountingEntry invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(agreementInformation.Total.Value, clientManager, new UserTypePlanInformation { IdUserTypePlan = (int)UserTypeEnum.MONTHLY }, payment, AccountTypeEnum.CM);
                    AccountingEntry paymentEntry = null;
                    authorizationNumber = payment.AuthorizationNumber;

                    if (payment.Status == PaymentStatusEnum.Approved)
                    {
                        paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                    }

                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);
                }

                var marketingBillingCreditId = 0;

                if (agreementInformation.AdditionalServices.Select(s => s.Type).Contains(AdditionalServiceTypeEnum.Chat))
                {
                    var marketingBillingCredit = await _billingRepository.GetBillingCredit(userFromCM.IdCurrentBillingCredit.Value);
                    marketingBillingCreditId = marketingBillingCredit.IdBillingCredit;

                    var chatPlanBillingCreditId = await BuyChatPlan(userFromCM, agreementInformation, payment, currentChatPlan, AccountTypeEnum.CM);
                }
                else
                {
                    if (currentChatPlan != null)
                    {
                        await CancelChatPlan(userFromCM);
                    }
                }

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    ((clientManager.PaymentMethod == PaymentMethodEnum.CC) ||
                    (clientManager.PaymentMethod == PaymentMethodEnum.MP) ||
                    (clientManager.PaymentMethod == PaymentMethodEnum.TRANSF && clientManager.IdBillingCountry == (int)CountryEnum.Argentina) ||
                    (clientManager.PaymentMethod == PaymentMethodEnum.DA)))
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(userFromCM.IdCurrentBillingCredit.Value);
                    var cardNumber = clientManager.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number) : "";
                    var holderName = clientManager.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.HolderName) : "";
                    var sapAdditionalServices = await GenerateAdditionalServcies(agreementInformation, userFromCM, billingCredit, currentChatPlan);
                    var userTypePlanInformation = new UserTypePlanInformation { IdUserType = UserTypeEnum.CM_MONTHLY };

                    var totalChatPlan = sapAdditionalServices.Sum(c => c.Charge);
                    var total = agreementInformation.Total - (decimal)totalChatPlan;
                    if (billingCredit != null)
                    {
                        var billingSystem = ResponsabileBillingEnum.QBL;

                        switch (clientManager.PaymentMethod)
                        {
                            case PaymentMethodEnum.CC:
                                billingSystem = ResponsabileBillingEnum.QBL;
                                break;
                            case PaymentMethodEnum.DA:
                            case PaymentMethodEnum.TRANSF:
                                billingSystem = ResponsabileBillingEnum.GBBISIDE;
                                break;
                            case PaymentMethodEnum.MP:
                                billingSystem = ResponsabileBillingEnum.Mercadopago;
                                break;
                        }

                        billingCredit.IdUser = clientManager.IdUser;
                        billingCredit.Cuit = clientManager.Cuit;
                        billingCredit.IdResponsabileBilling = (int)billingSystem;

                        await _sapService.SendBillingToSap(
                            BillingHelper.MapBillingToSapAsync(_sapSettings.Value,
                                cardNumber,
                                holderName,
                                billingCredit,
                                null,
                                userTypePlanInformation,
                                authorizationNumber,
                                invoiceId,
                                total,
                                sapAdditionalServices),
                            accountname);
                    }
                    else
                    {
                        var slackMessage = $"Could not send invoice to SAP because the BillingCredit is null, User: {accountname} ";
                        await _slackService.SendNotification(slackMessage);
                    }
                }

                var message = $"CM - Successful at creating a new agreement for: User: {accountname} - Conversations plan: {agreementInformation.AdditionalServices.FirstOrDefault().PlanId}";
                await _slackService.SendNotification(message);

                return new OkObjectResult("Successfully");
            }
            catch (Exception e)
            {
                return new ObjectResult("Failed at creating new agreement")
                {
                    StatusCode = 500,
                    Value = e.Message,
                };
            }
        }

        private async Task<List<SapAdditionalServiceDto>> GenerateAdditionalServcies(AgreementInformation agreementInformation, UserBillingInformation user, BillingCredit billingCredit, CurrentPlan currentChatPlan)
        {
            var sapAdditionalServices = new List<SapAdditionalServiceDto>();
            foreach (var additionalService in agreementInformation.AdditionalServices)
            {
                if (additionalService.Type == AdditionalServiceTypeEnum.Chat)
                {
                    var conversationPlan = await _chatPlanRepository.GetById(additionalService.PlanId.Value);
                    var isUpSelling = (currentChatPlan != null && currentChatPlan.Fee > 0);

                    if ((currentChatPlan != null && currentChatPlan.ConversationQty != conversationPlan.ConversationQty) ||
                        currentChatPlan == null)
                    {
                        var planFee = (double)conversationPlan.Fee * (billingCredit.TotalMonthPlan ?? 1);
                        var sapAdditionalServiceDto = new SapAdditionalServiceDto
                        {
                            Charge = !isUpSelling ? planFee : (double)additionalService.Charge.Value,
                            ConversationQty = conversationPlan.ConversationQty,
                            IsUpSelling = isUpSelling,
                            Type = AdditionalServiceTypeEnum.Chat,
                            IsCustom = additionalService.IsCustom,
                            UserEmail = user.Email
                        };

                        sapAdditionalServices.Add(sapAdditionalServiceDto);
                    }
                }
            }

            return sapAdditionalServices;
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/purchase-intention")]
        public async Task<IActionResult> UpdateLastPurchaseIntentionDate(string accountname)
        {
            var result = await _userRepository.UpdateUserPurchaseIntentionDate(accountname);

            if (result.Equals(0))
            {
                return new BadRequestObjectResult("Failed updating purchase intention. Invalid account.");
            }

            return new OkObjectResult("Successfully");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPost("/accounts/{accountname}/landings/buy")]
        public async Task<ActionResult> BuyLandingPlans(string accountname, [FromBody] BuyLandingPlans buyLandingPlans)
        {
            var user = await _userRepository.GetUserBillingInformation(accountname);

            try
            {
                if (user == null)
                {
                    var messageError = $"Failed at buy a landing plan for user {accountname}, Invalid user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new NotFoundObjectResult("Invalid user");
                }

                if (user.IsCancelated)
                {
                    var messageError = $"Failed at buy a landing plan for user {accountname}, Canceled user";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("UserCanceled");
                }

                if (!AllowedPaymentMethodsForBilling.Any(p => p == user.PaymentMethod))
                {
                    var messageError = $"Failed at buy a landing plan for user {accountname}, Invalid payment method {user.PaymentMethod}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                if (user.PaymentMethod == PaymentMethodEnum.TRANSF && !AllowedCountriesForTransfer.Any(p => (int)p == user.IdBillingCountry))
                {
                    var messageErrorTransference = $"Failed at buy a landing plan for user {accountname}, payment method {user.PaymentMethod} it's only supported for {AllowedCountriesForTransfer.Select(p => p)}";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid payment method");
                }

                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit ?? 0);
                if (currentBillingCredit == null || currentBillingCredit.ActivationDate == null)
                {
                    var messageErrorTransference = $"Failed at buy a landing plan for user {accountname}. The user has not an active marketing plan";
                    _logger.LogError(messageErrorTransference);
                    await _slackService.SendNotification(messageErrorTransference);
                    return new BadRequestObjectResult("Invalid marketing plan");
                }

                /* Get current information about landing plans */
                var currentLandingBillingCredit = await _billingRepository.GetCurrentBillingCreditForLanding(user.IdUser);
                IList<LandingPlanUser> currentLandingPlans = [];

                if (currentLandingBillingCredit != null)
                {
                    currentLandingPlans = await _landingPlanUserRepository.GetLandingPlansByUserIdAndBillingCreditIdAsync(user.IdUser, currentLandingBillingCredit.IdBillingCredit);
                }

                PlanAmountDetails amountDetails = null;
                try
                {
                    amountDetails = await _accountPlansService.GetCalculateLandingUpgrade(
                        user.Email,
                        buyLandingPlans.LandingPlans.Select(x => x.LandingPlanId),
                        buyLandingPlans.LandingPlans.Select(x => x.PackQty));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error to get total landing amount for user {user.Email}.(Send Notifications)");
                }

                var currentLandingAmount = currentLandingPlans.Sum(l => l.PackQty * l.Fee);
                var newLandingAmount = buyLandingPlans.LandingPlans.Sum(l => l.PackQty * l.Fee);

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                CreditCardPayment payment = null;

                if (buyLandingPlans.Total.GetValueOrDefault() > 0 &&
                    (user.PaymentMethod == PaymentMethodEnum.CC || user.PaymentMethod == PaymentMethodEnum.MP))
                {
                    encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    if (encryptedCreditCard == null)
                    {
                        var messageError = $"Failed at buy a landing plan for user {accountname}, missing credit card information";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new ObjectResult("User credit card missing")
                        {
                            StatusCode = 500
                        };
                    }

                    payment = await CreateCreditCardPayment(buyLandingPlans.Total.Value, user.IdUser, accountname, user.PaymentMethod, false, false, encryptedCreditCard);

                    var accountEntyMapper = GetAccountingEntryMapper(user.PaymentMethod);
                    AccountingEntry invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(buyLandingPlans.Total.Value, user.IdUser, SourceTypeEnum.BuyLandingId, payment, AccountTypeEnum.User);
                    AccountingEntry paymentEntry = null;
                    authorizationNumber = payment.AuthorizationNumber;

                    if (payment.Status == PaymentStatusEnum.Approved)
                    {
                        paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                    }

                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);
                }

                var isPaymentPending = BillingHelper.IsUpgradePending(user, null, payment);
                var billingCreditType = (newLandingAmount - currentLandingAmount > 0) ?
                                        user.PaymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.Landing_Buyed_CC : BillingCreditTypeEnum.Landing_Request :
                                        BillingCreditTypeEnum.Downgrade_Between_Landings;

                var billingCreditMapper = GetAddOnBillingCreditMapper(user.PaymentMethod);

                var total = buyLandingPlans.LandingPlans.Sum(l => l.PackQty * l.Fee);
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(total, user, currentBillingCredit, payment, billingCreditType);
                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                /* Save current billing credit in the UserAddOn table */
                await _userAddOnRepository.SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(user.IdUser, (int)AddOnType.Landing, billingCreditId);

                IList<LandingPlanUser> newLandingPlans = [];
                foreach (BuyLandingPlanItem landingPlanItem in buyLandingPlans.LandingPlans)
                {
                    LandingPlanUser newLandingPlanUser = new LandingPlanUser
                    {
                        Created = DateTime.UtcNow,
                        Fee = landingPlanItem.Fee,
                        IdBillingCredit = billingCreditId,
                        IdUser = user.IdUser,
                        PackQty = landingPlanItem.PackQty,
                        IdLandingPlan = landingPlanItem.LandingPlanId
                    };

                    var idLandingPlanUser = await _landingPlanUserRepository.CreateLandingPlanUserAsync(newLandingPlanUser);
                    newLandingPlanUser.IdLandingPlanUser = idLandingPlanUser;
                    newLandingPlans.Add(newLandingPlanUser);
                }

                //Send lading plan to SAP
                if (buyLandingPlans.Total.GetValueOrDefault() > 0 &&
                    ((user.PaymentMethod == PaymentMethodEnum.CC) ||
                    (user.PaymentMethod == PaymentMethodEnum.MP) ||
                    (user.PaymentMethod == PaymentMethodEnum.TRANSF && user.IdBillingCountry == (int)CountryEnum.Argentina) ||
                    (user.PaymentMethod == PaymentMethodEnum.DA)))
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);
                    var cardNumber = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number) : "";
                    var holderName = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.HolderName) : "";

                    var landingsToSendToSap = buyLandingPlans.LandingPlans;

                    foreach (var landingPlan in landingsToSendToSap)
                    {
                        var currentLandingPlan = currentLandingPlans.FirstOrDefault(l => l.IdLandingPlan == landingPlan.LandingPlanId);
                        if (currentLandingPlan != null)
                        {
                            landingPlan.PackQty -= currentLandingPlan.PackQty;
                            landingPlan.Fee = currentLandingPlan.Fee * landingPlan.PackQty;
                        }
                    }

                    if (billingCredit != null)
                    {
                        await _sapService.SendBillingToSap(
                            BillingHelper.MapLandingsBillingToSapAsync(_sapSettings.Value,
                                cardNumber,
                                holderName,
                                billingCredit,
                                landingsToSendToSap.Where(l => l.PackQty > 0).ToList(),
                                authorizationNumber,
                                invoiceId,
                                buyLandingPlans.Total,
                                user),
                            accountname);
                    }
                    else
                    {
                        var slackMessage = $"Could not send invoice to SAP because the BillingCredit is null, User: {accountname} ";
                        await _slackService.SendNotification(slackMessage);
                    }
                }

                //Send notification
                SendLandingNotifications(accountname, user, currentLandingPlans, newLandingPlans, amountDetails);
            }
            catch (Exception e)
            {
                var cardNumber = string.Empty;
                if (user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
                    cardNumber = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number)[^4..] : "";
                }

                var cardNumberDetails = !string.IsNullOrEmpty(cardNumber) ? "with credit card's last 4 digits: " + cardNumber : "";
                var messageError = $"Failed at buy landing plans for user {accountname} {cardNumberDetails}. Exception {e.Message}";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);

                return new ObjectResult("Failed at buying landing")
                {
                    StatusCode = 500,
                    Value = e.Message,
                };
            }

            var message = $"Successful buy landing plans for: User: {accountname}";
            await _slackService.SendNotification(message);

            return new OkObjectResult($"Successful buy landing plans for: User: {accountname}");
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
        [HttpPut("/accounts/{accountname}/landings/cancel")]
        public async Task<IActionResult> CancenlCurrentLandingPlan(string accountname)
        {
            try
            {
                User user = await _userRepository.GetUserInformation(accountname);

                if (user == null)
                {
                    return new NotFoundObjectResult("The user does not exist");
                }

                UserAddOn userAddOn = await _userAddOnRepository.GetByUserIdAndAddOnType(user.IdUser, (int)AddOnType.Landing);

                if (userAddOn == null)
                {
                    return new NotFoundObjectResult("The user does not have any landing plan");
                }

                await _billingRepository.UpdateBillingCreditType(userAddOn.IdCurrentBillingCredit, (int)BillingCreditTypeEnum.Landing_Canceled);

                await _landingPlanUserRepository.CancelLandingPLanByBillingCreditId(userAddOn.IdCurrentBillingCredit);

                return new OkObjectResult($"Successful cancel landing plan for: User: {accountname}");

            }
            catch
            {
                return new ObjectResult("Failed at canceling landing plan")
                {
                    StatusCode = 500,
                };
            }
        }

        private async void SendConversationNotifications(
            string accountname,
            UserBillingInformation user,
            ChatPlan newPlan,
            CurrentPlan currentPlan,
            CreditCardPayment payment,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails,
            AccountTypeEnum accountType)
        {
            User userInformation;
            if (accountType == AccountTypeEnum.User)
            {
                userInformation = await _userRepository.GetUserInformation(accountname);
            }
            else
            {
                userInformation = await _clientManagerRepository.GetUserInformation(accountname);
            }

            bool isUpgradeApproved;


            if (currentPlan == null)
            {
                isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, null, payment));
                await _emailTemplatesService.SendNotificationForUpgradeConversationPlan(accountname, userInformation, newPlan, user, planDiscountInformation, !isUpgradeApproved, true);
            }
            else
            {
                await _emailTemplatesService.SendNotificationForUpdateConversationPlan(accountname, userInformation, newPlan, user, planDiscountInformation, amountDetails, currentPlan);
            }
        }

        private async void SendLandingNotifications(
            string accountname,
            UserBillingInformation user,
            IList<LandingPlanUser> currentLandingPlans,
            IList<LandingPlanUser> newLandingPlans,
            PlanAmountDetails amountDetails)
        {
            User userInformation = await _userRepository.GetUserInformation(accountname);
            IList<LandingPlan> availableLandingPlans = await _landingPlanRepository.GetAll();
            BillingCredit newLandingBillingCredit = await _billingRepository.GetCurrentBillingCreditForLanding(user.IdUser);

            //Upgrade landing plan
            if (currentLandingPlans is null || currentLandingPlans.Count == 0)
            {
                await _emailTemplatesService.SendNotificationForUpgradeLandingPlan(
                    accountname,
                    userInformation,
                    user,
                    availableLandingPlans,
                    newLandingPlans,
                    newLandingBillingCredit);
            }
            else //Update landing plan
            {
                await _emailTemplatesService.SendNotificationForUpdateLandingPlan(
                    accountname,
                    userInformation,
                    user,
                    availableLandingPlans,
                    currentLandingPlans,
                    newLandingPlans,
                    newLandingBillingCredit,
                    amountDetails);
            }
        }

        private async void SendNotifications(
            string accountname,
            UserTypePlanInformation newPlan,
            UserBillingInformation user,
            int partialBalance,
            Promotion promotion,
            string promocode,
            int discountId,
            CreditCardPayment payment,
            BillingCreditTypeEnum billingCreditType,
            UserTypePlanInformation currentPlan,
            PlanAmountDetails amountDetails)
        {
            User userInformation = await _userRepository.GetUserInformation(accountname);
            var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(discountId);
            user.TaxRegimeDescription = await GetTaxRegimeDescription(user.TaxRegime);
            bool isUpgradeApproved;

            switch (billingCreditType)
            {
                case BillingCreditTypeEnum.UpgradeRequest:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));

                    if (newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                    {
                        await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved, true);
                    }
                    else
                    {
                        if (isUpgradeApproved && newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                        {
                            await _emailTemplatesService.SendNotificationForSuscribersPlan(accountname, userInformation, newPlan);
                        }

                        await _emailTemplatesService.SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId, planDiscountInformation, !isUpgradeApproved, true);
                    }

                    return;
                case BillingCreditTypeEnum.Upgrade_Between_Monthlies:
                case BillingCreditTypeEnum.Upgrade_Between_Subscribers:
                    await _emailTemplatesService.SendNotificationForUpdatePlan(accountname, userInformation, currentPlan, newPlan, user, promotion, promocode, discountId, planDiscountInformation, amountDetails);
                    return;
                case BillingCreditTypeEnum.Credit_Buyed_CC:
                case BillingCreditTypeEnum.Credit_Request:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));
                    await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved, true);
                    return;
                case BillingCreditTypeEnum.Individual_to_Monthly:
                case BillingCreditTypeEnum.Individual_to_Subscribers:
                    await _emailTemplatesService.SendNotificationForChangeIndividualToMontlyOrSubscribers(accountname, userInformation, currentPlan, newPlan, user, promotion, promocode, discountId, planDiscountInformation, amountDetails);
                    return;
                default:
                    return;
            }
        }

        private async Task<CreditCardPayment> CreateCreditCardPayment(decimal total, int userId, string accountname, PaymentMethodEnum paymentMethod, bool isFreeUser, bool isReprocessCall, CreditCard encryptedCreditCard)
        {
            //var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);

            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                    var authorizationNumber = _cloverSettings.Value.UseCloverApi ? await _cloverService.CreateCreditCardPayment(accountname, total, encryptedCreditCard, userId, isFreeUser, isReprocessCall) : await _paymentGateway.CreateCreditCardPayment(total, encryptedCreditCard, userId, isFreeUser, isReprocessCall);
                    return new CreditCardPayment { Status = PaymentStatusEnum.Approved, AuthorizationNumber = authorizationNumber };
                case PaymentMethodEnum.MP:
                    var paymentDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, total);
                    var mercadoPagoPayment = await _mercadoPagoService.CreatePayment(accountname, userId, paymentDetails.Total, encryptedCreditCard, isFreeUser, isReprocessCall);
                    return new CreditCardPayment { Status = mercadoPagoPayment.Status.MapToPaymentStatus(), AuthorizationNumber = mercadoPagoPayment.Id.ToString() };
                default:
                    return new CreditCardPayment { Status = PaymentStatusEnum.Approved };
            }
        }

        private IAccountingEntryMapper GetAccountingEntryMapper(PaymentMethodEnum paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                    return new AccountingEntryForCreditCardMapper();
                case PaymentMethodEnum.MP:
                    return new AccountingEntryForMercadopagoMapper(_paymentAmountService);
                default:
                    _logger.LogError($"The paymentMethod '{paymentMethod}' does not have a mapper.");
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private IBillingCreditMapper GetBillingCreditMapper(PaymentMethodEnum paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                    return new BillingCreditForCreditCardMapper(_billingRepository, _encryptionService);
                case PaymentMethodEnum.MP:
                    return new BillingCreditForMercadopagoMapper(_billingRepository, _encryptionService, _paymentAmountService);
                case PaymentMethodEnum.TRANSF:
                    return new BillingCreditForTransferMapper(_billingRepository);
                case PaymentMethodEnum.DA:
                    return new BillingCreditForAutomaticDebitMapper(_billingRepository);
                default:
                    _logger.LogError($"The paymentMethod '{paymentMethod}' does not have a mapper.");
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private Mappers.BillingCredit.AddOns.IBillingCreditMapper GetAddOnBillingCreditMapper(PaymentMethodEnum paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                    return new Mappers.BillingCredit.AddOns.BillingCreditForCreditCardMapper(_billingRepository, _encryptionService);
                case PaymentMethodEnum.MP:
                    return new Mappers.BillingCredit.AddOns.BillingCreditForMercadopagoMapper(_billingRepository, _encryptionService, _paymentAmountService);
                case PaymentMethodEnum.TRANSF:
                    return new Mappers.BillingCredit.AddOns.BillingCreditForTransferMapper(_billingRepository);
                case PaymentMethodEnum.DA:
                    return new Mappers.BillingCredit.AddOns.BillingCreditForAutomaticDebitMapper(_billingRepository);
                default:
                    _logger.LogError($"The paymentMethod '{paymentMethod}' does not have a mapper.");
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private async Task<int> ChangeBetweenMonthlyPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.EmailQty < newPlan.EmailQty)
            {
                PlanAmountDetails amountDetails = null;

                try
                {
                    amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error to get total amount for user {user.Email}.(Send Notifications)");
                }

                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
                if (currentBillingCredit != null)
                {
                    promotion = await _promotionRepository.GetById(currentBillingCredit.IdPromotion ?? 0);
                    if (promotion != null)
                    {
                        var timesAppliedPromocode = await _promotionRepository.GetHowManyTimesApplyedPromocode(promotion.Code, user.Email);
                        if (promotion.Duration == timesAppliedPromocode.CountApplied)
                        {
                            promotion = null;
                        }
                    }
                }

                var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, currentBillingCredit, BillingCreditTypeEnum.Upgrade_Between_Monthlies);
                billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                /* Update the user */
                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = false;

                await _userRepository.UpdateUserBillingCredit(user);

                var partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                await _billingRepository.CreateMovementBalanceAdjustmentAsync(user.IdUser, 0, UserTypeEnum.MONTHLY, UserTypeEnum.MONTHLY);
                int currentMonthlyAddedEmailsWithBilling = await _userRepository.GetCurrentMonthlyAddedEmailsWithBillingAsync(user.IdUser);

                await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan, currentMonthlyAddedEmailsWithBilling);

                if (promotion != null)
                    await _promotionRepository.IncrementUsedTimes(promotion);

                var status = PaymentStatusEnum.Approved.ToDescription();
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty, Source);

                //Send notifications
                SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.Upgrade_Between_Monthlies, currentPlan, amountDetails);

                return billingCreditId;
            }

            return 0;
        }

        private async Task<int> ChangeToMonthly(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            PlanAmountDetails amountDetails = null;

            try
            {
                amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error to get total amount for user {user.Email}.(Send Notifications)");
            }

            var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
            var creditsLeft = await _userRepository.GetAvailableCredit(user.IdUser);

            await _billingRepository.CreateMovementBalanceAdjustmentAsync(user.IdUser, creditsLeft, UserTypeEnum.INDIVIDUAL, UserTypeEnum.MONTHLY);

            var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
            var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, currentBillingCredit, BillingCreditTypeEnum.Individual_to_Monthly);
            billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

            var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

            await _billingRepository.CreateMovementCreditAsync(billingCreditId, 0, user, newPlan, null);

            if (creditsLeft != 0)
                await _billingRepository.CreateMovementCreditsLeftAsync(user.IdUser, creditsLeft, await _userRepository.GetAvailableCredit(user.IdUser));

            /* Update the user */
            user.IdCurrentBillingCredit = billingCreditId;
            user.OriginInbound = agreementInformation.OriginInbound;
            user.UpgradePending = false;

            await _userRepository.UpdateUserBillingCredit(user);

            var status = PaymentStatusEnum.Approved.ToDescription();
            await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty, Source);

            //Send notifications
            SendNotifications(user.Email, newPlan, user, creditsLeft, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.Individual_to_Monthly, currentPlan, amountDetails);

            return billingCreditId;
        }

        private async Task<int> ChangeBetweenSubscribersPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.SubscribersQty < newPlan.SubscribersQty)
            {
                PlanAmountDetails amountDetails = null;

                try
                {
                    amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error to get total amount for user {user.Email}.(Send Notifications)");
                }

                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
                if (currentBillingCredit != null)
                {
                    promotion = await _promotionRepository.GetById(currentBillingCredit.IdPromotion ?? 0);
                    if (promotion != null)
                    {
                        var timesAppliedPromocode = await _promotionRepository.GetHowManyTimesApplyedPromocode(promotion.Code, user.Email);
                        if (promotion.Duration == timesAppliedPromocode.CountApplied)
                        {
                            promotion = null;
                        }
                    }
                }

                var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, currentBillingCredit, BillingCreditTypeEnum.Upgrade_Between_Subscribers);
                billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;
                billingCreditAgreement.BillingCredit.ActivationDate = DateTime.UtcNow;

                var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);


                /* Update the user */
                user.IdCurrentBillingCredit = billingCreditId;
                user.OriginInbound = agreementInformation.OriginInbound;
                user.UpgradePending = false;
                user.MaxSubscribers = newPlan.SubscribersQty.Value;

                await _userRepository.UpdateUserBillingCredit(user);

                if (promotion != null)
                    await _promotionRepository.IncrementUsedTimes(promotion);

                var status = PaymentStatusEnum.Approved.ToDescription();
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty, Source);

                //Send notifications
                SendNotifications(user.Email, newPlan, user, 0, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.Upgrade_Between_Subscribers, currentPlan, amountDetails);

                //Activate StandBy Subscribers
                User userInformation = await _userRepository.GetUserInformation(user.Email);

                await _billingRepository.UpdateUserSubscriberLimitsAsync(user.IdUser);
                var activatedStandByAmount = await _billingRepository.ActivateStandBySubscribers(user.IdUser);
                if (activatedStandByAmount > 0)
                {
                    var lang = userInformation.Language ?? "en";
                    await _emailTemplatesService.SendActivatedStandByEmail(lang, userInformation.FirstName, activatedStandByAmount, user.Email);
                }

                return billingCreditId;
            }

            return 0;
        }

        private async Task<int> ChangeToSubscribers(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            PlanAmountDetails amountDetails = null;

            try
            {
                amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error to get total amount for user {user.Email}.(Send Notifications)");
            }

            var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
            var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
            var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, currentBillingCredit, BillingCreditTypeEnum.Individual_to_Subscribers);

            billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;
            billingCreditAgreement.BillingCredit.ActivationDate = DateTime.UtcNow;

            var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

            /* Update the user */
            user.IdCurrentBillingCredit = billingCreditId;
            user.OriginInbound = agreementInformation.OriginInbound;
            user.UpgradePending = false;
            user.MaxSubscribers = newPlan.SubscribersQty.Value;

            await _userRepository.UpdateUserBillingCredit(user);

            var status = PaymentStatusEnum.Approved.ToDescription();
            await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty, Source);

            //Send notifications
            SendNotifications(user.Email, newPlan, user, 0, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.Individual_to_Subscribers, currentPlan, amountDetails);

            return billingCreditId;
        }

        private async Task<int> BuyCredits(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            var isPaymentPending = BillingHelper.IsUpgradePending(user, promotion, payment);
            var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
            var billingCreditType = user.PaymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.Credit_Buyed_CC : BillingCreditTypeEnum.Credit_Request;
            var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
            var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, currentBillingCredit, billingCreditType);
            billingCreditAgreement.BillingCredit.DiscountPlanFeeAdmin = currentBillingCredit.DiscountPlanFeeAdmin;

            if (isPaymentPending)
            {
                billingCreditAgreement.BillingCredit.PaymentDate = null;
                billingCreditAgreement.BillingCredit.ActivationDate = null;
            }

            var billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

            if (!isPaymentPending)
            {
                user.IdCurrentBillingCredit = billingCreditId;
            }

            user.OriginInbound = agreementInformation.OriginInbound;
            user.UpgradePending = false;

            await _userRepository.UpdateUserBillingCredit(user);

            var partialBalance = 0;

            if (!isPaymentPending)
            {
                partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
            }

            if (promotion != null)
            {
                await _promotionRepository.IncrementUsedTimes(promotion);
            }

            var status = !isPaymentPending ? PaymentStatusEnum.Approved.ToDescription() : PaymentStatusEnum.Pending.ToDescription();
            await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty, Source);

            //Send notifications
            SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, billingCreditType, currentPlan, null);

            return billingCreditId;
        }

        private async Task CreateUserPaymentHistory(int idUser, int idPaymentMethod, int idPlan, string status, int idBillingCredit, string errorMessage, string source, string creditCardLastFourDigits = null)
        {
            var userPaymentHistory = new UserPaymentHistory
            {
                IdUser = idUser,
                Date = DateTime.Now,
                ErrorMessage = errorMessage,
                IdPaymentMethod = idPaymentMethod,
                IdPlan = idPlan,
                Source = source,
                Status = status,
                IdBillingCredit = idBillingCredit > 0 ? idBillingCredit : null,
                CreditCardLastFourDigits = creditCardLastFourDigits
            };

            await _userPaymentHistoryRepository.CreateUserPaymentHistoryAsync(userPaymentHistory);
        }

        private async Task CheckattemptsToCancelUser(int idUser, string accountname)
        {
            var attemptsToUpdate = await _userPaymentHistoryRepository.GetAttemptsToUpdateAsync(idUser, DateTime.UtcNow.AddMinutes(-_attemptsToUpdateSettings.Value.Minutes), DateTime.UtcNow, "PaymentMethod");
            if (attemptsToUpdate > _attemptsToUpdateSettings.Value.Attempts)
            {
                await _userRepository.CancelUser(idUser, _attemptsToUpdateSettings.Value.AccountCancellationReason, CancelatedObservation);

                var messageError = $"The user  {accountname} was canceled by exceed the attempts to update.";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
            }
        }

        private async Task<string> GetTaxRegimeDescription(int taxRegimeId)
        {
            if (taxRegimeId == 0)
            {
                return string.Empty;
            }

            var getTaxRegimesResult = await _staticDataClient.GetAllTaxRegimesAsync();

            if (getTaxRegimesResult.IsSuccessful)
            {
                var userTaxRegime = getTaxRegimesResult.TaxRegimes.Where(x => x.Id == taxRegimeId).FirstOrDefault();

                if (userTaxRegime != null)
                {
                    return string.Format("{0} - {1}", taxRegimeId, userTaxRegime.Description);
                }
            }

            //In case the static api client is down or something went wrong, in the email the tax regime will only show its Id
            return taxRegimeId.ToString();
        }

        private async Task<string> PutTaxCertificateUrl(PaymentMethod paymentMethod, string accountname)
        {
            using var _ = _timeCollector.StartScope();

            var currentPaymentMethod = await _billingRepository.GetCurrentPaymentMethod(accountname);

            if (paymentMethod == null || paymentMethod.TaxCertificate == null)
            {
                return currentPaymentMethod.TaxCertificateUrl;
            }

            var extension = Path.GetExtension(paymentMethod.TaxCertificate.FileName);
            string taxCertificateUrl;

            // User does not have any tax certificate uploaded
            if (currentPaymentMethod == null || currentPaymentMethod.TaxCertificateUrl == null)
            {
                taxCertificateUrl = await _fileStorage.SaveFile(paymentMethod.TaxCertificate.OpenReadStream(), extension, paymentMethod.TaxCertificate.ContentType);
            }
            else // User already has a tax certificate uploaded
            {
                var currentPaymentMethodTaxCerfiticateFileName = Path.GetFileName(currentPaymentMethod.TaxCertificateUrl);
                taxCertificateUrl = await _fileStorage.EditFile(paymentMethod.TaxCertificate.OpenReadStream(), extension, currentPaymentMethodTaxCerfiticateFileName, paymentMethod.TaxCertificate.ContentType);
            }

            return taxCertificateUrl;
        }

        private async Task GenerateWithoutRefundAsync(string accountName, AccountingEntry invoice)
        {
            string accountingEntryTypeDescriptionCnNoRefund = "Credit Note No Refund";
            string cancellationReasonSap = "credits purchase cancellation";

            try
            {
                /* Create the Credit Note */
                var creditNoteEntry = new AccountingEntry
                {
                    AccountingTypeDescription = accountingEntryTypeDescriptionCnNoRefund,
                    Amount = invoice.Amount,
                    Date = DateTime.Now,
                    IdInvoice = invoice.IdAccountingEntry,
                    IdClient = invoice.IdClient,
                    Source = invoice.Source,
                    IdAccountType = invoice.IdAccountType,
                    IdCurrencyType = invoice.IdCurrencyType,
                    Taxes = invoice.Taxes,
                    CurrencyRate = invoice.CurrencyRate,
                    IdInvoiceBillingType = invoice.IdInvoiceBillingType,
                    AccountEntryType = "P",
                    PaymentEntryType = "N"
                };

                /* Create the Credit Note */
                var creditNoteEntryId = await _billingRepository.CreateCreditNoteEntryAsync(creditNoteEntry);
                creditNoteEntry.IdAccountingEntry = creditNoteEntryId;


                /* Update status of the current invoice */
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.CreditNoteGenerated, string.Empty, string.Empty);

                /* Send the credit note to sap */
                await SendCreditNoteToSapAsync(accountName, creditNoteEntry, cancellationReasonSap, (int)ResponsabileBillingEnum.QBL);
            }
            catch (Exception e)
            {
                var errorMessage = string.Format("Error generating credit note for AccountingEntry {0} -- {1}", invoice.IdInvoice, e);
                _logger.LogError(errorMessage);
                await _slackService.SendNotification(errorMessage);
            }

        }

        private async Task<bool> SendCreditNoteToSapAsync(string accountName, AccountingEntry invoice, string reason, int billingSystemId)
        {
            var dtoSapCreditNote = new SapCreditNoteDto()
            {
                CreditNoteId = (int)invoice.IdAccountingEntry,
                InvoiceId = (int)invoice.IdInvoice,
                Amount = Decimal.ToDouble(invoice.Amount),
                ClientId = (int)invoice.IdClient,
                BillingSystemId = billingSystemId,
                Type = 1,
                Reason = reason
            };

            await _sapService.SendCreditNoteToSapAsync(accountName, dtoSapCreditNote);

            return true;
        }

        private IChatPlanUserMapper GetChatPlanUserMapper(PaymentMethodEnum paymentMethod)
        {
            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                case PaymentMethodEnum.MP:
                case PaymentMethodEnum.TRANSF:
                case PaymentMethodEnum.DA:
                    return new ChatPlanUserForCreditCardMapper();
                default:
                    _logger.LogError($"The paymentMethod '{paymentMethod}' does not have a mapper.");
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private async Task<int> BuyChatPlan(UserBillingInformation user, AgreementInformation agreementInformation, CreditCardPayment payment, CurrentPlan currentChatPlan, AccountTypeEnum accountType)
        {
            var billingCreditId = 0;
            User userFromCM = null;
            UserBillingInformation clientManager = null;

            PaymentMethodEnum paymentMethod = user.PaymentMethod;

            if (accountType == AccountTypeEnum.CM)
            {
                userFromCM = await _userRepository.GetUserInformation(user.Email);
                clientManager = await _clientManagerRepository.GetUserBillingInformation(userFromCM.IdClientManager.Value);
                paymentMethod = clientManager.PaymentMethod;
            }

            if (agreementInformation.AdditionalServices.Select(s => s.Type).Contains(AdditionalServiceTypeEnum.Chat))
            {
                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit.Value);
                var additionalServiceChatPlan = agreementInformation.AdditionalServices.FirstOrDefault(s => s.Type == AdditionalServiceTypeEnum.Chat);
                var chatPlan = await _chatPlanRepository.GetById(additionalServiceChatPlan.PlanId ?? 0);
                PlanAmountDetails amountDetails = await _accountPlansService.GetCalculateAmountToUpgrade(user.Email, (int)PlanTypeEnum.Chat, additionalServiceChatPlan.PlanId ?? 0, currentBillingCredit.IdDiscountPlan ?? 0, string.Empty);

                if (currentChatPlan == null || (currentChatPlan != null && currentChatPlan.IdPlan != chatPlan.IdChatPlan))
                {
                    var billingCreditType = paymentMethod == PaymentMethodEnum.CC ? BillingCreditTypeEnum.Conversation_Buyed_CC : BillingCreditTypeEnum.Conversation_Request;
                    if (currentChatPlan != null && currentChatPlan.ConversationQty > chatPlan.ConversationQty)
                    {
                        billingCreditType = BillingCreditTypeEnum.Downgrade_Between_Conversation;
                    }

                    var billingCreditMapper = GetAddOnBillingCreditMapper(paymentMethod);

                    var total = chatPlan.Fee;
                    var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(total, user, currentBillingCredit, payment, billingCreditType);

                    billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                    var chatPlanUserMapper = GetChatPlanUserMapper(paymentMethod);
                    var chatPlanUser = chatPlanUserMapper.MapToChatPlanUser(user.IdUser, chatPlan.IdChatPlan, billingCreditId);
                    await _billingRepository.CreateChatPlanUserAsync(chatPlanUser);

                    /* Save current billing credit in the UserAddOn table */
                    await _userAddOnRepository.SaveCurrentBillingCreditByUserIdAndAddOnTypeAsync(user.IdUser, (int)AddOnType.Chat, billingCreditId);

                    try
                    {
                        var isUpgradeApproved = (paymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, null, payment));
                        if (isUpgradeApproved)
                        {
                            if (currentChatPlan != null)
                            {
                                await _beplicService.UnassignPlanToUser(user.IdUser);
                            }

                            await _beplicService.AssignPlanToUser(user.IdUser, chatPlan.Description);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, message: ex.Message);
                        await _slackService.SendNotification(ex.Message);
                    }

                    //Send notifications
                    UserBillingInformation userToSendNotification = null;
                    if (accountType == AccountTypeEnum.User)
                    {
                        userToSendNotification = user;
                    }
                    else
                    {
                        userToSendNotification = clientManager;
                    }

                    var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(currentBillingCredit.IdDiscountPlan ?? 0);
                    SendConversationNotifications(userToSendNotification.Email, userToSendNotification, chatPlan, currentChatPlan, payment, planDiscountInformation, amountDetails, accountType);
                }
            }

            return billingCreditId;
        }

        private async Task CancelChatPlan(UserBillingInformation user)
        {
            UserAddOn userAddOn = await _userAddOnRepository.GetByUserIdAndAddOnType(user.IdUser, (int)AddOnType.Chat);

            if (userAddOn != null)
            {
                var currentChatPlanBillingCredit = await _billingRepository.GetBillingCredit(userAddOn.IdCurrentBillingCredit);

                if (currentChatPlanBillingCredit != null && currentChatPlanBillingCredit.IdBillingCreditType != (int)BillingCreditTypeEnum.Conversation_Canceled)
                {
                    await _billingRepository.UpdateBillingCreditType(userAddOn.IdCurrentBillingCredit, (int)BillingCreditTypeEnum.Conversation_Canceled);
                    await _beplicService.UnassignPlanToUser(user.IdUser);
                }
            }
        }
    }
}
