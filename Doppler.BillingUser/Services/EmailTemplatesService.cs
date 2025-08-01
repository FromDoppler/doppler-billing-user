using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Extensions;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.TimeCollector;
using Doppler.BillingUser.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public class EmailTemplatesService : IEmailTemplatesService
    {
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly IEmailSender _emailSender;
        private readonly ITimeCollector _timeCollector;

        public EmailTemplatesService(IOptions<EmailNotificationsConfiguration> emailSettings, IEmailSender emailSender, ITimeCollector timeCollector)
        {
            _emailSettings = emailSettings;
            _emailSender = emailSender;
            _timeCollector = timeCollector;
        }

        public Task<bool> SendNotificationForSuscribersPlan(string accountname, User userInformation, UserTypePlanInformation newPlan)
        {
            var template = _emailSettings.Value.SubscribersPlanPromotionTemplateId[userInformation.Language ?? "en"];

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        planName = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });
        }

        public Task<bool> SendActivatedStandByEmail(string language, string fistName, int standByAmount, string sendTo)
        {
            var template = _emailSettings.Value.ActivatedStandByNotificationTemplateId[language];

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        firstName = fistName,
                        standByAmount = standByAmount,
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        year = DateTime.Now.Year,
                        isOnlyOneSubscriber = standByAmount == 1,
                    },
                    to: new[] { sendTo });
        }

        public Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, bool isUpgradePending, bool needSendToBilling)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAccountTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeRequestTemplateId[userInformation.Language ?? "en"];

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserTypeEnum.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        showMonthDescription = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 1 : false,
                        isDiscountWith3Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 3 : false,
                        isDiscountWith6Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 6 : false,
                        isDiscountWith12Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 12 : false,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.UpgradeAccountTemplateAdminTemplateId :
                _emailSettings.Value.UpgradeRequestAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: needSendToBilling ? new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail } : new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(adminEmail, upgradeEmail);
        }

        public Task SendNotificationForCredits(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, bool isUpgradePending, bool needSendToBilling)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.CreditsApprovedTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.CheckAndTransferPurchaseNotification[userInformation.Language ?? "en"];

            var creditsEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        availableCreditsQty = partialBalance + newPlan.EmailQty + (promotion != null ? promotion.ExtraCredits ?? 0 : 0),
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.CreditsApprovedAdminTemplateId :
                _emailSettings.Value.CreditsPendingAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        //userMessage = user.ExclusiveMessage, //TODO: set when the property is set in BilligCredit
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        year = DateTime.UtcNow.Year
                    },
                    to: needSendToBilling ? new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail } : new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(creditsEmail, adminEmail);
        }

        public Task SendNotificationForPaymentFailedTransaction(int userId, string errorCode, string errorMessage, string transactionCTR, string bankMessage, PaymentMethodEnum paymentMethod, bool isFreeUser, string cardHolderName, string lastFourDigits)
        {
            using var _ = _timeCollector.StartScope();
            var template = paymentMethod == PaymentMethodEnum.CC ?
                isFreeUser ? _emailSettings.Value.FailedCreditCardFreeUserPurchaseNotificationAdminTemplateId : _emailSettings.Value.FailedCreditCardPurchaseNotificationAdminTemplateId :
                //Mercadopago
                isFreeUser ? _emailSettings.Value.FailedMercadoPagoFreeUserPurchaseNotificationAdminTemplateId : _emailSettings.Value.FailedMercadoPagoPurchaseNotificationAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        errorCode,
                        errorMessage,
                        transactionCTR,
                        bankMessage,
                        cardHolderName,
                        lastFourDigits,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.CommercialEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }

        public Task SendNotificationForMercadoPagoPaymentApproved(int userId, string accountname)
        {
            var template = _emailSettings.Value.MercadoPagoPaymentApprovedAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        email = accountname,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }

        public Task SendNotificationForMercadoPagoPaymentInProcess(int userId, string accountname, string errorCode, string errorMessage, bool isFreeUser)
        {
            using var _ = _timeCollector.StartScope();
            var template = isFreeUser ?
                _emailSettings.Value.MercadoPagoFreeUserPaymentInProcessAdminTemplateId :
                _emailSettings.Value.MercadoPagoPaymentInProcessAdminTemplateId;

            return _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId,
                        email = accountname,
                        errorCode,
                        errorMessage,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.CommercialEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }

        public Task SendNotificationForUpdatePlan(
            string accountname,
            User userInformation,
            UserTypePlanInformation currentPlan,
            UserTypePlanInformation newPlan,
            UserBillingInformation user,
            Promotion promotion,
            string promocode,
            int discountId,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails)
        {
            var template = _emailSettings.Value.UpdatePlanTemplateId[userInformation.Language ?? "en"];

            var updatePlanEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserTypeEnum.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        showMonthDescription = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 1 : false,
                        isDiscountWith3Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 3 : false,
                        isDiscountWith6Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 6 : false,
                        isDiscountWith12Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 12 : false,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = _emailSettings.Value.UpdatePlanAdminTemplateId;

            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        currentIsIndividualPlan = currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        currentIsMonthlyPlan = currentPlan.IdUserType == UserTypeEnum.MONTHLY,
                        currentIsSubscribersPlan = currentPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        currentCreditsQty = currentPlan.EmailQty,
                        currentSubscribersQty = currentPlan.Subscribers,
                        currentAmount = currentPlan.Fee,
                        currentIsPaymentMethodCC = currentPlan.PaymentMethod == PaymentMethodEnum.CC,
                        currentIsPaymentMethodMP = currentPlan.PaymentMethod == PaymentMethodEnum.MP,
                        currentIsPaymentMethodTransf = currentPlan.PaymentMethod == PaymentMethodEnum.TRANSF,
                        currentIsPaymentMethodDA = currentPlan.PaymentMethod == PaymentMethodEnum.DA,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPlanFeeAdmin = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        hasDiscountPrepayment = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentAmount = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        discountPrepaymentPercentage = amountDetails != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        hasDiscountPromocode = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0,
                        discountPromocodeAmount = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0 ? amountDetails.DiscountPromocode.Amount : 0,
                        discountPromocodePercentage = amountDetails != null && amountDetails.DiscountPromocode.DiscountPercentage > 0 ? amountDetails.DiscountPromocode.DiscountPercentage : 0,
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(updatePlanAdminEmail, updatePlanEmail);
        }

        public Task SendReprocessStatusNotification(
            string accountname,
            int userId,
            decimal amount,
            string reprocessStatus,
            decimal pendingAmount,
            string paymentMethod)
        {
            var template = _emailSettings.Value.ReprocessStatusAdminTemplateId;
            return _emailSender.SafeSendWithTemplateAsync(
                templateId: template,
                templateModel: new
                {
                    urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                    userId,
                    userEmail = accountname,
                    amount,
                    reprocessStatus,
                    pendingAmount,
                    paymentMethod,
                    year = DateTime.UtcNow.Year
                },
                to: new[] { _emailSettings.Value.BillingEmail },
                replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }

        public Task SendContactInformationForTransferNotification(
            int userId,
            string username,
            string userlastname,
            string contactemail,
            string contactphonenumber)
        {
            var template = _emailSettings.Value.ContactInformationForTransferAdminTemplateId;
            return _emailSender.SafeSendWithTemplateAsync(
                templateId: template,
                templateModel: new
                {
                    urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                    userId,
                    userName = username,
                    userLastname = userlastname,
                    userEmail = contactemail,
                    userPhoneNumber = contactphonenumber,
                    year = DateTime.UtcNow.Year
                },
                to: new[] { _emailSettings.Value.BillingEmail },
                replyTo: _emailSettings.Value.InfoDopplerAppsEmail);
        }


        public Task SendNotificationForRejectedMercadoPagoPayment(string accountname, User user, bool isUpgradePending, string paymentStatusDetails)
        {
            var template = isUpgradePending ?
                _emailSettings.Value.DecliendPaymentMercadoPagoUpgradeTemplateId[user.Language ?? "en"] :
                _emailSettings.Value.DecliendPaymentMercadoPagoUpsellingTemplateId[user.Language ?? "en"];

            var creditsEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = user.FirstName,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = isUpgradePending ?
                _emailSettings.Value.DecliendPaymentMercadoPagoUpgradeAdminTemplateId :
                _emailSettings.Value.DecliendPaymentMercadoPagoUpsellingAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        userId = user.IdUser,
                        userEmail = accountname,
                        motivoError = paymentStatusDetails,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.CommercialEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(creditsEmail, adminEmail);
        }

        public Task SendNotificationForChangeIndividualToMontlyOrSubscribers(
            string accountname,
            User userInformation,
            UserTypePlanInformation currentPlan,
            UserTypePlanInformation newPlan,
            UserBillingInformation user,
            Promotion promotion,
            string promocode,
            int discountId,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails)
        {
            var template = _emailSettings.Value.UpdatePlanTemplateId[userInformation.Language ?? "en"];

            var updatePlanEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        planName = newPlan.IdUserType == UserTypeEnum.MONTHLY ? newPlan.EmailQty.ToString() : newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        showMonthDescription = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        discountPlanFee = planDiscountInformation != null ? planDiscountInformation.DiscountPlanFee : 0,
                        isDiscountWith1Month = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 1 : false,
                        isDiscountWith3Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 3 : false,
                        isDiscountWith6Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 6 : false,
                        isDiscountWith12Months = planDiscountInformation != null ? planDiscountInformation.MonthPlan == 12 : false,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { accountname });

            var templateAdmin = _emailSettings.Value.UpdatePlanCreditsToMontlyOrContactsAdminTemplateId;

            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        promotionCode = promocode,
                        promotionCodeDiscount = promotion?.DiscountPercentage,
                        promotionCodeExtraCredits = promotion?.ExtraCredits,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        billingEmails = userInformation.BillingEmails,
                        isIndividualPlan = newPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        isMonthlyPlan = newPlan.IdUserType == UserTypeEnum.MONTHLY,
                        isSubscribersPlan = newPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        creditsQty = newPlan.EmailQty,
                        subscribersQty = newPlan.Subscribers,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        currentIsIndividualPlan = currentPlan.IdUserType == UserTypeEnum.INDIVIDUAL,
                        currentIsMonthlyPlan = currentPlan.IdUserType == UserTypeEnum.MONTHLY,
                        currentIsSubscribersPlan = currentPlan.IdUserType == UserTypeEnum.SUBSCRIBERS,
                        currentCreditsQty = currentPlan.EmailQty,
                        currentSubscribersQty = currentPlan.Subscribers,
                        currentAmount = currentPlan.Fee,
                        currentIsPaymentMethodCC = currentPlan.PaymentMethod == PaymentMethodEnum.CC,
                        currentIsPaymentMethodMP = currentPlan.PaymentMethod == PaymentMethodEnum.MP,
                        currentIsPaymentMethodTransf = currentPlan.PaymentMethod == PaymentMethodEnum.TRANSF,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPlanFeeAdmin = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        hasDiscountPrepayment = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentAmount = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        discountPrepaymentPercentage = amountDetails != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        hasDiscountPromocode = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0,
                        discountPromocodeAmount = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0 ? amountDetails.DiscountPromocode.Amount : 0,
                        discountPromocodePercentage = amountDetails != null && amountDetails.DiscountPromocode.DiscountPercentage > 0 ? amountDetails.DiscountPromocode.DiscountPercentage : 0,
                        positiveBalance = amountDetails != null ? amountDetails.PositiveBalance : 0,
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(updatePlanAdminEmail, updatePlanEmail);
        }

        public Task SendNotificationForUpgradeLandingPlan(
            string accountname,
            User userInformation,
            UserBillingInformation userBillingInformation,
            IList<LandingPlan> availableLandingPlans,
            IList<LandingPlanUser> newLandingPlans,
            BillingCredit landingBillingCredit,
            bool isUpgradePending)
        {
            string newPlanDescription = "";
            decimal newPlanFee = 0;
            var totalQuantity = 0;
            foreach (LandingPlanUser newPlan in newLandingPlans)
            {
                var landingPage = availableLandingPlans.FirstOrDefault(x => x.IdLandingPlan == newPlan.IdLandingPlan);
                string planDescription = landingPage.Description;
                newPlanDescription += $"[{planDescription} x {newPlan.PackQty}]";
                newPlanFee += newPlan.PackQty * newPlan.Fee;
                totalQuantity += newPlan.PackQty * landingPage.LandingQty;
            }

            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = totalQuantity,
                        isConversationAddon = false,
                        isLandingAddon = true,
                        isOnSiteAddon = false,
                        isPushNotificationAddon = false,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = userBillingInformation.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = userBillingInformation.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = userBillingInformation.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = monthPlan,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = _emailSettings.Value.UpgradeLandingAdminTemplateId;

            var upgradePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = userBillingInformation.CFDIUse == "G03",
                        isCfdiUseP01 = userBillingInformation.CFDIUse == "P01",
                        isPaymentTypePPD = userBillingInformation.PaymentType == "PPD",
                        isPaymentTypePUE = userBillingInformation.PaymentType == "PUE",
                        isPaymentWayCash = userBillingInformation.PaymentWay == "CASH",
                        isPaymentWayCheck = userBillingInformation.PaymentWay == "CHECK",
                        isPaymentWayTransfer = userBillingInformation.PaymentWay == "TRANSFER",
                        bankName = userBillingInformation.BankName,
                        bankAccount = userBillingInformation.BankAccount,
                        taxRegime = userBillingInformation.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        isPaymentMethodCC = userBillingInformation.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = userBillingInformation.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = userBillingInformation.PaymentMethod == PaymentMethodEnum.DA,
                        is1MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 1 : false,
                        is3MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 3 : false,
                        is6MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 6 : false,
                        is12MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 12 : false,
                        newPlanDescription,
                        newPlanFee,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(upgradePlanAdminEmail, upgradeEmail);
        }

        public Task SendNotificationForUpdateLandingPlan(
            string accountname,
            User userInformation,
            UserBillingInformation userBillingInformation,
            IList<LandingPlan> availableLandingPlans,
            IList<LandingPlanUser> currentLandingPlans,
            IList<LandingPlanUser> newLandingPlans,
            BillingCredit landingBillingCredit,
            PlanAmountDetails amountDetails,
            bool isUpgradePending)
        {
            string currentPlanDescription = "";
            decimal currentPlanFee = 0;
            var totalQuantity = 0;
            foreach (LandingPlanUser currentPlan in currentLandingPlans)
            {
                string planDescription = availableLandingPlans.FirstOrDefault(x => x.IdLandingPlan == currentPlan.IdLandingPlan).Description;
                currentPlanDescription += $"[{planDescription} x {currentPlan.PackQty}]";
                currentPlanFee += currentPlan.PackQty * currentPlan.Fee;
            }

            string newPlanDescription = "";
            decimal newPlanFee = 0;
            foreach (LandingPlanUser newPlan in newLandingPlans)
            {
                var landingPage = availableLandingPlans.FirstOrDefault(x => x.IdLandingPlan == newPlan.IdLandingPlan);
                string planDescription = landingPage.Description;
                newPlanDescription += $"[{planDescription} x {newPlan.PackQty}]";
                newPlanFee += newPlan.PackQty * newPlan.Fee;
                totalQuantity += newPlan.PackQty * landingPage.LandingQty;
            }

            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = totalQuantity,
                        isConversationAddon = false,
                        isLandingAddon = true,
                        isOnSiteAddon = false,
                        isPushNotificationAddon = false,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = userBillingInformation.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = userBillingInformation.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = userBillingInformation.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = monthPlan,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = _emailSettings.Value.UpdateLandingAdminTemplateId;

            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        billingEmails = userInformation.BillingEmails,
                        isCfdiUseG03 = userBillingInformation.CFDIUse == "G03",
                        isCfdiUseP01 = userBillingInformation.CFDIUse == "P01",
                        isPaymentTypePPD = userBillingInformation.PaymentType == "PPD",
                        isPaymentTypePUE = userBillingInformation.PaymentType == "PUE",
                        isPaymentWayCash = userBillingInformation.PaymentWay == "CASH",
                        isPaymentWayCheck = userBillingInformation.PaymentWay == "CHECK",
                        isPaymentWayTransfer = userBillingInformation.PaymentWay == "TRANSFER",
                        bankName = userBillingInformation.BankName,
                        bankAccount = userBillingInformation.BankAccount,
                        taxRegime = userBillingInformation.TaxRegimeDescription,
                        isPaymentMethodCC = userBillingInformation.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = userBillingInformation.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = userBillingInformation.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = userBillingInformation.PaymentMethod == PaymentMethodEnum.DA,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPrepayment = amountDetails?.DiscountPrepayment != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentPercentage = amountDetails?.DiscountPrepayment != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        discountPrepaymentAmount = amountDetails?.DiscountPrepayment != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        hasDiscountPlanFeeAdmin = amountDetails?.DiscountPlanFeeAdmin != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails?.DiscountPlanFeeAdmin != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails?.DiscountPlanFeeAdmin != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        is1MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 1 : false,
                        is3MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 3 : false,
                        is6MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 6 : false,
                        is12MonthPlan = landingBillingCredit.TotalMonthPlan != null ? landingBillingCredit?.TotalMonthPlan == 12 : false,
                        currentPlanDescription,
                        currentPlanFee,
                        newPlanDescription,
                        newPlanFee,
                        total = (amountDetails != null && amountDetails.Total > 0) ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(updatePlanAdminEmail, upgradeEmail);
        }

        public Task SendNotificationForUpdateConversationPlan(
            string accountname,
            User userInformation,
            ChatPlan newPlan,
            UserBillingInformation user,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails,
            CurrentPlan currentPlan,
            bool isUpgradePending)
        {

            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = newPlan.ConversationQty,
                        isConversationAddon = true,
                        isLandingAddon = false,
                        isOnSiteAddon = false,
                        isPushNotificationAddon = false,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = _emailSettings.Value.UpdateConversationPlanAdminTemplateId;

            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        conversationsQty = newPlan.ConversationQty,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        currentConversationsQty = currentPlan.ConversationQty,
                        currentAmount = currentPlan.Fee,
                        currentIsPaymentMethodCC = currentPlan.PaymentMethod == (int)PaymentMethodEnum.CC,
                        currentIsPaymentMethodMP = currentPlan.PaymentMethod == (int)PaymentMethodEnum.MP,
                        currentIsPaymentMethodTransf = currentPlan.PaymentMethod == (int)PaymentMethodEnum.TRANSF,
                        currentIsPaymentMethodDA = currentPlan.PaymentMethod == (int)PaymentMethodEnum.DA,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPlanFeeAdmin = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        hasDiscountPrepayment = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentAmount = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        discountPrepaymentPercentage = amountDetails != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        hasDiscountPromocode = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0,
                        discountPromocodeAmount = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0 ? amountDetails.DiscountPromocode.Amount : 0,
                        discountPromocodePercentage = amountDetails != null && amountDetails.DiscountPromocode.DiscountPercentage > 0 ? amountDetails.DiscountPromocode.DiscountPercentage : 0,
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [_emailSettings.Value.AdminEmail],
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(upgradeEmail, updatePlanAdminEmail);
        }

        public Task SendNotificationForUpgradeConversationPlan(
            string accountname,
            User userInformation,
            ChatPlan newPlan,
            UserBillingInformation user,
            PlanDiscountInformation planDiscountInformation,
            bool isUpgradePending,
            bool needSendToBilling)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = newPlan.ConversationQty,
                        isConversationAddon = true,
                        isLandingAddon = false,
                        isOnSiteAddon = false,
                        isPushNotificationAddon = false,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.UpgradeConversationPlanAdminTemplateId :
                _emailSettings.Value.UpgradeConversationPlanRequestAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        conversationsQty = newPlan.ConversationQty,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: needSendToBilling ? [_emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail] : new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(upgradeEmail, adminEmail);
        }

        public Task SendNotificationForUpdateAddOnPlan(
            string accountname,
            User userInformation,
            AddOnPlan newPlan,
            UserBillingInformation user,
            PlanDiscountInformation planDiscountInformation,
            PlanAmountDetails amountDetails,
            CurrentPlan currentPlan,
            bool isUpgradePending,
            AddOnType addOnType)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = newPlan.Quantity,
                        isConversationAddon = addOnType == AddOnType.Chat,
                        isLandingAddon = addOnType == AddOnType.Landing,
                        isOnSiteAddon = addOnType == AddOnType.OnSite,
                        isPushNotificationAddon = addOnType == AddOnType.PushNotification,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = _emailSettings.Value.UpdateAddOnPlanAdminTemplateId;
            var updatePlanAdminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        addOnType = addOnType.ToString(),
                        currentAddOnIsOnSite = addOnType == AddOnType.OnSite,
                        currentAddOnIsPushNotification = addOnType == AddOnType.PushNotification,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        quantity = newPlan.Quantity,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        currentQuantity = currentPlan.Quantity,
                        currentAmount = currentPlan.Fee,
                        currentIsPaymentMethodCC = currentPlan.PaymentMethod == (int)PaymentMethodEnum.CC,
                        currentIsPaymentMethodMP = currentPlan.PaymentMethod == (int)PaymentMethodEnum.MP,
                        currentIsPaymentMethodTransf = currentPlan.PaymentMethod == (int)PaymentMethodEnum.TRANSF,
                        currentIsPaymentMethodDA = currentPlan.PaymentMethod == (int)PaymentMethodEnum.DA,
                        hasDiscountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0,
                        discountPaymentAlreadyPaid = amountDetails != null && amountDetails.DiscountPaymentAlreadyPaid > 0 ? amountDetails.DiscountPaymentAlreadyPaid : 0,
                        hasDiscountPlanFeeAdmin = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0,
                        discountPlanFeeAdminAmount = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.Amount > 0 ? amountDetails.DiscountPlanFeeAdmin.Amount : 0,
                        discountPlanFeeAdminPercentage = amountDetails != null && amountDetails.DiscountPlanFeeAdmin.DiscountPercentage > 0 ? amountDetails.DiscountPlanFeeAdmin.DiscountPercentage : 0,
                        hasDiscountPrepayment = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0,
                        discountPrepaymentAmount = amountDetails != null && amountDetails.DiscountPrepayment.Amount > 0 ? amountDetails.DiscountPrepayment.Amount : 0,
                        discountPrepaymentPercentage = amountDetails != null && amountDetails.DiscountPrepayment.DiscountPercentage > 0 ? amountDetails.DiscountPrepayment.DiscountPercentage : 0,
                        hasDiscountPromocode = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0,
                        discountPromocodeAmount = amountDetails != null && amountDetails.DiscountPromocode.Amount > 0 ? amountDetails.DiscountPromocode.Amount : 0,
                        discountPromocodePercentage = amountDetails != null && amountDetails.DiscountPromocode.DiscountPercentage > 0 ? amountDetails.DiscountPromocode.DiscountPercentage : 0,
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [_emailSettings.Value.AdminEmail],
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(upgradeEmail, updatePlanAdminEmail);
        }

        public Task SendNotificationForUpgradeAddOnPlan(
            string accountname,
            User userInformation,
            AddOnPlan newPlan,
            UserBillingInformation user,
            PlanDiscountInformation planDiscountInformation,
            bool isUpgradePending,
            bool needSendToBilling,
            AddOnType addOnType)
        {
            var template = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanTemplateId[userInformation.Language ?? "en"] :
                _emailSettings.Value.UpgradeAddOnPlanRequestTemplateId[userInformation.Language ?? "en"];

            var monthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 1;

            var upgradeEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        quantity = newPlan.Quantity,
                        isConversationAddon = addOnType == AddOnType.Chat,
                        isLandingAddon = addOnType == AddOnType.Landing,
                        isOnSiteAddon = addOnType == AddOnType.OnSite,
                        isPushNotificationAddon = addOnType == AddOnType.PushNotification,
                        isDiscountWith1Month = monthPlan == 1,
                        isDiscountWith3Months = monthPlan == 3,
                        isDiscountWith6Months = monthPlan == 6,
                        isDiscountWith12Months = monthPlan == 12,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.BillingEmail]);

            var templateAdmin = !isUpgradePending ?
                _emailSettings.Value.UpgradeAddOnPlanAdminTemplateId :
                _emailSettings.Value.UpgradeAddOnPlanRequestAdminTemplateId;

            var adminEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        addOnType = addOnType.ToString(),
                        currentAddOnIsOnSite = addOnType == AddOnType.OnSite,
                        currentAddOnIsPushNotification = addOnType == AddOnType.PushNotification,
                        user = accountname,
                        client = $"{userInformation.FirstName} {userInformation.LastName}",
                        address = userInformation.Address,
                        phone = userInformation.PhoneNumber,
                        company = userInformation.Company,
                        city = userInformation.CityName,
                        state = userInformation.BillingStateName,
                        zipCode = userInformation.ZipCode,
                        language = userInformation.Language,
                        country = userInformation.BillingCountryName,
                        vendor = userInformation.Vendor,
                        razonSocial = userInformation.RazonSocial,
                        cuit = userInformation.CUIT,
                        isConsumerCF = userInformation.IdConsumerType == (int)ConsumerTypeEnum.CF,
                        isConsumerRFC = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RFC,
                        isConsumerRI = userInformation.IdConsumerType == (int)ConsumerTypeEnum.RI,
                        isEmptyConsumer = userInformation.IdConsumerType == 0,
                        isCfdiUseG03 = user.CFDIUse == "G03",
                        isCfdiUseP01 = user.CFDIUse == "P01",
                        isPaymentTypePPD = user.PaymentType == "PPD",
                        isPaymentTypePUE = user.PaymentType == "PUE",
                        isPaymentWayCash = user.PaymentWay == "CASH",
                        isPaymentWayCheck = user.PaymentWay == "CHECK",
                        isPaymentWayTransfer = user.PaymentWay == "TRANSFER",
                        bankName = user.BankName,
                        bankAccount = user.BankAccount,
                        taxRegime = user.TaxRegimeDescription,
                        billingEmails = userInformation.BillingEmails,
                        quantity = newPlan.Quantity,
                        amount = newPlan.Fee,
                        isPaymentMethodCC = user.PaymentMethod == PaymentMethodEnum.CC,
                        isPaymentMethodMP = user.PaymentMethod == PaymentMethodEnum.MP,
                        isPaymentMethodTransf = user.PaymentMethod == PaymentMethodEnum.TRANSF,
                        isPaymentMethodDA = user.PaymentMethod == PaymentMethodEnum.DA,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: needSendToBilling ? [_emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail] : new[] { _emailSettings.Value.AdminEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(upgradeEmail, adminEmail);
        }


        public Task SendNotificationForRequestAdditionalServices(
            string accountname,
            User user,
            BillingCredit currentBillingCredit,
            PlanDiscountInformation planDiscountInformation,
            AdditionalServicesRequestModel additionalServicesRequestModel)
        {
            var template = _emailSettings.Value.SendAdditionalServiceRequestTemplateId[user.Language ?? "en"];

            var sendAdditionalServicesRequestForUser = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = user.FirstName,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname]);

            var fee = currentBillingCredit != null ? (decimal?)currentBillingCredit.PlanFee : null;
            var isIndividualPlan = currentBillingCredit != null && currentBillingCredit.IdUserType == (int)UserTypeEnum.INDIVIDUAL;
            var isMonthlyPlan = currentBillingCredit != null && currentBillingCredit.IdUserType == (int)UserTypeEnum.MONTHLY;
            var isSubscribersPlan = currentBillingCredit != null && currentBillingCredit.IdUserType == (int)UserTypeEnum.SUBSCRIBERS;
            var creditsQty = currentBillingCredit?.CreditsQty;
            var subscribersQty = currentBillingCredit?.SubscribersQty;

            var templateAdmin = _emailSettings.Value.SendAdditionalServiceRequestAdminTemplateId;
            var sendAdditionalServicesRequestForAdmin = _emailSender.SafeSendWithTemplateAsync(
                    templateId: templateAdmin,
                    templateModel: new
                    {
                        email = additionalServicesRequestModel.Email,
                        firstAndLastName = user.FirstName + " " + user.LastName,
                        phone = additionalServicesRequestModel.Phone,
                        contactSchedule = additionalServicesRequestModel.ContactSchedule,
                        sendingVolume = additionalServicesRequestModel.SendingVolume,
                        features = additionalServicesRequestModel.Features,
                        isIndividualPlan,
                        isMonthlyPlan,
                        isSubscribersPlan,
                        creditsQty,
                        subscribersQty,
                        amount = fee,
                        currentPaymentMethod = user.PaymentMethod == (int)PaymentMethodEnum.CC ? "Tarjeta de Crédito"
                            : user.PaymentMethod == (int)PaymentMethodEnum.MP ? "Mercadopago"
                            : user.PaymentMethod == (int)PaymentMethodEnum.TRANSF ? "Transferencia"
                            : null,
                        hasDiscountMonthPlan = planDiscountInformation != null,
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        year = DateTime.UtcNow.Year
                    },
                    to: [_emailSettings.Value.AdminEmail],
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(sendAdditionalServicesRequestForUser, sendAdditionalServicesRequestForAdmin);
        }

        public Task SendNotificationForCancelAddOnPlan(string accountname, User userInformation, AddOnType addOnType)
        {
            var template = _emailSettings.Value.CancelAddOnPlanTemplateId[userInformation.Language ?? "en"];

            var cancelAddonEmail = _emailSender.SafeSendWithTemplateAsync(
                    templateId: template,
                    templateModel: new
                    {
                        urlImagesBase = _emailSettings.Value.UrlEmailImagesBase,
                        firstName = userInformation.FirstName,
                        isConversationAddon = addOnType == AddOnType.Chat,
                        isLandingAddon = addOnType == AddOnType.Landing,
                        isOnSiteAddon = addOnType == AddOnType.OnSite,
                        isPushNotificationAddon = addOnType == AddOnType.PushNotification,
                        year = DateTime.UtcNow.Year
                    },
                    to: [accountname],
                    cc: [_emailSettings.Value.CustomerExperienceEmail]);

            return cancelAddonEmail;
        }

    }
}

