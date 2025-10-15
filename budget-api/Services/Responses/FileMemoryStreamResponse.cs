namespace budget_api.Services.Responses
{
    public class FileMemoryStreamResponse
    {
        public FileMemoryStreamResponse(MemoryStream memoryStream, string contentType, string fileDownloadName)
        {
            MemoryStream = memoryStream;
            ContentType = contentType;
            FileDownloadName = fileDownloadName;
        }

        public MemoryStream MemoryStream { get; set; }
        public string ContentType { get; set; }
        public string FileDownloadName { get; set; }
    }
}
