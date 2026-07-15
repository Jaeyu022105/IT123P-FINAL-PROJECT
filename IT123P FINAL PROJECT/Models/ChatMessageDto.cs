namespace IT123P_FINAL_PROJECT.Models
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public string Text { get; set; } = string.Empty;
        public bool PlanUpdated { get; set; }
        public double ProposedCalories { get; set; }
        public double ProposedProtein { get; set; }
        public double ProposedCarbs { get; set; }
        public double ProposedFat { get; set; }
    }
}
