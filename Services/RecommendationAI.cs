using System;
using System.Collections.Generic;

public static class RecommendationAI
{
    /// <summary>
    /// sectionScores      : actual scores per section (double)
    /// sectionMaxScores   : max possible scores per section (int)
    /// Needs support if Achieved % < 100
    /// </summary>
    public static Dictionary<string, List<string>> Generate(
        Dictionary<string, double> sectionScores,
        Dictionary<string, int> sectionMaxScores)
    {
        var result = new Dictionary<string, List<string>>();

        if (sectionScores == null || sectionScores.Count == 0)
            return result;

        foreach (var sec in sectionScores)
        {
            string category = sec.Key;
            double score = sec.Value;

            if (!sectionMaxScores.TryGetValue(category, out int max) || max <= 0)
                continue;

            double pct = (score / max) * 100.0;

            // 🔥 Your rule: anything below 100% → needs support
            if (pct < 100.0)
            {
                result[category] = GetRecommendationsForCategory(category);
            }
        }

        return result;
    }

    private static List<string> GetRecommendationsForCategory(string category)
    {
        // Simple mapping by category name – adjust text as you like
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Personal Care & Safety Skills", new()
                {
                    "Provide step-wise training for personal hygiene routines.",
                    "Introduce visual schedules for daily self-care activities.",
                    "Reinforce safety rules in home and community settings."
                }
            },
            { "Communication and Interpersonal", new()
                {
                    "Use structured conversation and role-play activities.",
                    "Encourage turn-taking and listening during interaction.",
                    "Practice understanding and following verbal instructions."
                }
            },
            { "Social-Emotional Maturity Skills", new()
                {
                    "Teach recognition and expression of basic emotions.",
                    "Use social stories to model appropriate behaviour.",
                    "Provide guided peer-group activities to build social skills."
                }
            },
            { "Cognitive Skills", new()
                {
                    "Introduce sequencing and problem-solving tasks.",
                    "Use matching, categorisation and memory games.",
                    "Provide workplace-like tasks that need planning."
                }
            },
            { "Motor Skills", new()
                {
                    "Practice fine-motor tasks like threading, folding and cutting.",
                    "Engage in gross-motor activities to build strength and balance.",
                    "Use task-based motor activities related to work settings."
                }
            },
            { "Work-Related Functional Academic Skills", new()
                {
                    "Introduce functional money, time and measurement activities.",
                    "Use job-related reading/writing tasks (labels, forms, slips).",
                    "Practice simple numeracy in real or simulated work situations."
                }
            },
            { "Sex Education", new()
                {
                    "Teach privacy rules and appropriate personal boundaries.",
                    "Explain safe vs unsafe touch and how to seek help.",
                    "Reinforce public vs private behaviours using clear examples."
                }
            },
            { "Self-Advocacy", new()
                {
                    "Encourage the candidate to express choices and preferences.",
                    "Teach basic rights and responsibilities in daily life.",
                    "Set simple personal goals and review achievements regularly."
                }
            }
        };

        if (map.TryGetValue(category, out var recs))
            return recs;

        // Default generic recommendations if category not matched
        return new List<string>
        {
            "Provide structured support and gradual skill-building activities in this area.",
            "Monitor progress regularly and adjust the level of assistance as needed."
        };
    }
}
