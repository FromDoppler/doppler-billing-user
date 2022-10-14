using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserPaymentHistoryRepository
    {
        Task<int> CreateUserPaymentHistoryAsync(UserPaymentHistory userPaymentHistory);
    }
}
