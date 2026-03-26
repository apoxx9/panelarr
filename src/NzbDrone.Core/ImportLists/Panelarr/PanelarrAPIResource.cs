using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Panelarr
{
    public class PanelarrAuthor
    {
        public string AuthorName { get; set; }
        public int Id { get; set; }
        public string ForeignAuthorId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class PanelarrEdition
    {
        public string Title { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
    }

    public class PanelarrBook
    {
        public string Title { get; set; }
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public PanelarrAuthor Author { get; set; }
        public int AuthorId { get; set; }
        public List<PanelarrEdition> Editions { get; set; }
    }

    public class PanelarrProfile
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class PanelarrTag
    {
        public string Label { get; set; }
        public int Id { get; set; }
    }

    public class PanelarrRootFolder
    {
        public string Path { get; set; }
        public int Id { get; set; }
    }
}
