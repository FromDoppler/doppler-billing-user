using Doppler.BillingUser.Model;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IPayrollOfBCRAEntityRepository
    {
        Task<PayrollOfBCRAEntity> GetByBankCode(string bankCode);
    }
}
