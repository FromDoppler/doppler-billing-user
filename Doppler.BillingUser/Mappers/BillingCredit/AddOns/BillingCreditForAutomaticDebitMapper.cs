using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.AddOns
{
    public class BillingCreditForAutomaticDebitMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private const int ArgentinaIva = 21;

        public BillingCreditForAutomaticDebitMapper(IBillingRepository billingRepository)
        {
            _billingRepository = billingRepository;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = null,
                CCExpMonth = null,
                CCExpYear = null,
                CCHolderFullName = null,
                CCIdentificationType = null,
                CCIdentificationNumber = null,
                CCNumber = null,
                CCVerification = null,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                Cbu = user.Cbu,
                CFDIUse = null,
                PaymentWay = null,
                PaymentType = null,
                BankName = null,
                BankAccount = null,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.GBBISIDE
            };

            DateTime now = DateTime.UtcNow;
            var isUpgradePending = BillingHelper.IsUpgradePending(user, null, payment);

            buyCreditAgreement.BillingCredit = new BillingCreditModel()
            {
                Date = now,
                PaymentDate = (billingCreditType == BillingCreditTypeEnum.Landing_Request ||
                                billingCreditType == BillingCreditTypeEnum.Conversation_Request) ? !isUpgradePending ? now : null : now,
                ActivationDate = (billingCreditType == BillingCreditTypeEnum.Landing_Request ||
                                billingCreditType == BillingCreditTypeEnum.Conversation_Request) ? !isUpgradePending ? now : null : now,
                Approved = (billingCreditType != BillingCreditTypeEnum.Landing_Request && billingCreditType != BillingCreditTypeEnum.Conversation_Request) || !isUpgradePending,
                PlanFee = (double)total,
                IdBillingCreditType = (int)billingCreditType,
                TotalMonthPlan = currentBillingCredit.TotalMonthPlan,
                IdDiscountPlan = currentBillingCredit.IdDiscountPlan,
                CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan
            };

            buyCreditAgreement.BillingCredit.Payed = buyCreditAgreement.BillingCredit.PaymentDate != null;

            buyCreditAgreement.BillingCredit.Taxes = Convert.ToDouble(total * ArgentinaIva / 100);

            return buyCreditAgreement;
        }
    }
}
