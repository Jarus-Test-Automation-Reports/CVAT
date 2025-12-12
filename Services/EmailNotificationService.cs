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

        public Task NotifyAssessmentAssignedAsync(ApplicationUser assessor, Candidate candidate, Assessment assessment)
        {
            _logger.LogInformation(
                "NOTIFY: Assessor {Email} assigned to candidate {CandidateId}",
                assessor.Email, candidate.Id);

            // TODO: plug actual email/SMS here
            // e.g. send "You have a new assessment to perform"

            return Task.CompletedTask;
        }

        public Task NotifyAssessmentSubmittedAsync(ApplicationUser lead, Candidate candidate, Assessment assessment)
        {
            _logger.LogInformation(
                "NOTIFY: Lead {Email} – assessment {AssessmentId} submitted for candidate {CandidateId}",
                lead.Email, assessment.Id, candidate.Id);

            return Task.CompletedTask;
        }

        public Task NotifyAssessmentApprovedAsync(ApplicationUser lead, Candidate candidate, Assessment assessment)
        {
            _logger.LogInformation(
                "NOTIFY: Candidate {CandidateId} assessment {AssessmentId} approved by {LeadEmail}",
                candidate.Id, assessment.Id, lead.Email);

            return Task.CompletedTask;
        }
    }
}
