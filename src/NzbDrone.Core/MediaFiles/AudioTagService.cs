using System.Collections.Generic;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IAudioTagService
    {
        ParsedTrackInfo ReadTags(string file);
        void WriteTags(ComicFile comicFile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId);
        List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId);
        void RetagFiles(RetagFilesCommand message);
        void RetagSeries(RetagSeriesCommand message);
    }

    public class AudioTagService : IAudioTagService
    {
        public ParsedTrackInfo ReadTags(string file)
        {
            return new ParsedTrackInfo();
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
        }

        public void SyncTags(List<Issue> issues)
        {
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId)
        {
            return new List<RetagComicFilePreview>();
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            return new List<RetagComicFilePreview>();
        }

        public void RetagFiles(RetagFilesCommand message)
        {
        }

        public void RetagSeries(RetagSeriesCommand message)
        {
        }
    }
}
