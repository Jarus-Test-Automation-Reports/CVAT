namespace CAT.AID.Models.DTO

{
    public class DashboardDTO
    {
        // Summary Cards
        public int TotalAssessments { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }

        // Monthly Trend
        public List<string> MonthLabels { get; set; } = new();
        public List<int> MonthCounts { get; set; } = new();

        // Assessor Performance
        public List<string> AssessorNames { get; set; } = new();
        public List<int> AssessorCounts { get; set; } = new();

        // Low Performing Domains
        public Dictionary<string, double> LowDomains { get; set; } = new();

        // Activity Timeline (last 30 days)
        public List<string> RecentDates { get; set; } = new();
        public List<int> RecentCounts { get; set; } = new();
    }
}

