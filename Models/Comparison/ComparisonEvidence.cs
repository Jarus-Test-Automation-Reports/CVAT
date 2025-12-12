using System;

namespace CAT.AID.Models
{
    public class ComparisonEvidence
    {
        public int Id { get; set; }
        public int ComparisonId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public byte[] FileData { get; set; }
        public Guid UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        public ComparisonMaster Master { get; set; }
    }
}
