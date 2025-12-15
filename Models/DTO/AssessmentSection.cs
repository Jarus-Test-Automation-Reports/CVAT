namespace CAT.AID.Models.DTO
{
    public class AssessmentSection
    {
        public string Category { get; set; } = string.Empty;

        // JSON-driven questions
        public List<AssessmentQuestion> Questions { get; set; } = new();

        // Default max score per question
        public int MaxScore { get; set; } = 3;
    }

    public class AssessmentQuestion
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public List<string> Options { get; set; } = new();

        public string Correct { get; set; } = string.Empty;

        public int ScoreWeight { get; set; } = 1;
    }
}
