using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.EmailSender;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public class EmailTemplatesService : IEmailTemplatesService
    {
        private readonly IOptions<EmailNotificationsConfiguration> _emailSettings;
        private readonly IEmailSender _emailSender;
        public EmailTemplatesService(IOptions<EmailNotificationsConfiguration> emailSettings, IEmailSender emailSender)
        {
            _emailSettings = emailSettings;
            _emailSender = emailSender;
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

        public Task SendNotificationForUpgradePlan(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, Promotion promotion, string promocode, int discountId, PlanDiscountInformation planDiscountInformation, bool isUpgradePending)
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
                        discountMonthPlan = planDiscountInformation != null ? planDiscountInformation.MonthPlan : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(adminEmail, upgradeEmail);
        }

        public Task SendNotificationForCredits(string accountname, User userInformation, UserTypePlanInformation newPlan, UserBillingInformation user, int partialBalance, Promotion promotion, string promocode, bool isUpgradePending)
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
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(creditsEmail, adminEmail);
        }

        public Task SendNotificationForPaymentFailedTransaction(int userId, string errorCode, string errorMessage, string transactionCTR, string bankMessage, PaymentMethodEnum paymentMethod, bool isFreeUser)
        {
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
                    to: new[] { _emailSettings.Value.CommercialEmail, _emailSettings.Value.BillingEmail },
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
                        total = amountDetails != null ? amountDetails.Total : 0,
                        year = DateTime.UtcNow.Year
                    },
                    to: new[] { _emailSettings.Value.AdminEmail, _emailSettings.Value.BillingEmail },
                    replyTo: _emailSettings.Value.InfoDopplerAppsEmail);

            return Task.WhenAll(updatePlanAdminEmail, updatePlanEmail);
        }

        public Task SendReprocessStatusNotification(
            string accountname,
            int userId,
            decimal amount,
            string reprocessStatus,
            decimal pendingAmount)
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
    }
}

