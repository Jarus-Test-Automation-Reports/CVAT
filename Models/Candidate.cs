using CAT.AID.Web.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.AID.Models
{
    public class Candidate
    {

        public string? PhotoFileName { get; set; }   // nullable → optional
        public string? PhotoFilePath { get; set; }   // nullable → optional

        public ICollection<CandidateAttachment>? Attachments { get; set; }

        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = "";

        [Required]
        public string Gender { get; set; } = "";

        [Column(TypeName = "date")]
        public DateTime DOB { get; set; }


        public string IntellectualLevel { get; set; } = "";
        public string MaritalStatus { get; set; } = "";
        public string Education { get; set; } = "";

        public string FatherName { get; set; } = "";
        public string FatherEducation { get; set; } = "";
        public string FatherOccupation { get; set; } = "";

        public string MotherName { get; set; } = "";
        public string MotherEducation { get; set; } = "";
        public string MotherOccupation { get; set; } = "";

        public string MotherTongue { get; set; } = "";
        public string OtherLanguages { get; set; } = "";

        public string FamilyType { get; set; } = "";
        public string FamilyDisabilityHistory { get; set; } = "";
        public string DisabilityType { get; set; } = "";

        public string MonthlyIncome { get; set; } = "";
        public string ResidentialArea { get; set; } = "";
        public string ContactNumber { get; set; } = "";
        public string CommunicationAddress { get; set; } = "";

        public string? Notes { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}
