using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Doppler.BillingUser.Services.Aws
{
    public class AwsS3Service : IFileStorage
    {
        private readonly IOptions<DopplerAwsSettings> _awsSettings;
        private readonly ILogger<AwsS3Service> _logger;

        public AwsS3Service(IOptions<DopplerAwsSettings> awsSettings, ILogger<AwsS3Service> logger)
        {
            _awsSettings = awsSettings;
            _logger = logger;
        }

        private IAmazonS3 CreateS3Client()
        {
            AmazonS3Client client = new AmazonS3Client(_awsSettings.Value.AccessKey, _awsSettings.Value.SecretKey,
                RegionEndpoint.GetBySystemName(_awsSettings.Value.Region));
            return client;
        }

        public async Task<string> SaveFile(byte[] data, string extension, string contentType)
        {
            var client = CreateS3Client();

            var fileName = $"{Guid.NewGuid()}{extension}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _awsSettings.Value.BucketName,
                Key = fileName,
                ContentType = contentType
            };

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(data, 0, data.Length);
                putRequest.InputStream = memoryStream;

                await client.PutObjectAsync(putRequest);

                memoryStream.Close();
            }

            return string.Format("https://{0}.s3.{1}.amazonaws.com/{2}", _awsSettings.Value.BucketName, _awsSettings.Value.Region, fileName);
        }

        public async Task<string> EditFile(byte[] data, string extension, string fileName, string contentType)
        {
            await DeleteFile(fileName);
            var url = await SaveFile(data, extension, contentType);
            return url;
        }

        public async Task DeleteFile(string fileName)
        {
            try
            {
                var client = CreateS3Client();
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.Value.BucketName,
                    Key = fileName,
                };

                await client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error trying to delete a S3 object", ex);
            }

        }
    }
}
