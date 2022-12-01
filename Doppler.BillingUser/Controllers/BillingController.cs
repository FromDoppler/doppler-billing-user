using System;
using Doppler.BillingUser.DopplerSecurity;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Doppler.BillingUser.ExternalServices.AccountPlansApi;
using FluentValidation;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.FirstData;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.Encryption;
using System.Linq;
using Doppler.BillingUser.ExternalServices.Slack;
using Microsoft.Extensions.Options;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Utils;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Newtonsoft.Json;
using System.Collections.Generic;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.BillingCredit;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.Mappers.PaymentStatus;
using FluentValidator;
using System.Drawing;
using System.Text;
using Tavis.UriTemplates;
using Doppler.BillingUser.ApiModels;

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
            PaymentMethodEnum.MP
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
            IUserPaymentHistoryRepository userPaymentHistoryRepository)
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
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER)]
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

                if (currentPaymentMethod != null & currentPaymentMethod.PaymentMethodName == PaymentMethodEnum.TRANSF.ToString())
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

            return new OkObjectResult(currentPaymentMethod);
        }

        [Authorize(Policies.OWN_RESOURCE_OR_SUPERUSER_OR_PROVISORY_USER)]
        [HttpPut("/accounts/{accountname}/payment-methods/current")]
        public async Task<IActionResult> UpdateCurrentPaymentMethod(string accountname, [FromBody] PaymentMethod paymentMethod)
        {
            try
            {
                _logger.LogDebug("Update current payment method.");

                User userInformation = await _userRepository.GetUserInformation(accountname);
                var isSuccess = await _billingRepository.UpdateCurrentPaymentMethod(userInformation, paymentMethod);

                if (!isSuccess)
                {
                    var messageError = $"Failed at updating payment method for user {accountname}";
                    _logger.LogError(messageError);
                    await _slackService.SendNotification(messageError);
                    return new BadRequestObjectResult("Failed at updating payment");
                }

                return new OkObjectResult("Successfully");
            }
            catch (DopplerApplicationException e)
            {
                var cardNumberDetails = paymentMethod.PaymentMethodName == PaymentMethodEnum.CC.ToString() ? "with credit card's last 4 digits: " + paymentMethod.CCNumber[^4..] : "";
                var messageError = $"Failed at updating payment method for user {accountname} {cardNumberDetails}. Exception {e.Message}.";
                _logger.LogError(e, messageError);
                await _slackService.SendNotification(messageError);
                return new BadRequestObjectResult(e.Message);
            }
        }

        [Authorize(Policies.PROVISORY_USER_OR_SUPER_USER)]
        [HttpGet("/accounts/{accountname}/invoices")]
        public async Task<IActionResult> GetInvoices(string accountname, [FromQuery] PaymentStatusApiEnum[] withStatus)
        {
            var user = await _userRepository.GetUserInformation(accountname);

            if (user == null)
            {
                return new BadRequestObjectResult("The user does not exist");
            }
            var mappedStatus = withStatus.Select(x => x.MapToPaymentStatusEnum()).ToArray();

            var invoices = await _billingRepository.GetInvoices(user.IdUser, mappedStatus);

            var invoicesData = invoices.Select(invoice => new DeclinedInvoiceData()
            {
                Date = invoice.Date,
                InvoiceNumber = invoice.InvoiceNumber,
                Amount = invoice.Amount,
                Error = invoice.ErrorMessage,
                Status = invoice.Status.MapToPaymentStatusApiEnum(),
            })
            .ToList();
            var totalPending = invoices.Where(x => x.Status != PaymentStatusEnum.Approved).Sum(x => x.Amount);
            return new OkObjectResult(new GetDeclinedInvoicesResult()
            {
                Invoices = invoicesData
            });
        }

        [Authorize(Policies.PROVISORY_USER_OR_SUPER_USER)]
        [HttpPut("/accounts/{accountname}/payments/reprocess")]
        public async Task<IActionResult> Reprocess(string accountname)
        {
            var userBillingInfo = await _userRepository.GetUserBillingInformation(accountname);

            if (userBillingInfo.PaymentMethod != PaymentMethodEnum.CC && userBillingInfo.PaymentMethod != PaymentMethodEnum.MP)
            {
                return new BadRequestObjectResult("Payment method not supported")
                {
                    StatusCode = 500
                };
            }

            var user = await _userRepository.GetUserInformation(accountname);

            var invoices = await _billingRepository.GetInvoices(user.IdUser, PaymentStatusEnum.DeclinedPaymentTransaction);

            if (invoices.Count == 0)
            {
                _logger.LogError("Invoices with accountname: {accountname} were not found.", accountname);
                return new BadRequestObjectResult("No invoice found with status declined");
            }

            var invoicesPaymentResults = new List<ReprocessInvoicePaymentResult>();

            foreach (var invoice in invoices)
            {
                var reprocessResult = await ReprocessInvoicePayment(invoice, accountname, user, userBillingInfo);
                invoicesPaymentResults.Add(reprocessResult);
            }
            var invoicesResults = invoicesPaymentResults.Select(x => x.Result);

            if (invoicesResults.All(x => x.Equals(PaymentStatusEnum.DeclinedPaymentTransaction)))
            {
                await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Fallido", invoices.Sum(x => x.Amount));
                return new BadRequestObjectResult("No invoice was processed succesfully");
            }
            _userRepository.UnblockAccountNotPayed(accountname);

            if (invoicesResults.Any(x => x == PaymentStatusEnum.Pending))
            {
                var failedAndPendingInvoicesAmount = invoicesPaymentResults.Where(x => x.Result != PaymentStatusEnum.Approved).Sum(x => x.Amount);

                await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Pendiente", failedAndPendingInvoicesAmount);
                return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = false, anyPendingInvoices = true });
            }

            if (invoicesResults.All(x => x.Equals(PaymentStatusEnum.Approved)))
            {
                await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Exitoso", 0.0M);
                return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = true, anyPendingInvoices = false });
            }
            else
            {
                var failedInvoicesAmount = invoicesPaymentResults.Where(x => x.Result == PaymentStatusEnum.DeclinedPaymentTransaction).Sum(x => x.Amount);

                await _emailTemplatesService.SendReprocessStatusNotification(accountname, user.IdUser, invoices.Sum(x => x.Amount), "Parcialmente exitoso", failedInvoicesAmount);
                return new OkObjectResult(new ReprocessInvoiceResult { allInvoicesProcessed = false, anyPendingInvoices = false });
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

            var payment = await CreateCreditCardPayment(invoice.Amount, user.IdUser, accountname, userBillingInfo.PaymentMethod, currentPlan == null);

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
        [HttpPost("/accounts/{accountname}/agreements")]
        public async Task<IActionResult> CreateAgreement([FromRoute] string accountname, [FromBody] AgreementInformation agreementInformation)
        {
            var user = await _userRepository.GetUserBillingInformation(accountname);

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
                    if (currentPlan.SubscribersQty >= newPlan.SubscribersQty)
                    {
                        var messageError = $"Failed at creating new agreement for user {accountname}, Invalid selected plan {agreementInformation.PlanId}. Only supports upselling.";
                        _logger.LogError(messageError);
                        await _slackService.SendNotification(messageError);
                        return new BadRequestObjectResult("Invalid selected plan. Only supports upselling.");
                    }
                }

                if (currentPlan != null && currentPlan.IdUserType == UserTypeEnum.MONTHLY)
                {
                    if (currentPlan.EmailQty >= newPlan.EmailQty)
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
                    promotion = await _accountPlansService.GetValidPromotionByCode(agreementInformation.Promocode, agreementInformation.PlanId);
                }

                int invoiceId = 0;
                string authorizationNumber = string.Empty;
                CreditCard encryptedCreditCard = null;
                CreditCardPayment payment = null;

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

                    payment = await CreateCreditCardPayment(agreementInformation.Total.Value, user.IdUser, accountname, user.PaymentMethod, currentPlan == null);

                    var accountEntyMapper = GetAccountingEntryMapper(user.PaymentMethod);
                    AccountingEntry invoiceEntry = await accountEntyMapper.MapToInvoiceAccountingEntry(agreementInformation.Total.Value, user, newPlan, payment);
                    AccountingEntry paymentEntry = null;
                    authorizationNumber = payment.AuthorizationNumber;

                    if (payment.Status == PaymentStatusEnum.Approved)
                    {
                        paymentEntry = await accountEntyMapper.MapToPaymentAccountingEntry(invoiceEntry, encryptedCreditCard);
                    }

                    invoiceId = await _billingRepository.CreateAccountingEntriesAsync(invoiceEntry, paymentEntry);
                }

                var billingCreditId = 0;
                var partialBalance = 0;

                if (currentPlan == null)
                {
                    var billingCreditMapper = GetBillingCreditMapper(user.PaymentMethod);
                    var billingCreditAgreement = await billingCreditMapper.MapToBillingCreditAgreement(agreementInformation, user, newPlan, promotion, payment, null, BillingCreditTypeEnum.UpgradeRequest);
                    billingCreditId = await _billingRepository.CreateBillingCreditAsync(billingCreditAgreement);

                    user.IdCurrentBillingCredit = billingCreditId;
                    user.OriginInbound = agreementInformation.OriginInbound;
                    user.UpgradePending = BillingHelper.IsUpgradePending(user, promotion, payment);
                    user.UTCFirstPayment = !user.UpgradePending ? DateTime.UtcNow : null;
                    user.UTCUpgrade = user.UTCFirstPayment;

                    if (newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS && newPlan.SubscribersQty.HasValue && !user.UpgradePending)
                    {
                        user.MaxSubscribers = newPlan.SubscribersQty.Value;
                    }

                    await _userRepository.UpdateUserBillingCredit(user);

                    partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);

                    if (!user.UpgradePending)
                    {
                        if (newPlan.IdUserType != UserTypeEnum.SUBSCRIBERS)
                        {
                            await _billingRepository.CreateMovementCreditAsync(billingCreditId, partialBalance, user, newPlan);
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

                    var status = !user.UpgradePending ? PaymentStatusEnum.Approved.ToDescription() : PaymentStatusEnum.Pending.ToDescription();
                    await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty);

                    //Send notifications
                    SendNotifications(accountname, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.UpgradeRequest, currentPlan, null);
                }
                else
                {
                    if (currentPlan.IdUserType == UserTypeEnum.MONTHLY && newPlan.IdUserType == UserTypeEnum.MONTHLY)
                    {
                        if (currentPlan.IdUserTypePlan != newPlan.IdUserTypePlan)
                        {
                            billingCreditId = await ChangeBetweenMonthlyPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                    }
                    else
                    {
                        if (currentPlan.IdUserType == UserTypeEnum.SUBSCRIBERS && newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                        {
                            billingCreditId = await ChangeBetweenSubscribersPlans(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                        else if (currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL && newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                        {
                            billingCreditId = await BuyCredits(currentPlan, newPlan, user, agreementInformation, promotion, payment);
                        }
                    }
                }

                if (agreementInformation.Total.GetValueOrDefault() > 0 &&
                    ((user.PaymentMethod == PaymentMethodEnum.CC) ||
                    (user.PaymentMethod == PaymentMethodEnum.MP) ||
                    (user.PaymentMethod == PaymentMethodEnum.TRANSF && user.IdBillingCountry == (int)CountryEnum.Argentina)))
                {
                    var billingCredit = await _billingRepository.GetBillingCredit(billingCreditId);
                    var cardNumber = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.Number) : "";
                    var holderName = user.PaymentMethod == PaymentMethodEnum.CC ? _encryptionService.DecryptAES256(encryptedCreditCard.HolderName) : "";

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
                                agreementInformation.Total),
                            accountname);
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

                        if (!user.UpgradePending)
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
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, PaymentStatusEnum.DeclinedPaymentTransaction.ToDescription(), 0, e.Message);

                var cardNumber = string.Empty;
                if (user.PaymentMethod == PaymentMethodEnum.CC)
                {
                    var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
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
            bool isUpgradeApproved;

            switch (billingCreditType)
            {
                case BillingCreditTypeEnum.UpgradeRequest:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));

                    if (newPlan.IdUserType == UserTypeEnum.INDIVIDUAL)
                    {
                        await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved);
                    }
                    else
                    {
                        if (isUpgradeApproved && newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS)
                        {
                            await _emailTemplatesService.SendNotificationForSuscribersPlan(accountname, userInformation, newPlan);
                        }

                        await _emailTemplatesService.SendNotificationForUpgradePlan(accountname, userInformation, newPlan, user, promotion, promocode, discountId, planDiscountInformation, !isUpgradeApproved);
                    }

                    return;
                case BillingCreditTypeEnum.Upgrade_Between_Monthlies:
                case BillingCreditTypeEnum.Upgrade_Between_Subscribers:
                    await _emailTemplatesService.SendNotificationForUpdatePlan(accountname, userInformation, currentPlan, newPlan, user, promotion, promocode, discountId, planDiscountInformation, amountDetails);
                    return;
                case BillingCreditTypeEnum.Credit_Buyed_CC:
                case BillingCreditTypeEnum.Credit_Request:
                    isUpgradeApproved = (user.PaymentMethod == PaymentMethodEnum.CC || !BillingHelper.IsUpgradePending(user, promotion, payment));
                    await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, newPlan, user, partialBalance, promotion, promocode, !isUpgradeApproved);
                    return;
                default:
                    return;
            }
        }

        private async Task<CreditCardPayment> CreateCreditCardPayment(decimal total, int userId, string accountname, PaymentMethodEnum paymentMethod, bool isFreeUser)
        {
            var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);

            switch (paymentMethod)
            {
                case PaymentMethodEnum.CC:
                    var authorizationNumber = await _paymentGateway.CreateCreditCardPayment(total, encryptedCreditCard, userId, isFreeUser);
                    return new CreditCardPayment { Status = PaymentStatusEnum.Approved, AuthorizationNumber = authorizationNumber };
                case PaymentMethodEnum.MP:
                    var paymentDetails = await _paymentAmountService.ConvertCurrencyAmount(CurrencyTypeEnum.UsS, CurrencyTypeEnum.sARG, total);
                    var mercadoPagoPayment = await _mercadoPagoService.CreatePayment(accountname, userId, paymentDetails.Total, encryptedCreditCard, isFreeUser);
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
                default:
                    _logger.LogError($"The paymentMethod '{paymentMethod}' does not have a mapper.");
                    throw new ArgumentException($"The paymentMethod '{paymentMethod}' does not have a mapper.");
            }
        }

        private async Task<int> ChangeBetweenMonthlyPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.EmailQty < newPlan.EmailQty)
            {
                var amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);

                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
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
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty);

                //Send notifications
                SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, BillingCreditTypeEnum.Upgrade_Between_Monthlies, currentPlan, amountDetails);

                return billingCreditId;
            }

            return 0;
        }

        private async Task<int> ChangeBetweenSubscribersPlans(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            if (currentPlan.SubscribersQty < newPlan.SubscribersQty)
            {
                var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
                var amountDetails = await _accountPlansService.GetCalculateUpgrade(user.Email, agreementInformation);

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
                await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty);

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

        private async Task<int> BuyCredits(UserTypePlanInformation currentPlan, UserTypePlanInformation newPlan, UserBillingInformation user, AgreementInformation agreementInformation, Promotion promotion, CreditCardPayment payment)
        {
            var isPaymentPending = BillingHelper.IsUpgradePending(user, promotion, payment);
            var currentBillingCredit = await _billingRepository.GetBillingCredit(user.IdCurrentBillingCredit);
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
            await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, agreementInformation.PlanId, status, billingCreditId, string.Empty);

            //Send notifications
            SendNotifications(user.Email, newPlan, user, partialBalance, promotion, agreementInformation.Promocode, agreementInformation.DiscountId, payment, billingCreditType, currentPlan, null);

            return billingCreditId;
        }

        private async Task CreateUserPaymentHistory(int idUser, int idPaymentMethod, int idPlan, string status, int idBillingCredit, string errorMessage)
        {
            var userPaymentHistory = new UserPaymentHistory
            {
                IdUser = idUser,
                Date = DateTime.Now,
                ErrorMessage = errorMessage,
                IdPaymentMethod = idPaymentMethod,
                IdPlan = idPlan,
                Source = Source,
                Status = status,
                IdBillingCredit = idBillingCredit > 0 ? idBillingCredit : null
            };

            await _userPaymentHistoryRepository.CreateUserPaymentHistoryAsync(userPaymentHistory);
        }
    }
}
