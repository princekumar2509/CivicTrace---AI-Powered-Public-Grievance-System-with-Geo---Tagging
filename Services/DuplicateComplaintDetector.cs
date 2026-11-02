using PublicGrievanceSystem.Models;

namespace PublicGrievanceSystem.Services
{
    public class DuplicateComplaintDetector
    {
        public List<DuplicateMatch> FindDuplicates(
            Complaint newComplaint,
            IEnumerable<Complaint> existingComplaints)
        {
            var matches = new List<DuplicateMatch>();

            foreach (var existing in existingComplaints)
            {
                // Resolved complaints are not considered duplicates.
                if (existing.Status == "Resolved")
                {
                    continue;
                }

                double distance = CalculateDistance(
                    newComplaint.Latitude,
                    newComplaint.Longitude,
                    existing.Latitude,
                    existing.Longitude);

                double textSimilarity = CalculateTextSimilarity(
                    $"{newComplaint.Title} {newComplaint.Description}",
                    $"{existing.Title} {existing.Description}");

                string? matchType = null;

                // High-confidence duplicate
                if (textSimilarity >= 0.75 && distance <= 300)
                {
                    matchType = "Likely Duplicate";
                }

                // Nearby complaint with moderate similarity
                else if (textSimilarity >= 0.40 && distance <= 100)
                {
                    matchType = "Possible Duplicate";
                }

                if (matchType != null)
                {
                    matches.Add(new DuplicateMatch
                    {
                        ComplaintId = existing.Id,
                        Title = existing.Title,
                        Status = existing.Status,
                        Department = existing.Department,
                        DistanceInMeters = distance,
                        Similarity = textSimilarity,
                        MatchType = matchType
                    });
                }
            }

            return matches
                .OrderByDescending(x => x.Similarity)
                .ThenBy(x => x.DistanceInMeters)
                .ToList();
        }

        private double CalculateTextSimilarity(
            string text1,
            string text2)
        {
            var words1 = GetWords(text1);
            var words2 = GetWords(text2);

            if (words1.Count == 0 || words2.Count == 0)
            {
                return 0;
            }

            var intersection =
                words1.Intersect(words2).Count();

            var union =
                words1.Union(words2).Count();

            if (union == 0)
            {
                return 0;
            }

            return (double)intersection / union;
        }

        private HashSet<string> GetWords(string text)
        {
            char[] separators =
            {
                ' ',
                '.',
                ',',
                '!',
                '?',
                ':',
                ';',
                '-',
                '_',
                '(',
                ')',
                '\n',
                '\r'
            };

            return text
                .ToLower()
                .Split(
                    separators,
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 2)
                .ToHashSet();
        }

        private double CalculateDistance(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            const double EarthRadius = 6371000;

            double lat1 =
                DegreesToRadians(latitude1);

            double lat2 =
                DegreesToRadians(latitude2);

            double deltaLat =
                DegreesToRadians(latitude2 - latitude1);

            double deltaLon =
                DegreesToRadians(longitude2 - longitude1);

            double a =
                Math.Sin(deltaLat / 2) *
                Math.Sin(deltaLat / 2)
                +
                Math.Cos(lat1) *
                Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) *
                Math.Sin(deltaLon / 2);

            double c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));

            return EarthRadius * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }

    public class DuplicateMatch
    {
        public int ComplaintId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? Department { get; set; }

        public double DistanceInMeters { get; set; }

        public double Similarity { get; set; }

        public string MatchType { get; set; } = "Possible Duplicate";
    }
}