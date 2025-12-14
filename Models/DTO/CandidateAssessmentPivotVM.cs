namespace CAT.AID.Models.DTO

{
    public class CandidateAssessmentPivotVM
    {
        public int CandidateId { get; set; }
        public string CandidateName { get; set; }
        public Dictionary<DateTime, int?> AssessmentIds { get; set; } = new();
        public Dictionary<int, string> StatusMapping { get; set; } = new();
    }

}

