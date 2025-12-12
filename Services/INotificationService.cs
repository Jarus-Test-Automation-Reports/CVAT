using System.Threading.Tasks;
using CAT.AID.Models;
using CAT.AID.Web.Models;

namespace CAT.AID.Web.Services
{
    public interface INotificationService
    {
        Task NotifyAssessmentAssignedAsync(ApplicationUser assessor, Candidate candidate, Assessment assessment);
        Task NotifyAssessmentSubmittedAsync(ApplicationUser lead, Candidate candidate, Assessment assessment);
        Task NotifyAssessmentApprovedAsync(ApplicationUser lead, Candidate candidate, Assessment assessment);
    }
}
