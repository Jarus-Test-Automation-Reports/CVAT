using System;
using System.Collections.Generic;

namespace CAT.AID.Models
{
    public class ComparisonMaster
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public string AssessmentIds { get; set; } // JSON string
        public string Status { get; set; } = "Draft"; // Draft / SentForReview / Approved
        public string? OverallComments { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public ICollection<ComparisonDetail> Details { get; set; }
        public ICollection<ComparisonEvidence> EvidenceFiles { get; set; }
    }
}
