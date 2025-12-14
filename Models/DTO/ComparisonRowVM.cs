namespace CAT.AID.Models.DTO

{
    public class ComparisonRowVM
    {
        public string Domain { get; set; }
        public int QuestionId { get; set; }
        public string Question { get; set; }
        public List<string> Scores { get; set; } = new();
        public string Difference { get; set; }
    }
}


