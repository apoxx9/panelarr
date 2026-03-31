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
    }
}
