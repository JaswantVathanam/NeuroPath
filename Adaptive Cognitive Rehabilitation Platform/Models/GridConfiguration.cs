using System;

namespace NeuroPath.Models
{
    /// <summary>
    /// Represents grid configuration returned by AI for adaptive difficulty
    /// This is what the AI sends to adjust game difficulty
    /// </summary>
    public class GridConfiguration
    {
        // Grid Dimensions
        public int GridColumns { get; set; } = 3;
        public int GridRows { get; set; } = 4;
        public int TotalPairs { get; set; }
        
        // Animation & Timing
        public int FlipAnimationMs { get; set; } = 400; // Milliseconds for card flip
        public int? TimeLimitSeconds { get; set; } // Null = no time limit (practice)
        
        // Visual Complexity
        public string VisualComplexity { get; set; } = "simple"; // "simple", "moderate", "complex"
        public string CardStyle { get; set; } = "colors"; // "colors", "animals", "patterns", "geometric"
        public string BackgroundTheme { get; set; } = "light"; // "light", "dark", "colorful"
        
        // Cognitive Load
        public string PatternType { get; set; } = "direct"; // "direct", "themed", "symbolic", "rotation"
        
        // Difficulty & Progression
        public int DifficultyLevel { get; set; } = 1; // 1-5
        public bool ShouldLevelUp { get; set; } = false;
        public bool ShouldLevelDown { get; set; } = false;
        
        // AI Decision Info
        public string AdjustmentReason { get; set; } // Why this adjustment was made
        public decimal ConfidenceScore { get; set; } // 0-1, how confident AI is in this adjustment
        
        public override string ToString()
        {
            return $"Grid {GridColumns}x{GridRows} ({TotalPairs} pairs), " +
                   $"Level {DifficultyLevel}, " +
                   $"Animation {FlipAnimationMs}ms, " +
                   $"Complexity: {VisualComplexity}";
        }
    }
}
