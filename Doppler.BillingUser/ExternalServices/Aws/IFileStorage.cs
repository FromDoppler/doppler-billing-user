using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace Doppler.BillingUser.ExternalServices.Aws
{
    public interface IFileStorage
    {
        Task<string> SaveFile(Stream data, string extension, string contentType);
        Task<string> EditFile(Stream data, string extension, string fileName, string contentType);
        Task DeleteFile(string fileName);
    }
}
