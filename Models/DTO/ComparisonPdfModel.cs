using CAT.AID.Models;

namespace CAT.AID.Web.Models
{
    public class ComparisonPdfModel
    {
        public string CandidateName { get; set; }
        public List<Assessment> Assessments { get; set; }
        public Dictionary<string, List<(string Question, string Answer)>> Comparison { get; set; }
    }


}
