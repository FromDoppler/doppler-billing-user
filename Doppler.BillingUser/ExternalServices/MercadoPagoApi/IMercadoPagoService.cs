using Doppler.BillingUser.ExternalServices.FirstData;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.MercadoPagoApi
{
    public interface IMercadoPagoService
    {
        Task<MercadoPagoPayment> GetPaymentById(long id, string accountname);
        Task<MercadoPagoPayment> CreatePayment(string accountname, int clientId, decimal total, CreditCard creditCard, bool isFreeUser, bool isReprocessCall);
    }
}
