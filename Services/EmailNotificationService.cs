using System.Threading.Tasks;
using CAT.AID.Models;
using CAT.AID.Web.Models;
using Microsoft.Extensions.Logging;

namespace CAT.AID.Web.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(ILogger<EmailNotificationService> logger)
        {
            _logger = logger;
        }

        // --------------------------------------------------------------
        // ASSESSMENT ASSIGNED
        // --------------------------------------------------------------
        public Task NotifyAssessmentAssignedAsync(
            ApplicationUser assessor,
            Candidate candidate,
            Assessment assessment)
        {
            _logger.LogInformation(
                "[NOTIFY][Assigned] Assessor={AssessorEmail} CandidateId={CandidateId} AssessmentId={AssessmentId}",
                assessor?.Email ?? "N/A",
                candidate?.Id,
                assessment?.Id
            );

            // TODO: integrate real email/SMS
            return Task.CompletedTask;
        }

        // --------------------------------------------------------------
        // ASSESSMENT SUBMITTED
        // --------------------------------------------------------------
        public Task NotifyAssessmentSubmittedAsync(
            ApplicationUser lead,
            Candidate candidate,
            Assessment assessment)
        {
            _logger.LogInformation(
                "[NOTIFY][Submitted] Lead={LeadEmail} AssessmentId={AssessmentId} CandidateId={CandidateId}",
                lead?.Email ?? "N/A",
                assessment?.Id,
                candidate?.Id
            );

            return Task.CompletedTask;
        }

        // --------------------------------------------------------------
        // ASSESSMENT APPROVED
        // --------------------------------------------------------------
        public Task NotifyAssessmentApprovedAsync(
            ApplicationUser lead,
            Candidate candidate,
            Assessment assessment)
        {
            _logger.LogInformation(
                "[NOTIFY][Approved] Lead={LeadEmail} AssessmentId={AssessmentId} CandidateId={CandidateId}",
                lead?.Email ?? "N/A",
                assessment?.Id,
                candidate?.Id
            );

            return Task.CompletedTask;
        }
    }
}
