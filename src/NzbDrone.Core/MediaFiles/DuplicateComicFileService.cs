using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public class DuplicateGroup
    {
        public int IssueId { get; set; }
        public List<ComicFile> Files { get; set; }
        public ComicFile Preferred { get; set; }
    }

    public interface IDuplicateComicFileService
    {
        List<DuplicateGroup> GetAllDuplicates();
        void AutoResolve(int issueId);
        void Resolve(int issueId, int keepComicFileId);
    }

    public class DuplicateComicFileService : IDuplicateComicFileService, IExecute<AutoResolveDuplicatesCommand>
    {
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly IMediaFileService _mediaFileService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public DuplicateComicFileService(IMediaFileRepository mediaFileRepository,
                                         IMediaFileService mediaFileService,
                                         IRecycleBinProvider recycleBinProvider,
                                         IDiskProvider diskProvider,
                                         Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _mediaFileService = mediaFileService;
            _recycleBinProvider = recycleBinProvider;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<DuplicateGroup> GetAllDuplicates()
        {
            // Fetch all mapped comic files, group by IssueId, keep only groups with > 1 file.
            var allFiles = _mediaFileRepository.All()
                .Where(f => f.IssueId > 0)
                .GroupBy(f => f.IssueId)
                .Where(g => g.Count() > 1)
                .Select(g =>
                {
                    var files = g.ToList();
                    return new DuplicateGroup
                    {
                        IssueId = g.Key,
                        Files = files,
                        Preferred = SelectPreferred(files)
                    };
                })
                .ToList();

            return allFiles;
        }

        public void AutoResolve(int issueId)
        {
            var files = _mediaFileService.GetFilesByIssue(issueId);
            if (files.Count <= 1)
            {
                _logger.Debug("DuplicateComicFileService.AutoResolve: no duplicates for issueId {0}", issueId);
                return;
            }

            var preferred = SelectPreferred(files);
            _logger.Info(
                "DuplicateComicFileService.AutoResolve: keeping {0}, deleting {1} other file(s) for issueId {2}",
                preferred.Path,
                files.Count - 1,
                issueId);

            var toDelete = files.Where(f => f.Id != preferred.Id).ToList();
            DeleteFiles(toDelete);
        }

        public void Resolve(int issueId, int keepComicFileId)
        {
            var files = _mediaFileService.GetFilesByIssue(issueId);
            if (files.Count <= 1)
            {
                _logger.Debug("DuplicateComicFileService.Resolve: no duplicates for issueId {0}", issueId);
                return;
            }

            var keepFile = files.FirstOrDefault(f => f.Id == keepComicFileId);
            if (keepFile == null)
            {
                _logger.Warn("DuplicateComicFileService.Resolve: keepComicFileId {0} not found for issueId {1}", keepComicFileId, issueId);
                return;
            }

            _logger.Info(
                "DuplicateComicFileService.Resolve: keeping {0}, deleting {1} other file(s) for issueId {2}",
                keepFile.Path,
                files.Count - 1,
                issueId);

            var toDelete = files.Where(f => f.Id != keepComicFileId).ToList();
            DeleteFiles(toDelete);
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private void DeleteFiles(List<ComicFile> files)
        {
            foreach (var file in files)
            {
                _logger.Info("DuplicateComicFileService: removing duplicate file {0}", file.Path);

                if (_diskProvider.FileExists(file.Path))
                {
                    _recycleBinProvider.DeleteFile(file.Path);
                }

                _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }
        }

        /// <summary>
        /// Preferred = highest ImageQualityScore, tie-break by QualityWeight descending, then size descending.
        /// </summary>
        internal static ComicFile SelectPreferred(List<ComicFile> files)
        {
            return files
                .OrderByDescending(f => f.ImageQualityScore)
                .ThenByDescending(f => QualityWeightOf(f))
                .ThenByDescending(f => f.Size)
                .First();
        }

        private static int QualityWeightOf(ComicFile f)
        {
            if (f.Quality == null)
            {
                return 0;
            }

            var def = Quality.DefaultQualityDefinitions.FirstOrDefault(q => q.Quality == f.Quality.Quality);
            if (def == null)
            {
                return 0;
            }

            var weight = def.Weight;
            weight += f.Quality.Revision.Real * 10;
            weight += f.Quality.Revision.Version;
            return weight;
        }

        // ── IExecute<AutoResolveDuplicatesCommand> ───────────────────────────
        public void Execute(AutoResolveDuplicatesCommand message)
        {
            var duplicates = GetAllDuplicates();
            _logger.Info("AutoResolveDuplicates: found {0} issue(s) with duplicate files", duplicates.Count);

            foreach (var group in duplicates)
            {
                AutoResolve(group.IssueId);
            }
        }
    }
}
