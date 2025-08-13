using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IAccountCancellationReasonRepository
    {
        Task<AccountCancellationReason> GetById(int accountCancellationReasonId);
    }
}
