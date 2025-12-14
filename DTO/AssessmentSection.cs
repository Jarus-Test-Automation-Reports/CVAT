using System.Collections.Generic;

namespace CAT.AID.Models.DTO
{
    /// <summary>
    /// Represents a section in the assessment containing a group of questions.
    /// </summary>
    public class AssessmentSection
    {
        /// <summary>
        /// Section name (e.g., APTITUDE, COMMUNICATION, MOTOR SKILLS)
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// List of questions belonging to this section.
        /// </summary>
        public List<AssessmentQuestion> Questions { get; set; } = new();

        /// <summary>
        /// Maximum score allowed per question (default = 3).
        /// </summary>
        public int MaxScore { get; set; } = 3;
    }


    /// <summary>
    /// Represents a single question in the assessment.
    /// </summary>
    public class AssessmentQuestion
    {
        /// <summary>
        /// Unique question ID (used to map answers and comments).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display text of the question.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Options for multiple-choice questions (A,B,C,D,E).
        /// </summary>
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// Correct answer (OPTION KEY like A,B,C…).
        /// Not required for scoring, but kept for future use.
        /// </summary>
        public string Correct { get; set; } = string.Empty;

        /// <summary>
        /// Score weight for the question (0–3, default = 1).
        /// </summary>
        public int ScoreWeight { get; set; } = 1;
    }
}
