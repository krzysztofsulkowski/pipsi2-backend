using budget_api.Models.DatabaseModels;

namespace budget_api.Models.Dto
{
    public class FrequencyDto
    {
        public Frequency Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
