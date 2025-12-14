namespace CAT.AID.Models.DTO

{
    public class ComparisonQuestionRowVM
    {
        public int QuestionId { get; set; }
        public string Domain { get; set; }
        public string QuestionText { get; set; }
        public List<double?> Scores { get; set; } = new();
        public double? Difference { get; set; }
        public string? Notes { get; set; }
        public int MaxScore { get; set; } = 3;   // <-- ADD THIS

    }
}


