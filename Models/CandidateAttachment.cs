using System;

namespace CAT.AID.Models
{
    public class CandidateAttachment
    {
        public int Id { get; set; }

        public int CandidateId { get; set; }   // FK
        public Candidate Candidate { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }    // image/pdf/doc/xls etc
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
