using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.BeplicApi
{
    public interface IBeplicService
    {
        Task AssignPlanToUser(int userId, string planName);
    }
}
