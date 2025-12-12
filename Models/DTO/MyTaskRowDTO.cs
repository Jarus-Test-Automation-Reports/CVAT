using CAT.AID.Models;

namespace CAT.AID.Web.Models.DTO
{
    public class MyTaskRowDTO
    {
        public Candidate Candidate { get; set; }
        public List<Assessment> Assessments { get; set; }
    }

}
