namespace PublicGrievanceSystem.Services
{
    public class AISeverityPredictor
    {
        public string PredictSeverity(
            string title,
            string description,
            string category)
        {
            string text =
                $"{title} {description} {category}"
                .ToLower();

            // ==========================================
            // CRITICAL SEVERITY
            // ==========================================

            string[] criticalKeywords =
            {
                "accident",
                "fire",
                "flood",
                "life threatening",
                "life-threatening",
                "dangerous",
                "emergency",
                "electrocution",
                "electric shock",
                "major leakage",
                "severe flooding"
            };

            if (criticalKeywords.Any(text.Contains))
            {
                return "Critical";
            }


            // ==========================================
            // HIGH SEVERITY
            // ==========================================

            string[] highKeywords =
            {
                "severe",
                "serious",
                "major",
                "huge pothole",
                "large pothole",
                "road collapse",
                "broken road",
                "water leakage",
                "sewage overflow",
                "drain overflow",
                "open manhole",
                "transformer",
                "power failure",
                "electricity failure",
                "no electricity"
            };

            if (highKeywords.Any(text.Contains))
            {
                return "High";
            }


            // ==========================================
            // CATEGORY-BASED SEVERITY
            // ==========================================

            if (category == "Electricity")
            {
                return "High";
            }

            if (category == "Water")
            {
                return "High";
            }

            if (category == "Drainage")
            {
                return "High";
            }


            // ==========================================
            // MEDIUM SEVERITY
            // ==========================================

            string[] mediumKeywords =
            {
                "not working",
                "broken",
                "damaged",
                "blocked",
                "dirty",
                "waste",
                "garbage",
                "streetlight",
                "street light",
                "pothole"
            };

            if (mediumKeywords.Any(text.Contains))
            {
                return "Medium";
            }


            // ==========================================
            // DEFAULT
            // ==========================================

            return "Low";
        }
    }
}