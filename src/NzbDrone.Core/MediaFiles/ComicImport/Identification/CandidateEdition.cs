using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public class CandidateEdition
    {
        public CandidateEdition()
        {
        }

        public CandidateEdition(Issue issue)
        {
            Issue = issue;
            ExistingFiles = new List<ComicFile>();
        }

        public Issue Issue { get; set; }
        public List<ComicFile> ExistingFiles { get; set; }

        /// <summary>
        /// This candidate is the series' only issue, offered because nothing
        /// matched by title or number - a collected edition's line volume
        /// number is not an issue index, so distance scoring must not compare
        /// them.
        /// </summary>
        public bool SoleIssueFallback { get; set; }
    }
}
