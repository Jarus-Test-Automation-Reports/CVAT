using CAT.AID.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class AssessmentResult
{
    public int Id { get; set; }

    public int AssessmentId { get; set; }
    [ForeignKey(nameof(AssessmentId))]
    public Assessment Assessment { get; set; }

    public int QuestionId { get; set; }   // stored as numeric reference only
    [NotMapped]
    public AssessmentQuestion? Question { get; set; }

    public int Score { get; set; }
    public string? Comment { get; set; }
}
