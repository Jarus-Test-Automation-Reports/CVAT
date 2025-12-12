using System;
using System.Collections.Generic;

namespace CAT.AID.Web.Models.DTO
{
    public class AssessmentCompareVM
    {
        public string Section { get; set; }
        public string Question { get; set; }
        public Dictionary<int, int?> Scores { get; set; } = new();
    }
}
