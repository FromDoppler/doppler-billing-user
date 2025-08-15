using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserAccountCancellationRequestRepository
    {
        Task<int> SaveRequestAsync(int userId, string contactName, string accountCancellationReason, string contactPhone, string contactSchedule);
    }
}
