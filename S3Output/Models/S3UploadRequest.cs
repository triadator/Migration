namespace S3Output.Models
{
    public class S3UploadRequest
    {
        public string Path { get; set; }
        public string BucketName { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public string FileName { get; set; }
        public Stream Content { get; set; }
    }
}
