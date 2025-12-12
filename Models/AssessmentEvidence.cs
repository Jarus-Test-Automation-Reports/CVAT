using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.AID.Models
{
    public class AssessmentEvidence
    {
        public int Id { get; set; }

        public int AssessmentId { get; set; }
        [ForeignKey(nameof(AssessmentId))]
        public Assessment Assessment { get; set; }

        public string FileName { get; set; } = "";
        public string FileUrl { get; set; } = "";
    }
}
