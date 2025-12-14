using CAT.AID.Models;
using CAT.AID.Web.Models;
using CAT.AID.Models.DTO;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{

    public DbSet<CandidateAttachment> CandidateAttachments { get; set; } // <-- Add this

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Candidate> Candidates { get; set; }
    public DbSet<Assessment> Assessments { get; set; }
    public DbSet<ComparisonMaster> ComparisonMasters { get; set; }
    public DbSet<ComparisonDetail> ComparisonDetails { get; set; }
    public DbSet<ComparisonEvidence> ComparisonEvidences { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("public");

        // Enum → int mapping
        builder.Entity<Assessment>()
            .Property(a => a.Status)
            .HasConversion<int>();

        // ⬇ Ensure FK AssessorId & LeadAssessorId remain string (GUID)
        builder.Entity<Assessment>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.AssessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Assessment>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.LeadAssessorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    public DbSet<AssessmentSection> AssessmentSections { get; set; }

}

