using CAT.AID.Web.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.AID.Models   // <-- THIS WAS MISSING
{
    public class Assessment
    {
        public int Id { get; set; }

        // 🔹 Status
        public AssessmentStatus Status { get; set; } = AssessmentStatus.Assigned;

        public string? EvidenceFilesJson { get; set; }

        // 🔹 Assessment JSON data
        public string? AssessmentDataJson { get; set; }
        public string? AssessmentResultJson { get; set; }

        // 🔹 Auto scoring results
        public string? ScoreJson { get; set; }
        public double? TotalScore { get; set; }
        public double? MaxScore { get; set; }

        // 🔹 Candidate & assessors
        public int CandidateId { get; set; }
        public Candidate? Candidate { get; set; }

        public string? AssessorId { get; set; }
        [ForeignKey(nameof(AssessorId))]
        public ApplicationUser? Assessor { get; set; }

        public string? LeadAssessorId { get; set; }
        [ForeignKey(nameof(LeadAssessorId))]
        public ApplicationUser? LeadAssessor { get; set; }

        // 🔹 Comments
        public string? AssessorComments { get; set; }
        public string? LeadComments { get; set; }

        // 🔹 Status & Timeline
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // 🔹 UI logic
        [NotMapped]
        public bool IsEditableByAssessor =>
            Status == AssessmentStatus.Assigned ||
            Status == AssessmentStatus.InProgress ||
            Status == AssessmentStatus.Submitted ||
            Status == AssessmentStatus.SentBack;
    }
}
