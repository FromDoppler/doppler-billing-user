using System.Threading.Tasks;
using Doppler.BillingUser.Model;

namespace Doppler.BillingUser.ExternalServices.Sap
{
    public interface ISapService
    {
        Task SendUserDataToSap(SapBusinessPartner sapBusinessPartner, string resultMessage = null);
        Task SendBillingToSap(SapBillingDto sapBilling, string email);
    }
}
