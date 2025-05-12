using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPayrollOfBCRAEntityRepository
    {
        Task<bool> IsValidBankCode(string bankCode);
    }
}
