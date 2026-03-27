using System.IO;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Organizer
{
    public class NamingConfig : ModelBase
    {
        public static NamingConfig Default => new NamingConfig
        {
            RenameBooks = false,
            ReplaceIllegalCharacters = true,
            ColonReplacementFormat = ColonReplacementFormat.Smart,
            StandardBookFormat = "{Series Title} ({Series Year}) #{Issue Number:000}",
            AnnualIssueFormat = "{Series Title} ({Series Year}) Annual #{Issue Number:000}",
            TPBFormat = "{Series Title} ({Series Year}) Vol {Volume Number:00} TPB",
            SeriesFolderFormat = "{Publisher}" + Path.DirectorySeparatorChar + "{Series Title} ({Series Year})",
        };

        public bool RenameBooks { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardBookFormat { get; set; }
        public string AnnualIssueFormat { get; set; }
        public string TPBFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
    }
}
