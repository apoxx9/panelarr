using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEdition
    {
        public LocalEdition()
        {
            LocalIssues = new List<LocalIssue>();

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public LocalEdition(List<LocalIssue> tracks)
        {
            LocalIssues = tracks;

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public List<LocalIssue> LocalIssues { get; set; }
        public int TrackCount => LocalIssues.Count;

        public Distance Distance { get; set; }
        public Issue Issue { get; set; }
        public List<LocalIssue> ExistingTracks { get; set; }
        public bool NewDownload { get; set; }

        public void PopulateMatch(bool keepAllEditions)
        {
            if (Issue != null)
            {
                LocalIssues = LocalIssues.Concat(ExistingTracks).DistinctBy(x => x.Path).ToList();

                if (!keepAllEditions)
                {
                    // Manually clone the issue to avoid holding references to every issue seen during matching
                    var fullIssue = Issue;

                    var issue = new Issue();
                    issue.UseMetadataFrom(fullIssue);
                    issue.UseDbFieldsFrom(fullIssue);
                    issue.Series.Value.UseMetadataFrom(fullIssue.Series.Value);
                    issue.Series.Value.UseDbFieldsFrom(fullIssue.Series.Value);
                    issue.Series.Value.Metadata = fullIssue.SeriesMetadata.Value;
                    issue.SeriesMetadata = fullIssue.SeriesMetadata.Value;
                    issue.ComicFiles = fullIssue.ComicFiles;

                    if (fullIssue.SeriesLinks.IsLoaded)
                    {
                        issue.SeriesLinks = fullIssue.SeriesLinks.Value.Select(l => new SeriesGroupLink
                        {
                            Issue = issue,
                            SeriesGroup = new SeriesGroup
                            {
                                ForeignSeriesGroupId = l.SeriesGroup.Value.ForeignSeriesGroupId,
                                Title = l.SeriesGroup.Value.Title,
                                Description = l.SeriesGroup.Value.Description,
                                Numbered = l.SeriesGroup.Value.Numbered,
                                WorkCount = l.SeriesGroup.Value.WorkCount,
                                PrimaryWorkCount = l.SeriesGroup.Value.PrimaryWorkCount
                            },
                            IsPrimary = l.IsPrimary,
                            Position = l.Position,
                            SeriesPosition = l.SeriesPosition
                        }).ToList();
                    }
                    else
                    {
                        issue.SeriesLinks = fullIssue.SeriesLinks;
                    }

                    Issue = issue;

                    foreach (var localTrack in LocalIssues)
                    {
                        localTrack.Issue = issue;
                        localTrack.Series = issue.Series.Value;
                        localTrack.PartCount = LocalIssues.Count;
                    }
                }
                else
                {
                    foreach (var localTrack in LocalIssues)
                    {
                        localTrack.Issue = Issue;
                        localTrack.Series = Issue.Series.Value;
                        localTrack.PartCount = LocalIssues.Count;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", LocalIssues.Select(x => Path.GetDirectoryName(x.Path)).Distinct()) + "]";
        }
    }
}
