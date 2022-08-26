using Doppler.BillingUser.Enums;
using Doppler.BillingUser.ExternalServices.Sap;
using Doppler.BillingUser.ExternalServices.Zoho;
using Doppler.BillingUser.ExternalServices.Zoho.API;
using Doppler.BillingUser.Model;
using System;
using System.Linq;

namespace Doppler.BillingUser.Utils
{
    public static class BillingHelper
    {
        private const int CurrencyTypeUsd = 0;

        public static bool IsUpgradePending(UserBillingInformation user, Promotion promotion, CreditCardPayment creditCardPayment)
        {
            return user.PaymentMethod switch
            {
                PaymentMethodEnum.CC => false,
                PaymentMethodEnum.TRANSF => promotion == null || (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value < 100) || !promotion.DiscountPercentage.HasValue,
                PaymentMethodEnum.MP => creditCardPayment?.Status == PaymentStatusEnum.Pending &&
                                        (promotion == null || (promotion.DiscountPercentage.HasValue && promotion.DiscountPercentage.Value < 100) || !promotion.DiscountPercentage.HasValue),
                _ => true,
            };
        }

        public static SapBillingDto MapBillingToSapAsync(SapSettings timeZoneOffset, string cardNumber, string cardHolderName, BillingCredit billingCredit, UserTypePlanInformation currentUserPlan, UserTypePlanInformation newUserPlan, string authorizationNumber, int invoicedId, decimal? total)
        {
            var sapBilling = new SapBillingDto
            {
                Id = billingCredit.IdUser,
                CreditsOrSubscribersQuantity = newUserPlan.IdUserType == UserTypeEnum.SUBSCRIBERS ? newUserPlan.SubscribersQty.GetValueOrDefault() : billingCredit.CreditsQty.GetValueOrDefault(),
                IsCustomPlan = (new[] { 0, 9, 17 }).Contains(billingCredit.IdUserTypePlan),
                IsPlanUpgrade = true, // TODO: Check when the other types of purchases are implemented.
                Currency = CurrencyTypeUsd,
                Periodicity = BillingHelper.GetPeriodicity(newUserPlan, billingCredit),
                PeriodMonth = billingCredit.Date.Month,
                PeriodYear = billingCredit.Date.Year,
                PlanFee = newUserPlan.IdUserType == UserTypeEnum.SUBSCRIBERS ? billingCredit.PlanFee * (billingCredit.TotalMonthPlan ?? 1) : billingCredit.PlanFee,
                Discount = (billingCredit.DiscountPlanFee) +
                    billingCredit.DiscountPlanFeeAdmin.GetValueOrDefault() +
                    billingCredit.DiscountPlanFeePromotion.GetValueOrDefault(),
                ExtraEmailsPeriodMonth = billingCredit.Date.Month,
                ExtraEmailsPeriodYear = billingCredit.Date.Year,
                ExtraEmailsFee = 0,
                IsFirstPurchase = currentUserPlan == null,
                PlanType = (int)newUserPlan.IdUserType,
                CardHolder = cardHolderName,
                CardType = billingCredit.CCIdentificationType,
                CardNumber = !string.IsNullOrEmpty(cardNumber) ? cardNumber[^4..] : string.Empty,
                CardErrorCode = "100",
                CardErrorDetail = "Successfully approved",
                TransactionApproved = true,
                TransferReference = authorizationNumber,
                InvoiceId = invoicedId,
                PaymentDate = billingCredit.Date.ToHourOffset(timeZoneOffset.TimeZoneOffset),
                InvoiceDate = billingCredit.Date.ToHourOffset(timeZoneOffset.TimeZoneOffset),
                BillingSystemId = billingCredit.IdResponsabileBilling,
                FiscalID = billingCredit.Cuit,
                IsUpSelling = currentUserPlan != null && currentUserPlan.IdUserType != UserTypeEnum.INDIVIDUAL,
            };

            if (currentUserPlan != null)
            {
                sapBilling.DiscountedAmount = (double?)total;
            }

            return sapBilling;
        }


        public static int? GetPeriodicity(UserTypePlanInformation newUserPlan, BillingCredit billingCredit)
        {
            return newUserPlan.IdUserType == UserTypeEnum.INDIVIDUAL ?
                null : billingCredit.TotalMonthPlan == 3 ?
                1 : billingCredit.TotalMonthPlan == 6 ?
                2 : billingCredit.TotalMonthPlan == 12 ? 3 : 0;
        }

        public static void MapForUpgrade(ZohoEntityLead lead, ZohoDTO zohoDto)
        {
            lead.Doppler = zohoDto.Doppler;
            if (zohoDto.FirstPaymentDate == DateTime.MinValue)
            {
                lead.DFirstPayment = null;
            }
            else
            {
                lead.DFirstPayment = zohoDto.FirstPaymentDate;
            }
            lead.DDiscountType = zohoDto.DiscountType;
            lead.DBillingSystem = zohoDto.BillingSystem;
            if (zohoDto.UpgradeDate == DateTime.MinValue)
            {
                lead.DUpgradeDate = null;
            }
            else
            {
                lead.DUpgradeDate = zohoDto.UpgradeDate;
            }
            lead.DPromoCode = zohoDto.PromoCodo;
            lead.DDiscountTypeDesc = zohoDto.DiscountTypeDescription;
            lead.Industry = zohoDto.Industry;
        }

        public static void MapForUpgrade(ZohoEntityAccount account, ZohoDTO zohoDto)
        {
            account.Doppler = zohoDto.Doppler;
            if (zohoDto.FirstPaymentDate == DateTime.MinValue)
            {
                account.DFirstPayment = null;
            }
            else
            {
                account.DFirstPayment = zohoDto.FirstPaymentDate;
            }
            account.DDiscountType = zohoDto.DiscountType;
            account.DBillingSystem = zohoDto.BillingSystem;
            if (zohoDto.UpgradeDate == DateTime.MinValue)
            {
                account.DUpgradeDate = null;
            }
            else
            {
                account.DUpgradeDate = zohoDto.UpgradeDate;
            }
            account.DPromoCode = zohoDto.PromoCodo;
            account.DDiscountTypeDesc = zohoDto.DiscountTypeDescription;
            account.Industry = zohoDto.Industry;
        }
    }
}
