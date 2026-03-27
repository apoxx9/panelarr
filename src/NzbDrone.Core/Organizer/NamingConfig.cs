using System.IO;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameComics = false,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            StandardIssueFormat = "{Series Title} ({Series Year}) #{Issue Number:000}",
            AnnualIssueFormat = "{Series Title} ({Series Year}) Annual #{Issue Number:000}",
            TPBFormat = "{Series Title} ({Series Year}) Vol {Volume Number:00} TPB",
            SeriesFolderFormat = "{Publisher}" + Path.DirectorySeparatorChar + "{Series Title} ({Series Year})",
        };

        public bool RenameComics { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardIssueFormat { get; set; }
        public string AnnualIssueFormat { get; set; }
        public string TPBFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
    }
}
