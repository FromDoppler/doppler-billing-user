using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserAccountCancellationReasonRepository
    {
        Task<UserAccountCancellationReason> GetById(int userAccountCancellationReasonId);
    }
}
