using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.AID.Models
{
    [NotMapped]   // <--- prevents EF from treating it as a table
    public class AssessmentQuestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Text { get; set; } = "";
        public string Type { get; set; } = "mcq";
        public List<string> Options { get; set; } = new();
        public int Scale { get; set; } = 5;
        public bool AllowComments { get; set; } = false;
        public bool AllowFileUpload { get; set; } = false;
    }
}
