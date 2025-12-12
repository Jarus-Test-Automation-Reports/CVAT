using System;

namespace CAT.AID.Models
{
    public class ComparisonDetail
    {
        public int Id { get; set; }
        public int ComparisonId { get; set; }
        public int QuestionId { get; set; }
        public decimal? Difference { get; set; }
        public string? Notes { get; set; }

        public ComparisonMaster Master { get; set; }
    }
}
