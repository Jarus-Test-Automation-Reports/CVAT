using System;
using System.Collections.Generic;
using System.Linq;

public static class RecommendationAI
{
    /// <summary>
    /// Generate recommendations based on section performance.
    /// </summary>
    public static Dictionary<string, List<string>> Generate(
        Dictionary<string, double> sectionScores,
        Dictionary<string, int> sectionMaxScores)
    {
        var result = new Dictionary<string, List<string>>();

        if (sectionScores == null || sectionScores.Count == 0)
            return result;

        foreach (var (categoryRaw, scoreRaw) in sectionScores)
        {
            if (string.IsNullOrWhiteSpace(categoryRaw))
                continue;

            string category = categoryRaw.Trim();
            double score = Math.Max(0, scoreRaw);

            if (!sectionMaxScores.TryGetValue(category, out int max) || max <= 0)
                continue;

            if (score > max)
                score = max; // safety clamp

            double pct = (score / max) * 100.0;

            // Decide support level
            SupportLevel level =
                pct >= 90 ? SupportLevel.Mild :
                pct >= 70 ? SupportLevel.Moderate :
                pct >= 1  ? SupportLevel.High :
                SupportLevel.None;

            if (level == SupportLevel.None)
                continue;

            result[category] = GetRecommendationsForCategory(category, level);
        }

        return result;
    }

    // ----------------------------------------------------------------------
    // RECOMMENDATION LOOKUP
    // ----------------------------------------------------------------------

    private enum SupportLevel { None, Mild, Moderate, High }

    private static readonly Dictionary<string, List<string>> MasterRecommendations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Personal Care & Safety Skills"] = new()
            {
                "Teach step-wise personal hygiene routines using visual aids.",
                "Practice daily living skills with real-life cues.",
                "Reinforce community and home safety rules.",
                "Provide consistent supervision during risky activities."
            },

            ["Communication and Interpersonal"] = new()
            {
                "Use structured conversation practice and role-play tasks.",
                "Encourage active listening and turn-taking.",
                "Reinforce understanding and following instructions.",
                "Introduce activities that require peer interaction."
            },

            ["Social-Emotional Maturity Skills"] = new()
            {
                "Teach recognition and expression of emotions.",
                "Use social stories to model expected behaviour.",
                "Support conflict-resolution practice in supervised groups.",
                "Encourage guided peer engagement activities."
            },

            ["Cognitive Skills"] = new()
            {
                "Introduce sequencing and problem-solving tasks.",
                "Use categorisation, memory and reasoning games.",
                "Provide structured tasks that require planning.",
                "Encourage analytical thinking through puzzles."
            },

            ["Motor Skills"] = new()
            {
                "Engage in fine-motor activities such as threading & folding.",
                "Provide gross-motor exercises to improve balance.",
                "Use task-based activities resembling workplace tasks.",
                "Practice coordinated hand-eye movement games."
            },

            ["Work-Related Functional Academic Skills"] = new()
            {
                "Teach functional literacy with job-related reading tasks.",
                "Practice money, time and measurement concepts.",
                "Use simulated workplace paperwork for training.",
                "Reinforce real-life numeracy in practical tasks."
            },

            ["Sex Education"] = new()
            {
                "Teach personal boundaries and privacy rules.",
                "Explain safe vs unsafe touch with examples.",
                "Use age-appropriate resources for body awareness.",
                "Reinforce public vs private behaviours."
            },

            ["Self-Advocacy"] = new()
            {
                "Encourage expressing choices and preferences.",
                "Introduce basic rights and responsibilities.",
                "Practice goal-setting and self-evaluation.",
                "Teach communication strategies for seeking help."
            }
        };

    // ----------------------------------------------------------------------

    private static List<string> GetRecommendationsForCategory(string category, SupportLevel level)
    {
        if (!MasterRecommendations.TryGetValue(category, out var fullList))
        {
            fullList = new List<string>
            {
                "Provide structured support to strengthen this skill area.",
                "Use guided practice with gradual reduction of prompts.",
                "Monitor progress regularly and modify strategies as needed."
            };
        }

        return level switch
        {
            SupportLevel.Mild => fullList.Take(1).ToList(),
            SupportLevel.Moderate => fullList.Take(2).ToList(),
            SupportLevel.High => fullList.ToList(),
            _ => new List<string>()
        };
    }
}
