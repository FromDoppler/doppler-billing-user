using Doppler.BillingUser.Encryption;
using Doppler.BillingUser.Enums;
using Doppler.BillingUser.Infrastructure;
using Doppler.BillingUser.Model;
using Doppler.BillingUser.Utils;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Mappers.BillingCredit.LandingPlan
{
    public class BillingCreditForCreditCardMapper : IBillingCreditMapper
    {
        private readonly IBillingRepository _billingRepository;
        private readonly IEncryptionService _encryptionService;

        public BillingCreditForCreditCardMapper(IBillingRepository billingRepository, IEncryptionService encryptionService)
        {
            _billingRepository = billingRepository;
            _encryptionService = encryptionService;
        }

        public async Task<BillingCreditAgreement> MapToBillingCreditAgreement(decimal total, UserBillingInformation user, Model.BillingCredit currentBillingCredit, CreditCardPayment payment, BillingCreditTypeEnum billingCreditType)
        {
            var currentPaymentMethod = await _billingRepository.GetPaymentMethodByUserName(user.Email);

            var buyCreditAgreement = new BillingCreditAgreement
            {
                IdUser = user.IdUser,
                IdCountry = user.IdBillingCountry,
                IdPaymentMethod = (int)user.PaymentMethod,
                IdCCType = currentPaymentMethod.IdCCType,
                CCExpMonth = short.Parse(currentPaymentMethod.CCExpMonth),
                CCExpYear = short.Parse(currentPaymentMethod.CCExpYear),
                CCHolderFullName = currentPaymentMethod.CCHolderFullName,
                CCIdentificationType = currentPaymentMethod.CCType,
                CCIdentificationNumber = CreditCardHelper.ObfuscateNumber(_encryptionService.DecryptAES256(currentPaymentMethod.CCNumber)),
                CCNumber = currentPaymentMethod.CCNumber,
                CCVerification = currentPaymentMethod.CCVerification,
                IdConsumerType = !string.IsNullOrEmpty(currentPaymentMethod.IdConsumerType) ? int.Parse(currentPaymentMethod.IdConsumerType) : null,
                RazonSocial = currentPaymentMethod.RazonSocial,
                ResponsableIVA = user.ResponsableIVA,
                Cuit = currentPaymentMethod.IdentificationNumber,
                IdResponsabileBilling = (int)ResponsabileBillingEnum.QBL
            };

            DateTime now = DateTime.UtcNow;
            buyCreditAgreement.BillingCredit = new BillingCreditModel
            {
                Date = now,
                PaymentDate = now,
                ActivationDate = now,
                Approved = true,
                Payed = true,
                PlanFee = (double)total,
                IdBillingCreditType = (int)billingCreditType,
                TotalMonthPlan = currentBillingCredit.TotalMonthPlan,
                IdDiscountPlan = currentBillingCredit.IdDiscountPlan,
                CurrentMonthPlan = currentBillingCredit.CurrentMonthPlan
            };

            return buyCreditAgreement;
        }
    }
}
