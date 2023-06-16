using Doppler.BillingUser.ApiModels;
using Doppler.BillingUser.Model;
using System.IO;

namespace Doppler.BillingUser.Mappers.PaymentMethod
{
    public static class PaymentMethodMapper
    {
        public static GetPaymentMethodResult MapFromPaymentMethodToGetPaymentMethodResult(this Model.PaymentMethod paymentMethod)
        {
            var getPaymentMethodResult = new GetPaymentMethodResult()
            {
                BankAccount = paymentMethod.BankAccount,
                BankName = paymentMethod.BankName,
                CCExpMonth = paymentMethod.CCExpMonth,
                CCExpYear = paymentMethod.CCExpYear,
                CCHolderFullName = paymentMethod.CCHolderFullName,
                CCNumber = paymentMethod.CCNumber,
                CCType = paymentMethod.CCType,
                CCVerification = paymentMethod.CCVerification,
                IdCCType = paymentMethod.IdCCType,
                IdConsumerType = paymentMethod.IdConsumerType,
                IdentificationNumber = paymentMethod.IdentificationNumber,
                IdentificationType = paymentMethod.IdentificationType,
                IdSelectedPlan = paymentMethod.IdSelectedPlan,
                PaymentMethodName = paymentMethod.PaymentMethodName,
                PaymentType = paymentMethod.PaymentType,
                PaymentWay = paymentMethod.PaymentWay,
                RazonSocial = paymentMethod.RazonSocial,
                RenewalMonth = paymentMethod.RenewalMonth,
                ResponsableIVA = paymentMethod.ResponsableIVA,
                TaxCertificate = new TaxCertificate()
                {
                    DownloadURL = paymentMethod.TaxCertificateUrl,
                    Name = Path.GetFileName(paymentMethod.TaxCertificateUrl)
                },
                TaxRegime = paymentMethod.TaxRegime,
                UseCFDI = paymentMethod.UseCFDI
            };

            return getPaymentMethodResult;
        }

    }
}
