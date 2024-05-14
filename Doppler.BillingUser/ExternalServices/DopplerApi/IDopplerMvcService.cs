using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.DopplerApi
{
    public interface IDopplerMvcService
    {
        Task<int> GetPublishedLandingPages(int idUser);
    }
}
