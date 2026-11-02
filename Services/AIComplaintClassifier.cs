namespace PublicGrievanceSystem.Services
{
    public class AIComplaintClassifier
    {
        public ComplaintClassificationResult Classify(
            string title,
            string description)
        {
            string text =
                $"{title} {description}".ToLower();

            string category = "General";
            string department = "Public Works";
            string priority = "Medium";

            // Road / Pothole
            if (ContainsAny(text,
                "pothole",
                "road damage",
                "damaged road",
                "broken road",
                "road",
                "street",
                "footpath",
                "sidewalk"))
            {
                category = "Road";
                department = "Road & Transport";
                priority = "High";
            }

            // Garbage / Waste
            else if (ContainsAny(text,
                "garbage",
                "waste",
                "trash",
                "dustbin",
                "dirty",
                "rubbish"))
            {
                category = "Garbage";
                department = "Sanitation";
                priority = "Medium";
            }

            // Streetlight
            else if (ContainsAny(text,
                "streetlight",
                "street light",
                "street lamp",
                "lamp post",
                "light not working",
                "broken light"))
            {
                category = "Streetlight";
                department = "Electrical";
                priority = "Medium";
            }

            // Electricity
            else if (ContainsAny(text,
                "electricity",
                "power cut",
                "power outage",
                "transformer",
                "electric pole",
                "wire",
                "electric wire"))
            {
                category = "Electricity";
                department = "Electricity Department";
                priority = "High";
            }

            // Water
            else if (ContainsAny(text,
                "water leakage",
                "water leak",
                "water supply",
                "pipeline",
                "water pipe",
                "no water"))
            {
                category = "Water";
                department = "Water Supply";
                priority = "High";
            }

            // Drainage / Sewage
            else if (ContainsAny(text,
                "drainage",
                "drain",
                "sewage",
                "sewer",
                "blocked drain",
                "overflow"))
            {
                category = "Drainage";
                department = "Public Works";
                priority = "High";
            }

            // Emergency keywords
            if (ContainsAny(text,
                "danger",
                "dangerous",
                "accident",
                "emergency",
                "flood",
                "fire"))
            {
                priority = "High";
            }

            return new ComplaintClassificationResult
            {
                Category = category,
                Department = department,
                Priority = priority
            };
        }

        private bool ContainsAny(
            string text,
            params string[] keywords)
        {
            return keywords.Any(keyword =>
                text.Contains(keyword));
        }
    }

    public class ComplaintClassificationResult
    {
        public string Category { get; set; } = "General";

        public string Department { get; set; } = "Public Works";

        public string Priority { get; set; } = "Medium";
    }
}
