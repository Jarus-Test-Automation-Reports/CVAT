namespace CAT.AID.Models.DTO

{
    public class AssessmentResultModel
    {
        public string Section { get; set; } = "";
        public List<QuestionResult> Questions { get; set; } = new();
    }

    public class QuestionResult
    {
        public string? Value { get; set; }

        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public string? Comments { get; set; }
        public string? FileUrl { get; set; }
    }
}


