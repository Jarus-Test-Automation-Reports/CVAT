namespace CAT.AID.Models.DTO

{
    public class AssessmentScoreDTO
    {
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public Dictionary<string, double> SectionScores { get; set; } = new();

        public double Percentage =>
            MaxScore > 0 ? (TotalScore / MaxScore) * 100 : 0;

        public Dictionary<string, Dictionary<string, int>> SectionQuestionScores { get; set; }
            = new Dictionary<string, Dictionary<string, int>>();



    }
}


