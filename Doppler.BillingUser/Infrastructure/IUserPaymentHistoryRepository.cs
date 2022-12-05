using Doppler.BillingUser.Model;
using System;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Infrastructure
{
    public interface IUserPaymentHistoryRepository
    {
        Task<int> CreateUserPaymentHistoryAsync(UserPaymentHistory userPaymentHistory);
        Task<int> GetAttemptsToUpdateAsync(int idUser, DateTime from, DateTime to, string source);
    }
}
