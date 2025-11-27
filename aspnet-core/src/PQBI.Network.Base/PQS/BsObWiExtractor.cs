using System.Text.RegularExpressions;

namespace PQBI.PQS
{
    public static class BsObWiExtractor
    {
        private static readonly Regex BsObWiRegex =
            new(@"BS[^_]*_OB[^_]*(?:_WI[^_]*)?",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string Extract(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var match = BsObWiRegex.Match(input);
            return match.Success ? match.Value : null;
        }
    }
}
