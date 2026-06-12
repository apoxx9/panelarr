namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    public enum ProposalConfidence
    {
        // Backed by a provider id found in the folder's cvinfo or the files' tags
        Exact,

        // Best name-search hit; needs a deliberate tick on the review screen
        Probable
    }

    public class LibraryImportProposal
    {
        public string Folder { get; set; }
        public string ForeignSeriesId { get; set; }
        public string Name { get; set; }
        public int? Year { get; set; }
        public ProposalConfidence Confidence { get; set; }

        // Where the id came from: "cvinfo", "file tags" or "name search"
        public string IdSource { get; set; }
        public int FileCount { get; set; }

        // Set when the series is already in the library (not importable in v1)
        public int? ExistingSeriesId { get; set; }
    }
}
