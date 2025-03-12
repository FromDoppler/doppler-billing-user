using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.ExternalServices.MercadoPagoApi;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Slack;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Mappers;
using Doppler.BillingUser.Mappers.PaymentStatus;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Services;
using Doppler.BillingUser.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Controllers
{
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IPaymentAmountHelper _paymentAmountService;
        private readonly ILogger<WebhooksController> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly IEmailTemplatesService _emailTemplatesService;
        private readonly ISlackService _slackService;
        private readonly IOptions<ZohoSettings> _zohoSettings;
        private readonly IZohoService _zohoService;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUserPaymentHistoryRepository _userPaymentHistoryRepository;
        private readonly ISapService _sapService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOptions<SapSettings> _sapSettings;

        private const string CreditNote = "Credit Note";
        private const string CancellationReasonSap = "credits purchase cancellation";
        private const string Source = "Webhooks";
        private readonly string PAYMENT_UPDATED = "payment.updated";
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore
        };

        public WebhooksController(
            IPaymentAmountHelper paymentAmountService,
            ILogger<WebhooksController> logger,
            IBillingRepository billingRepository,
            IUserRepository userRepository,
            IMercadoPagoService mercadoPagoService,
            IEmailTemplatesService emailTemplatesService,
            ISlackService slackService,
            IOptions<ZohoSettings> zohoSettings,
            IZohoService zohoService,
            IPromotionRepository promotionRepository,
            IUserPaymentHistoryRepository userPaymentHistoryRepository,
            ISapService sapService,
            IEncryptionService encryptionService,
            IOptions<SapSettings> sapSettings)
        {
            _billingRepository = billingRepository;
            _paymentAmountService = paymentAmountService;
            _logger = logger;
            _userRepository = userRepository;
            _mercadoPagoService = mercadoPagoService;
            _emailTemplatesService = emailTemplatesService;
            _slackService = slackService;
            _zohoSettings = zohoSettings;
            _zohoService = zohoService;
            _promotionRepository = promotionRepository;
            _userPaymentHistoryRepository = userPaymentHistoryRepository;
            _sapService = sapService;
            _sapSettings = sapSettings;
            _encryptionService = encryptionService;
        }

        [HttpPost("/accounts/{accountname}/integration/mercadopagonotification")]
        public async Task<IActionResult> UpdateMercadoPagoPaymentStatusAsync([FromRoute] string accountname, [FromBody] MercadoPagoNotification notification)
        {
            if (notification.Action != PAYMENT_UPDATED)
            {
                return new BadRequestResult();
            }

            var user = await _userRepository.GetUserBillingInformation(accountname);
            if (user is null)
            {
                return new NotFoundObjectResult("Account not found");
            }

            var invoice = await _billingRepository.GetInvoice(user.IdUser, notification.Data.Id.ToString());
            if (invoice is null)
            {
                _logger.LogError("Invoice with authorization number: {authorizationNumber} was not found.", notification.Data.Id);
                return new NotFoundObjectResult("Invoice not found");
            }

            var payment = await _mercadoPagoService.GetPaymentById(notification.Data.Id, accountname);

            var status = payment.Status.MapToPaymentStatus();
            if (status == PaymentStatusEnum.Pending && invoice.Status == PaymentStatusEnum.Approved)
            {
                return new OkObjectResult("Successful");
            }

            if (status == PaymentStatusEnum.DeclinedPaymentTransaction && invoice.Status != PaymentStatusEnum.DeclinedPaymentTransaction)
            {
                await CancelPaymentAsync(accountname, user, invoice, payment, status);
            }

            if (status == PaymentStatusEnum.Approved && invoice.Status != PaymentStatusEnum.Approved)
            {
                await ApprovePaymentAsync(accountname, user, invoice, payment, status);
            }

            return new OkObjectResult("Successful");
        }

        [HttpPost("/accounts/{accountname}/integration/monthly/mercadopagonotification")]
        public async Task<IActionResult> UpdateMercadoPagoMonthlyPaymentStatusAsync([FromRoute] string accountname, [FromBody] MercadoPagoNotification notification)
        {
            _logger.LogInformation($"MercadopagoNotification - Monthly: id_user:{notification.UserId},payment_id: {(notification.Data == null ? 0 : notification.Data.Id)}, action: {notification.Action}");

            if (notification.Action != PAYMENT_UPDATED)
            {
                return new BadRequestResult();
            }

            var user = await _userRepository.GetUserBillingInformation(accountname);
            if (user is null)
            {
                return new NotFoundObjectResult("Account not found");
            }

            var invoice = await _billingRepository.GetInvoice(user.IdUser, notification.Data.Id.ToString());
            if (invoice is null)
            {
                _logger.LogError("Invoice with authorization number: {authorizationNumber} was not found.", notification.Data.Id);
                return new NotFoundObjectResult("Invoice not found");
            }

            if (invoice.Status == PaymentStatusEnum.Pending)
            {
                var payment = await _mercadoPagoService.GetPaymentById(notification.Data.Id, accountname);
                var status = payment.Status.MapToPaymentStatus();

                await UpdateInvoiceStatus(accountname, invoice, status, payment);
            }

            return new OkObjectResult("Successful");
        }

        private async Task ApprovePaymentAsync(string accountname, UserBillingInformation user, AccountingEntry invoice, MercadoPagoPayment payment, PaymentStatusEnum status)
        {
            var accountingEntryMapper = new AccountingEntryForMercadopagoMapper(_paymentAmountService);
            var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountname);
            var paymentEntry = await accountingEntryMapper.MapToPaymentAccountingEntry(invoice, encryptedCreditCard);
            await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.Approved, payment.StatusDetail, invoice.AuthorizationNumber);
            await _billingRepository.CreatePaymentEntryAsync(invoice.IdAccountingEntry, paymentEntry);

            await _emailTemplatesService.SendNotificationForMercadoPagoPaymentApproved(user.IdUser, user.Email);

            //Get Pending billing request and approve this.
            var billingCredits = await _billingRepository.GetPendingBillingCreditsAsync(user.IdUser, PaymentMethodEnum.MP);

            if (billingCredits.Count == 0)
            {
                var messageError = $"The billing credits does not exist for the user {accountname}. You must be manually update.";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
            }

            if (billingCredits.Count > 1)
            {
                var messageError = $"Can not update the billing credit because the user {accountname} has more one pending billing credits. You must be manually update.";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
            }

            if (billingCredits.Count == 1)
            {
                var pendingBillingCredit = billingCredits.FirstOrDefault();
                if (pendingBillingCredit != null)
                {
                    Promotion promotion = null;
                    if (pendingBillingCredit.IdPromotion != null)
                    {
                        promotion = await _promotionRepository.GetById(pendingBillingCredit.IdPromotion.Value);
                    }

                    if (pendingBillingCredit.PaymentDate == null)
                    {
                        var partialBalance = 0;

                        //This is exec only once
                        if (pendingBillingCredit.ActivationDate == null)
                        {
                            user.UpgradePending = false;

                            if (pendingBillingCredit.IdUserType == (int)UserTypeEnum.INDIVIDUAL)
                            {
                                user.IdCurrentBillingCredit = pendingBillingCredit.IdBillingCredit;
                            }

                            pendingBillingCredit.ActivationDate = DateTime.UtcNow;

                            if (pendingBillingCredit.IdUserType == (int)UserTypeEnum.SUBSCRIBERS && pendingBillingCredit.SubscribersQty.HasValue)
                                user.MaxSubscribers = pendingBillingCredit.SubscribersQty.Value;

                            pendingBillingCredit.ActivationDate = DateTime.Now;
                        }

                        if (pendingBillingCredit.IdBillingCreditType == (int)BillingCreditTypeEnum.UpgradeRequest)
                        {
                            if (user.UTCUpgrade == null)
                                user.UTCUpgrade = pendingBillingCredit.ActivationDate;

                            if (user.UTCFirstPayment == null)
                                user.UTCFirstPayment = pendingBillingCredit.PaymentDate;

                            if (pendingBillingCredit.IdUserType != (int)UserTypeEnum.SUBSCRIBERS)
                            {
                                partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                                await _billingRepository.CreateMovementCreditAsync(pendingBillingCredit.IdBillingCredit, partialBalance, user, new UserTypePlanInformation { IdUserType = (UserTypeEnum)pendingBillingCredit.IdUserType });

                                _logger.LogInformation($"Successful at creating movement credits for the User: {accountname}");
                            }

                            /* Send zoho */
                            if (_zohoSettings.Value.UseZoho)
                            {
                                ZohoDTO zohoDto = new ZohoDTO()
                                {
                                    Email = user.Email,
                                    Doppler = ((UserTypeEnum)pendingBillingCredit.IdUserType).ToDescription(),
                                    BillingSystem = user.PaymentMethod.ToString(),
                                    OriginInbound = user.OriginInbound,
                                    UpgradeDate = DateTime.UtcNow,
                                    FirstPaymentDate = DateTime.UtcNow
                                };

                                if (pendingBillingCredit.IdPromotion != null)
                                {
                                    promotion = await _promotionRepository.GetById(pendingBillingCredit.IdPromotion.Value);
                                    zohoDto.PromoCodo = promotion.Code;
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
                        else
                        {
                            if (pendingBillingCredit.IdUserType == (int)UserTypeEnum.INDIVIDUAL)
                            {
                                partialBalance = await _userRepository.GetAvailableCredit(user.IdUser);
                                await _billingRepository.CreateMovementCreditAsync(pendingBillingCredit.IdBillingCredit, partialBalance, user, new UserTypePlanInformation { IdUserType = (UserTypeEnum)pendingBillingCredit.IdUserType });

                                _logger.LogInformation($"Successful at creating movement credits for the User: {accountname}");
                            }
                        }

                        await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, pendingBillingCredit.IdUserTypePlan, PaymentStatusEnum.Approved.ToDescription(), pendingBillingCredit.IdBillingCredit, string.Empty, Source);

                        _logger.LogInformation($"Successful at creating payment history for the User: {accountname}");


                        pendingBillingCredit.PaymentDate = DateTime.Now;

                        await _userRepository.UpdateUserBillingCredit(user);

                        _logger.LogInformation($"Successful at updating user billing credit for the User: {accountname}");

                        await _billingRepository.ApproveBillingCreditAsync(pendingBillingCredit);

                        _logger.LogInformation($"Successful at approving the billing credit for: User: {accountname} and BillingCredit: {pendingBillingCredit.IdBillingCredit}");

                        SendNotifications(accountname, pendingBillingCredit, user, partialBalance, promotion);
                    }
                }

                var message = $"Successful at updating the payment for: User: {accountname} - Billing Credit: {pendingBillingCredit.IdBillingCredit}";
                _logger.LogInformation(message);
                await _slackService.SendNotification(message);
            }
        }

        private async Task CancelPaymentAsync(string accountname, UserBillingInformation user, AccountingEntry invoice, MercadoPagoPayment payment, PaymentStatusEnum status)
        {
            if (invoice.Status == PaymentStatusEnum.Approved)
            {
                _logger.LogError("The payment associated to the invoiceId {invoiceId} was rejected. Reason: {reason}", invoice.IdAccountingEntry, payment.StatusDetail);
                return;
            }

            var billingCredits = await _billingRepository.GetPendingBillingCreditsAsync(user.IdUser, PaymentMethodEnum.MP);

            if (billingCredits.Count == 0)
            {
                var messageError = $"The billing credits does not exist for the user {accountname}. You must be manually update.";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
                return;
            }

            if (billingCredits.Count > 1)
            {
                var messageError = $"Can not update the billing credit because the user {accountname} has more one pending billing credits. You must be manually update.";
                _logger.LogError(messageError);
                await _slackService.SendNotification(messageError);
                return;
            }

            if (billingCredits.Count == 1)
            {
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, status, payment.StatusDetail, invoice.AuthorizationNumber);

                var pendingBillingCredit = billingCredits.FirstOrDefault();

                var upgradePending = !user.UpgradePending.HasValue || (user.UpgradePending.HasValue && user.UpgradePending.Value);
                var creditRequest = (pendingBillingCredit.IdBillingCreditType == (int)BillingCreditTypeEnum.Credit_Request);

                if (payment.StatusDetail == "cc_rejected_high_risk" && (upgradePending || creditRequest))
                {
                    if (pendingBillingCredit != null)
                    {
                        var previousBillingCredit = await _billingRepository.GetPreviousBillingCreditNotCancelledByIdUserAsync(user.IdUser, pendingBillingCredit.IdBillingCredit);
                        if (previousBillingCredit != null)
                        {
                            user.IdCurrentBillingCredit = previousBillingCredit.IdBillingCredit;
                        }
                        else
                        {
                            user.IdCurrentBillingCredit = null;
                        }

                        if (user.UpgradePending == true)
                        {
                            user.UpgradePending = null;

                            Promotion promotion = null;
                            if (pendingBillingCredit.IdPromotion != null)
                            {
                                promotion = await _promotionRepository.GetById(pendingBillingCredit.IdPromotion.Value);
                                await _promotionRepository.DecrementUsedTimesAsync(promotion);
                            }
                        }

                        await CreateUserPaymentHistory(user.IdUser, (int)user.PaymentMethod, pendingBillingCredit.IdUserTypePlan, PaymentStatusEnum.DeclinedPaymentTransaction.ToDescription(), pendingBillingCredit.IdBillingCredit, string.Empty, Source);

                        user.PaymentMethod = PaymentMethodEnum.NONE;
                        await _userRepository.UpdateUserBillingCredit(user);
                        await _billingRepository.CancelBillingCreditAsync(pendingBillingCredit);
                        await GenerateMercadoPagoCreditNoteAsync(accountname, invoice);
                    }

                    var message = $"Successful at canceling the payment for: User: {accountname} - Billing Credit: {pendingBillingCredit.IdBillingCredit}";
                    _logger.LogInformation(message);
                    await _slackService.SendNotification(message);
                }

                /* Send notification of rejected payment */
                await SendNotificationForRejectedMercadoPagoPayment(accountname, (upgradePending || creditRequest), payment.StatusDetail);
            }
        }

        private async Task GenerateMercadoPagoCreditNoteAsync(string accountName, AccountingEntry invoice)
        {
            try
            {
                var creditNoteEntry = new AccountingEntry
                {
                    AccountingTypeDescription = CreditNote,
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
                await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, PaymentStatusEnum.CreditNoteGenerated, string.Empty, invoice.AuthorizationNumber);

                /* Send the credit note to sap */
                await SendCreditNoteToSapAsync(accountName, creditNoteEntry, CancellationReasonSap, (int)ResponsabileBillingEnum.Mercadopago);
            }
            catch (Exception e)
            {
                var errorMessage = string.Format("Error generating credit note for AccountingEntry {0} -- {1}", invoice.IdInvoice, e);
                _logger.LogError(errorMessage);
                await _slackService.SendNotification(errorMessage);
            }
        }

        private async void SendNotifications(
            string accountname,
            BillingCredit billingCredit,
            UserBillingInformation user,
            int partialBalance,
            Promotion promotion)
        {
            User userInformation = await _userRepository.GetUserInformation(accountname);
            var discountId = billingCredit.IdDiscountPlan.HasValue ? billingCredit.IdDiscountPlan.Value : 0;
            var planDiscountInformation = await _billingRepository.GetPlanDiscountInformation(discountId);

            var userTypePlanInformation = new UserTypePlanInformation
            {
                IdUserType = (UserTypeEnum)billingCredit.IdUserType,
                EmailQty = billingCredit.CreditsQty,
                SubscribersQty = billingCredit.SubscribersQty,
                Fee = (double)billingCredit.PlanFee
            };

            var promotionCode = promotion != null ? promotion.Code : string.Empty;

            switch ((BillingCreditTypeEnum)billingCredit.IdBillingCreditType)
            {
                case BillingCreditTypeEnum.UpgradeRequest:
                    if (billingCredit.IdUserType == (int)UserTypeEnum.INDIVIDUAL)
                    {
                        await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, userTypePlanInformation, user, partialBalance, promotion, promotionCode, false, false);
                    }
                    else
                    {
                        if (billingCredit.IdUserType == (int)UserTypeEnum.SUBSCRIBERS)
                        {
                            await _emailTemplatesService.SendNotificationForSuscribersPlan(accountname, userInformation, userTypePlanInformation);
                        }

                        await _emailTemplatesService.SendNotificationForUpgradePlan(accountname, userInformation, userTypePlanInformation, user, promotion, promotionCode, discountId, planDiscountInformation, false, false);
                    }

                    return;
                case BillingCreditTypeEnum.Credit_Request:
                    await _emailTemplatesService.SendNotificationForCredits(accountname, userInformation, userTypePlanInformation, user, partialBalance, promotion, promotionCode, false, true);
                    return;
                default:
                    return;
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

        private async Task SendNotificationForRejectedMercadoPagoPayment(string accountname, bool upgradePending, string paymentStatus)
        {
            User user = await _userRepository.GetUserInformation(accountname);
            await _emailTemplatesService.SendNotificationForRejectedMercadoPagoPayment(accountname, user, upgradePending, paymentStatus);
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

        private async Task UpdateInvoiceStatus(string accountName, AccountingEntry invoice, PaymentStatusEnum status, MercadoPagoPayment payment)
        {
            await _billingRepository.UpdateInvoiceStatus(invoice.IdAccountingEntry, status, payment.StatusDetail, invoice.AuthorizationNumber);

            string message = string.Empty;

            if (status == PaymentStatusEnum.Approved)
            {
                var accountingEntryMapper = new AccountingEntryForMercadopagoMapper(_paymentAmountService);
                var encryptedCreditCard = await _userRepository.GetEncryptedCreditCard(accountName);
                var paymentEntry = await accountingEntryMapper.MapToPaymentAccountingEntry(invoice, encryptedCreditCard);

                await _billingRepository.CreatePaymentEntryAsync(invoice.IdAccountingEntry, paymentEntry);

                await _emailTemplatesService.SendNotificationForMercadoPagoPaymentApproved(invoice.IdClient, accountName);

                message = string.Format("Mercadopago UpdatePayment: the invoice status was updated from {0} to {1}", invoice.Status, status.ToString());
                _logger.LogInformation(message);
                await _slackService.SendNotification(message);
            }
        }
    }
}
