public static class RecommendationLibrary
{
    public static readonly Dictionary<string, Dictionary<string, List<string>>> Data =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ------------------------------------------------------------------
            // 1. COGNITIVE SKILLS
            // ------------------------------------------------------------------
            ["Cognitive Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Practice visual memory tasks such as picture recall.",
                    "Engage in simple sequencing activities (2–3 steps).",
                    "Use matching and sorting exercises to build attention.",
                    "Introduce simple classification tasks (colors, shapes)."
                },
                ["Moderate"] = new()
                {
                    "Use logical reasoning puzzles (patterns, analogies).",
                    "Practice multi-step sequencing activities (4–6 steps).",
                    "Introduce simple planning tasks like arranging daily routines.",
                    "Use story-based questions to improve comprehension."
                },
                ["Intensive"] = new()
                {
                    "Engage in real-life problem solving (shopping, travel planning).",
                    "Use cognitive-behavioural tasks requiring reasoning and prediction.",
                    "Implement structured cognitive training programs daily.",
                    "Provide scaffolded workplace-like problem-solving situations."
                }
            },

            // ------------------------------------------------------------------
            // 2. MOTOR SKILLS
            // ------------------------------------------------------------------
            ["Motor Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Practice fine-motor tasks like buttoning, zipping, and threading.",
                    "Perform wrist-strengthening activities using soft clay.",
                    "Use tracing and colouring exercises to improve control."
                },
                ["Moderate"] = new()
                {
                    "Introduce bilateral coordination activities (cutting, folding).",
                    "Practice task endurance through repetitive fine-motor activities.",
                    "Use pegboards, tweezers, and grip tools for strengthening."
                },
                ["Intensive"] = new()
                {
                    "Practice workplace-like motor tasks (packing, assembling items).",
                    "Use occupational therapy-based motor planning activities.",
                    "Engage in structured gross-motor programs (balance, agility)."
                }
            },

            // ------------------------------------------------------------------
            // 3. COMMUNICATION SKILLS
            // ------------------------------------------------------------------
            ["Communication Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Use picture-to-word pairing activities.",
                    "Encourage single-word or short phrase responses.",
                    "Practice naming familiar objects and actions.",
                    "Use modelling and repetition to teach vocabulary."
                },
                ["Moderate"] = new()
                {
                    "Practice structured conversation with prompts.",
                    "Engage in role-play to teach social communication.",
                    "Use WH-questions to improve comprehension.",
                    "Encourage descriptive sentences using visual scenes."
                },
                ["Intensive"] = new()
                {
                    "Use functional communication training across real environments.",
                    "Teach conflict resolution and assertive communication.",
                    "Implement AAC support if needed (PECS, speech devices).",
                    "Use pragmatic language programs for social interaction."
                }
            },

            // ------------------------------------------------------------------
            // 4. SOCIAL-EMOTIONAL SKILLS
            // ------------------------------------------------------------------
            ["Social-Emotional Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Teach recognition of common emotions using pictures.",
                    "Use calm-down strategies such as breathing exercises.",
                    "Model appropriate social responses during interactions."
                },
                ["Moderate"] = new()
                {
                    "Use role-play to practice social situations (greeting, requesting).",
                    "Teach perspective-taking through structured games.",
                    "Use social stories for behaviour modulation."
                },
                ["Intensive"] = new()
                {
                    "Teach emotional regulation strategies during real-life conflicts.",
                    "Use structured peer-group intervention to build social maturity.",
                    "Implement behaviour-modification programs where required."
                }
            },

            // ------------------------------------------------------------------
            // 5. PERSONAL CARE & SAFETY SKILLS
            // ------------------------------------------------------------------
            ["Personal Care & Safety Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Provide stepwise visual cues for hygiene routines.",
                    "Practice dressing and grooming tasks independently.",
                    "Teach identifying safe vs unsafe household items."
                },
                ["Moderate"] = new()
                {
                    "Introduce basic community safety rules (crossing roads, signals).",
                    "Practice safe behaviour in public areas (shops, parks).",
                    "Support meal preparation tasks with supervision."
                },
                ["Intensive"] = new()
                {
                    "Teach emergency responses (fire, injury, contacting help).",
                    "Work on independent travel training under supervision.",
                    "Implement structured safety training across environments."
                }
            },

            // ------------------------------------------------------------------
            // 6. WORK-RELATED FUNCTIONAL ACADEMICS
            // ------------------------------------------------------------------
            ["Work-Related Functional Academic Skills"] = new()
            {
                ["Basic"] = new()
                {
                    "Practice number recognition and simple counting.",
                    "Teach reading of basic symbols, labels, and signs.",
                    "Introduce simple time concepts (morning/evening)."
                },
                ["Moderate"] = new()
                {
                    "Teach money-handling using real or simulated currency.",
                    "Practice reading workplace forms and writing simple entries.",
                    "Introduce measurement activities (weight, quantity)."
                },
                ["Intensive"] = new()
                {
                    "Provide simulated work tasks involving calculations.",
                    "Teach time management using task schedules and timers.",
                    "Practice literacy tasks required in job environments."
                }
            },

            // ------------------------------------------------------------------
            // 7. SEX EDUCATION
            // ------------------------------------------------------------------
            ["Sex Education"] = new()
            {
                ["Basic"] = new()
                {
                    "Teach body privacy rules and boundaries.",
                    "Introduce the concept of public vs private behaviour.",
                    "Explain safe vs unsafe touch through simple examples."
                },
                ["Moderate"] = new()
                {
                    "Teach consent and communication of discomfort.",
                    "Discuss puberty-related changes using age-appropriate materials.",
                    "Reinforce how to seek help from trusted adults."
                },
                ["Intensive"] = new()
                {
                    "Teach real-life decision making in unsafe situations.",
                    "Use structured role-play on reporting inappropriate behaviour.",
                    "Provide personalised safety-response training."
                }
            },

            // ------------------------------------------------------------------
            // 8. SELF-ADVOCACY
            // ------------------------------------------------------------------
            ["Self-Advocacy"] = new()
            {
                ["Basic"] = new()
                {
                    "Encourage expressing choices and preferences.",
                    "Teach saying 'no' when uncomfortable.",
                    "Support identifying personal likes and dislikes."
                },
                ["Moderate"] = new()
                {
                    "Teach rights and responsibilities in daily life.",
                    "Practice asking for help and clarification.",
                    "Encourage participation in personal goal-setting."
                },
                ["Intensive"] = new()
                {
                    "Use structured self-advocacy programs for independence.",
                    "Teach communication of needs in workplace-like situations.",
                    "Support the candidate in evaluating own progress and goals."
                }
            }
        };
}
