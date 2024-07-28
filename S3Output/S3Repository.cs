using Amazon.Runtime;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using MongoInput.NewModels;
using Amazon.S3.Model;
using S3Output.Models;

namespace S3Output
{
    public class S3Repository
    {
        private readonly TransferUtility _transferUtility;
        private readonly IAmazonS3 _S3Client;
        public S3Repository(S3Configuration config)
        {
            BasicAWSCredentials awsCredentials = new BasicAWSCredentials(config.AccessKey, config.SecretKey);
            var amazonConfig = new AmazonS3Config
            {
                ServiceURL = config.ServiceURL,
                ForcePathStyle = true,
                UseHttp = true,
                SignatureVersion = "4"
            };
            _S3Client =  new AmazonS3Client(awsCredentials, amazonConfig);
            _transferUtility = new TransferUtility(_S3Client);
        }
        public async Task<S3FileInfoModel> UploadFileAsync(TransferUtilityUploadRequest request)
        {
            await _transferUtility.UploadAsync(request);
            return await GetObjectInfoAsync(request.BucketName, request.Key);
        }

        public async Task<S3FileInfoModel> GetObjectInfoAsync(string bucketName, string key)
        {
            var metadataRequest = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key
            };

            var s3Response = await _S3Client.GetObjectMetadataAsync(metadataRequest);

            var fileInfoModel = new S3FileInfoModel
            {
                Key = key,
                BucketName = bucketName,
                LastModified = s3Response.LastModified,
                VersionId = s3Response.VersionId,
                Metadata = s3Response.Metadata.ToDictionary(),
                СontentLength = s3Response.ContentLength,
                ExpirationDate = s3Response.Expiration?.ExpiryDateUtc
            };

            return fileInfoModel;
        }
    }
}
