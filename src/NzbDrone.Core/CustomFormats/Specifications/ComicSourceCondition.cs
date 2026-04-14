namespace NzbDrone.Core.CustomFormats
{
    /// <summary>
    /// Matches comic releases by source tag parsed from release name.
    /// Common source tags: Digital, Scan, c2c, Hybrid, Retail, Rip.
    /// The Value field is a regex pattern that will be matched against:
    ///   1. ParsedIssueInfo.ReleaseTitle / Filename
    ///   2. CustomFormatInput.ComicSource (if pre-parsed)
    /// </summary>
    public class ComicSourceCondition : RegexSpecificationBase
    {
        public override int Order => 10;
        public override string ImplementationName => "Comic Source";

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            // Match against pre-parsed source tag
            if (MatchString(input.ComicSource))
            {
                return true;
            }

            // Fall back to matching against release title / filename
            return MatchString(input.IssueInfo?.ReleaseTitle) || MatchString(input.Filename);
        }
    }
}
