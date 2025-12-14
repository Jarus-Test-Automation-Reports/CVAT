namespace CAT.AID.Models.DTO

{
    public class CandidateProgressDTO
    {
        public int AssessmentId { get; set; }
        public DateTime Date { get; set; }
        public double Total { get; set; }
        public double Max { get; set; }
        public double Percentage { get; set; }
        public Dictionary<string, double> SectionScores { get; set; } = new();
      

    }
}


