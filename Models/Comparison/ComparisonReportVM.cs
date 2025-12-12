using CAT.AID.Models;

namespace CAT.AID.Web.Models.DTO
{
    public class ComparisonReportVM
    {
        public int ComparisonId { get; set; }

        public int CandidateId { get; set; }
        public string CandidateName { get; set; }

        public List<int> AssessmentIds { get; set; } = new();
        public List<Assessment> Assessments { get; set; } = new();


        public string OverallComments { get; set; }
        public string Status { get; set; }
        public bool IsReviewMode { get; set; }
    }
}
