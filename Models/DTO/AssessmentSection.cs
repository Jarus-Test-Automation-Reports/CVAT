using CAT.AID.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.AID.Web.Models.DTO
{
    [NotMapped]
    public class AssessmentSection
    {
        public string Category { get; set; }
        public List<AssessmentQuestion> Questions { get; set; } = new();

        public int MaxScore { get; set; } = 3;   // <-- ADD THIS

    }
    public class AssessmentQuestion
    {
        public int Id { get; set; }                    // Unique Question Number
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();  // A,B,C,D,E

        public string Correct { get; set; } = string.Empty; // Correct Option
        public int ScoreWeight { get; set; } = 1;          // 0–3 score
    }
}
        