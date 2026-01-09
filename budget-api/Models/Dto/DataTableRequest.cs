namespace budget_api.Models.Dto
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }

        public string? SearchValue { get; set; }

        public int OrderColumn { get; set; }

        public string? OrderDir { get; set; }

        public Dictionary<string, string> ExtraFilters { get; set; } = new Dictionary<string, string>();
    }
}