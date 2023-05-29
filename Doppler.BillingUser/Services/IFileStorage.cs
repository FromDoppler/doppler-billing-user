using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services
{
    public interface IFileStorage
    {
        Task<string> SaveFile(byte[] data, string extension, string contentType);
        Task<string> EditFile(byte[] data, string extension, string fileName, string contentType);
        Task DeleteFile(string fileName);
    }
}
