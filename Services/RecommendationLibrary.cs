public static class RecommendationLibrary
{
    /// <summary>
    /// A clean, structured recommendation dataset for each skill category.
    /// The key is case-insensitive to avoid mismatch issues.
    /// </summary>
    public static readonly Dictionary<string, List<string>> Data =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Cognitive Skills"] = new()
            {
                "Practice problem-solving puzzles daily.",
                "Engage in memory activities (flash cards, sequencing tasks).",
                "Introduce categorization and sorting tasks.",
                "Use real-life problem situations to build reasoning."
            },

            ["Motor Skills"] = new()
            {
                "Perform daily hand grip and fine motor strengthening exercises.",
                "Engage in threading, buttoning, and peg-board activities.",
                "Use activities that require bilateral coordination.",
                "Practice workplace-like motor tasks (folding, packing)."
            },

            ["Communication Skills"] = new()
            {
                "Encourage expressive speech through storytelling.",
                "Use picture-to-word pairing and labelling exercises.",
                "Practice turn-taking in structured communication tasks.",
                "Model sentence formation using visual prompts."
            },

            ["Social-Emotional Skills"] = new()
            {
                "Teach recognition of basic emotions using visuals.",
                "Use role-play to practice appropriate social responses.",
                "Encourage peer interaction through guided group games.",
                "Use social stories to build behavioural understanding."
            },

            ["Personal Care & Safety Skills"] = new()
            {
                "Reinforce daily self-care routines with visual schedules.",
                "Teach safe and unsafe situations through examples.",
                "Practice road safety, home safety and public behaviour.",
                "Encourage independence in hygiene tasks."
            },

            ["Work-Related Functional Academic Skills"] = new()
            {
                "Introduce simple money-handling activities.",
                "Practice job-related reading (labels, forms).",
                "Perform functional numeracy tasks in real-life settings.",
                "Teach time management through daily task charts."
            },

            ["Sex Education"] = new()
            {
                "Teach privacy rules and appropriate personal boundaries.",
                "Use clear examples to explain safe vs unsafe touch.",
                "Reinforce public vs private behaviour expectations.",
                "Encourage the candidate to report discomfort to trusted adults."
            },

            ["Self-Advocacy"] = new()
            {
                "Encourage expressing choices and preferences.",
                "Teach personal rights and responsibilities.",
                "Practice communicating needs in different situations.",
                "Set simple personal goals and track progress."
            }
        };
}
