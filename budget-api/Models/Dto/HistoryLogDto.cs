namespace budget_api.Models.Dto
{
    public class HistoryLogDto
    {
        public DateTime CreationDate { get; set; }
        public string EventType { get; set; }
        public string ObjectId { get; set; }
        public string ObjectType { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
    }
}
