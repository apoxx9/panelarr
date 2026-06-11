namespace NzbDrone.Core.CustomFormats
{
    /// <summary>
    /// Matches comic releases by source tag in the release name or filename.
    /// Common source tags: Digital, Scan, c2c, Hybrid, Retail, Rip.
    /// The Value field is a regex pattern.
    /// </summary>
    public class ComicSourceCondition : RegexSpecificationBase
    {
        public override int Order => 10;
        public override string ImplementationName => "Comic Source";

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            return MatchString(input.IssueInfo?.ReleaseTitle) || MatchString(input.Filename);
        }
    }
}
