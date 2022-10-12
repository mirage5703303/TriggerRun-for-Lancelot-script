using System;
using System.Globalization;
using System.Text;

namespace IEdgeGames {

    public static class StringExtensions {

        /// <summary>
        /// Get string value between [first] a and [last] b.
        /// </summary>
        public static string Between(this string value, string a, string b) {
            var posA = value.IndexOf(a);
            var posB = value.LastIndexOf(b);

            if (posA == -1 || posB == -1)
                return string.Empty;

            var adjustedPosA = posA + a.Length;

            return adjustedPosA < posB ? value.Substring(adjustedPosA, posB - adjustedPosA) : string.Empty;
        }

        /// <summary>
        /// Get string value after [first] a.
        /// </summary>
        public static string Before(this string value, string a) {
            var posA = value.IndexOf(a);
            return posA != -1 ? value.Substring(0, posA) : string.Empty;
        }

        /// <summary>
        /// Get string value after [last] a.
        /// </summary>
        public static string After(this string value, string a) {
            var posA = value.LastIndexOf(a);

            if (posA == -1)
                return string.Empty;

            var adjustedPosA = posA + a.Length;

            if (adjustedPosA >= value.Length)
                return string.Empty;

            return value.Substring(adjustedPosA);
        }

        public static string RemoveDiacritics(this string text) {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString) {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string Reverse(this string s) {
            var arr = s.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        private static int ComputeLevenshteinDistance(string s, string t) {
            var n = s.Length;
            var m = t.Length;
            var distance = new int[n + 1, m + 1]; // matrix
            var cost = 0;

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            //init1
            for (var i = 0; i <= n; distance[i, 0] = i++)
                ;
            for (var j = 0; j <= m; distance[0, j] = j++)
                ;

            //find min distance
            for (var i = 1; i <= n; i++)
                for (var j = 1; j <= m; j++) {
                    cost = (t.Substring(j - 1, 1) == s.Substring(i - 1, 1) ? 0 : 1);
                    distance[i, j] = Min3(distance[i - 1, j] + 1,
                                          distance[i, j - 1] + 1,
                                          distance[i - 1, j - 1] + cost);
                }

            return distance[n, m];
        }

        private static int Min3(int a, int b, int c)
            => Math.Min(Math.Min(a, b), c);

        public static float GetSimilarity(string string1, string string2) {
            var dis = ComputeLevenshteinDistance(string1, string2);
            var maxLen = string1.Length;

            if (maxLen < string2.Length)
                maxLen = string2.Length;

            if (maxLen == 0)
                return 1;
            else
                return 1 - dis / maxLen;
        }
    }
}
