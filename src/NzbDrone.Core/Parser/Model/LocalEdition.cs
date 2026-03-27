using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEdition
    {
        public LocalEdition()
        {
            LocalBooks = new List<LocalBook>();

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public LocalEdition(List<LocalBook> tracks)
        {
            LocalBooks = tracks;

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public List<LocalBook> LocalBooks { get; set; }
        public int TrackCount => LocalBooks.Count;

        public Distance Distance { get; set; }
        public Issue Issue { get; set; }
        public List<LocalBook> ExistingTracks { get; set; }
        public bool NewDownload { get; set; }

        public void PopulateMatch(bool keepAllEditions)
        {
            if (Issue != null)
            {
                LocalBooks = LocalBooks.Concat(ExistingTracks).DistinctBy(x => x.Path).ToList();

                if (!keepAllEditions)
                {
                    // Manually clone the issue to avoid holding references to every issue seen during matching
                    var fullBook = Issue;

                    var issue = new Issue();
                    issue.UseMetadataFrom(fullBook);
                    issue.UseDbFieldsFrom(fullBook);
                    issue.Series.Value.UseMetadataFrom(fullBook.Series.Value);
                    issue.Series.Value.UseDbFieldsFrom(fullBook.Series.Value);
                    issue.Series.Value.Metadata = fullBook.SeriesMetadata.Value;
                    issue.SeriesMetadata = fullBook.SeriesMetadata.Value;
                    issue.ComicFiles = fullBook.ComicFiles;

                    if (fullBook.SeriesLinks.IsLoaded)
                    {
                        issue.SeriesLinks = fullBook.SeriesLinks.Value.Select(l => new SeriesGroupLink
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
                        issue.SeriesLinks = fullBook.SeriesLinks;
                    }

                    Issue = issue;

                    foreach (var localTrack in LocalBooks)
                    {
                        localTrack.Issue = issue;
                        localTrack.Series = issue.Series.Value;
                        localTrack.PartCount = LocalBooks.Count;
                    }
                }
                else
                {
                    foreach (var localTrack in LocalBooks)
                    {
                        localTrack.Issue = Issue;
                        localTrack.Series = Issue.Series.Value;
                        localTrack.PartCount = LocalBooks.Count;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", LocalBooks.Select(x => Path.GetDirectoryName(x.Path)).Distinct()) + "]";
        }
    }
}
