using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.IssueImport
{
    public interface IMakeImportDecision
    {
        List<ImportDecision<LocalIssue>> GetImportDecisions(List<IFileInfo> comicFiles, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config);
    }

    public class IdentificationOverrides
    {
        public Series Series { get; set; }
        public Issue Issue { get; set; }
    }

    public class ImportDecisionMakerInfo
    {
        public DownloadClientItem DownloadClientItem { get; set; }
        public ParsedIssueInfo ParsedIssueInfo { get; set; }
    }

    public class ImportDecisionMakerConfig
    {
        public FilterFilesType Filter { get; set; }
        public bool NewDownload { get; set; }
        public bool SingleRelease { get; set; }
        public bool IncludeExisting { get; set; }
        public bool AddNewSeries { get; set; }
        public bool KeepAllEditions { get; set; }
    }

    public class ImportDecisionMaker : IMakeImportDecision
    {
        private readonly IEnumerable<IImportDecisionEngineSpecification<LocalIssue>> _trackSpecifications;
        private readonly IEnumerable<IImportDecisionEngineSpecification<LocalEdition>> _issueSpecifications;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IAugmentingService _augmentingService;
        private readonly IIdentificationService _identificationService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly Logger _logger;

        public ImportDecisionMaker(IEnumerable<IImportDecisionEngineSpecification<LocalIssue>> trackSpecifications,
                                   IEnumerable<IImportDecisionEngineSpecification<LocalEdition>> issueSpecifications,
                                   IMediaFileService mediaFileService,
                                   IMetadataTagService metadataTagService,
                                   IAugmentingService augmentingService,
                                   IIdentificationService identificationService,
                                   IRootFolderService rootFolderService,
                                   IQualityProfileService qualityProfileService,
                                   Logger logger)
        {
            _trackSpecifications = trackSpecifications;
            _issueSpecifications = issueSpecifications;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _augmentingService = augmentingService;
            _identificationService = identificationService;
            _rootFolderService = rootFolderService;
            _qualityProfileService = qualityProfileService;
            _logger = logger;
        }

        public Tuple<List<LocalIssue>, List<ImportDecision<LocalIssue>>> GetLocalTracks(List<IFileInfo> comicFiles, DownloadClientItem downloadClientItem, ParsedIssueInfo folderInfo, FilterFilesType filter)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var files = _mediaFileService.FilterUnchangedFiles(comicFiles, filter);

            var localTracks = new List<LocalIssue>();
            var decisions = new List<ImportDecision<LocalIssue>>();

            _logger.Debug("Analyzing {0}/{1} files.", files.Count, comicFiles.Count);

            if (!files.Any())
            {
                return Tuple.Create(localTracks, decisions);
            }

            ParsedIssueInfo downloadClientItemInfo = null;

            if (downloadClientItem != null)
            {
                downloadClientItemInfo = Parser.Parser.ParseIssueTitle(downloadClientItem.Title);
            }

            var i = 1;
            foreach (var file in files)
            {
                _logger.ProgressInfo($"Reading file {i++}/{files.Count}");

                var fileTrackInfo = _metadataTagService.ReadTags(file);

                var localTrack = new LocalIssue
                {
                    DownloadClientIssueInfo = downloadClientItemInfo,
                    FolderTrackInfo = folderInfo,
                    Path = file.FullName,
                    Part = fileTrackInfo.TrackNumbers.Any() ? fileTrackInfo.TrackNumbers.First() : 1,
                    Size = file.Length,
                    Modified = file.LastWriteTimeUtc,
                    FileTrackInfo = fileTrackInfo,
                    AdditionalFile = false
                };

                try
                {
                    // TODO fix otherfiles?
                    _augmentingService.Augment(localTrack, true);
                    localTracks.Add(localTrack);
                }
                catch (AugmentingFailedException)
                {
                    decisions.Add(new ImportDecision<LocalIssue>(localTrack, new Rejection("Unable to parse file")));
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't import file. {0}", localTrack.Path);

                    decisions.Add(new ImportDecision<LocalIssue>(localTrack, new Rejection("Unexpected error processing file")));
                }
            }

            _logger.Debug($"Tags parsed for {files.Count} files in {watch.ElapsedMilliseconds}ms");

            return Tuple.Create(localTracks, decisions);
        }

        public List<ImportDecision<LocalIssue>> GetImportDecisions(List<IFileInfo> comicFiles, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config)
        {
            idOverrides = idOverrides ?? new IdentificationOverrides();
            itemInfo = itemInfo ?? new ImportDecisionMakerInfo();

            var localIssueData = GetLocalTracks(comicFiles, itemInfo.DownloadClientItem, itemInfo.ParsedIssueInfo, config.Filter);
            var localTracks = localIssueData.Item1;
            var decisions = localIssueData.Item2;

            localTracks.ForEach(x => x.ExistingFile = !config.NewDownload);

            var releases = _identificationService.Identify(localTracks, idOverrides, config);

            foreach (var release in releases)
            {
                // make sure the appropriate quality profile is set for the release series
                // in case it's a new series
                EnsureData(release);
                release.NewDownload = config.NewDownload;

                var releaseDecision = GetDecision(release, itemInfo.DownloadClientItem);

                foreach (var localTrack in release.LocalIssues)
                {
                    if (releaseDecision.Approved)
                    {
                        decisions.AddIfNotNull(GetDecision(localTrack, itemInfo.DownloadClientItem));
                    }
                    else
                    {
                        decisions.Add(new ImportDecision<LocalIssue>(localTrack, releaseDecision.Rejections.ToArray()));
                    }
                }
            }

            return decisions;
        }

        private void EnsureData(LocalEdition edition)
        {
            if (edition.Issue != null && edition.Issue.Series.Value.QualityProfileId == 0)
            {
                var rootFolder = _rootFolderService.GetBestRootFolder(edition.LocalIssues.First().Path);
                var qualityProfile = _qualityProfileService.Get(rootFolder.DefaultQualityProfileId);

                var series = edition.Issue.Series.Value;
                series.QualityProfileId = qualityProfile.Id;
                series.QualityProfile = qualityProfile;
            }
        }

        private ImportDecision<LocalEdition> GetDecision(LocalEdition localEdition, DownloadClientItem downloadClientItem)
        {
            ImportDecision<LocalEdition> decision = null;

            if (localEdition.Issue == null)
            {
                decision = new ImportDecision<LocalEdition>(localEdition, new Rejection($"Couldn't find similar issue for {localEdition}"));
            }
            else
            {
                var reasons = _issueSpecifications.Select(c => EvaluateSpec(c, localEdition, downloadClientItem))
                    .Where(c => c != null);

                decision = new ImportDecision<LocalEdition>(localEdition, reasons.ToArray());
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", localEdition);
            }
            else if (decision.Rejections.Any())
            {
                _logger.Debug("Issue rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
            }
            else
            {
                _logger.Debug("Issue accepted");
            }

            return decision;
        }

        private ImportDecision<LocalIssue> GetDecision(LocalIssue localIssue, DownloadClientItem downloadClientItem)
        {
            ImportDecision<LocalIssue> decision = null;

            if (localIssue.Issue == null)
            {
                decision = new ImportDecision<LocalIssue>(localIssue, new Rejection($"Couldn't parse issue from: {localIssue.FileTrackInfo}"));
            }
            else
            {
                var reasons = _trackSpecifications.Select(c => EvaluateSpec(c, localIssue, downloadClientItem))
                    .Where(c => c != null);

                decision = new ImportDecision<LocalIssue>(localIssue, reasons.ToArray());
            }

            if (decision == null)
            {
                _logger.Error("Unable to make a decision on {0}", localIssue.Path);
            }
            else if (decision.Rejections.Any())
            {
                _logger.Debug("File rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
            }
            else
            {
                _logger.Debug("File accepted");
            }

            return decision;
        }

        private Rejection EvaluateSpec<T>(IImportDecisionEngineSpecification<T> spec, T item, DownloadClientItem downloadClientItem)
        {
            try
            {
                var result = spec.IsSatisfiedBy(item, downloadClientItem);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Couldn't evaluate decision on {0}", item);
                return new Rejection($"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }
    }
}
