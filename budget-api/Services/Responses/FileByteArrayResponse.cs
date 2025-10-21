namespace budget_api.Services.Responses
{
    public class FileByteArrayResponse
    {
        public FileByteArrayResponse(byte[] bytes, string contentType, string fileDownloadName)
        {
            Bytes = bytes;
            ContentType = contentType;
            FileDownloadName = fileDownloadName;
        }

        public byte[] Bytes { get; set; }
        public string ContentType { get; set; }
        public string FileDownloadName { get; set; }
    }
}
