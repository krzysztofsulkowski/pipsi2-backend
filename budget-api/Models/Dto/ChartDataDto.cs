namespace budget_api.Models.Dto
{
    public class ChartDataDto
    {
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; }
    }
}
