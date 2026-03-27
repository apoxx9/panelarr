using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.Books
{
    public interface IAddBookService
    {
        Issue AddBook(Issue issue, bool doRefresh = true);
        List<Issue> AddBooks(List<Issue> issues, bool doRefresh = true);
    }

    public class AddBookService : IAddBookService
    {
        private readonly ISeriesService _authorService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IBookService _bookService;
        private readonly IProvideBookInfo _bookInfo;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public AddBookService(ISeriesService authorService,
                               IAddSeriesService addSeriesService,
                               IBookService bookService,
                               IProvideBookInfo bookInfo,
                               IImportListExclusionService importListExclusionService,
                               Logger logger)
        {
            _authorService = authorService;
            _addSeriesService = addSeriesService;
            _bookService = bookService;
            _bookInfo = bookInfo;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        public Issue AddBook(Issue issue, bool doRefresh = true)
        {
            _logger.Debug($"Adding issue {issue}");

            issue = AddSkyhookData(issue);

            // Check if the issue already exists
            var dbBook = _bookService.FindById(issue.ForeignIssueId);
            if (dbBook != null)
            {
                issue.UseDbFieldsFrom(dbBook);
            }

            // Remove any import list exclusions preventing addition
            _importListExclusionService.Delete(issue.ForeignIssueId);
            _importListExclusionService.Delete(issue.SeriesMetadata.Value.ForeignSeriesId);

            // Note it's a manual addition so it's not deleted on next refresh
            issue.AddOptions.AddType = IssueAddType.Manual;

            // Add the author if necessary
            var dbSeries = _authorService.FindById(issue.SeriesMetadata.Value.ForeignSeriesId);
            if (dbSeries == null)
            {
                var author = issue.Series.Value;

                author.Metadata.Value.ForeignSeriesId = issue.SeriesMetadata.Value.ForeignSeriesId;

                dbSeries = _addSeriesService.AddSeries(author, false);
            }

            issue.Series = dbSeries;
            issue.SeriesMetadataId = dbSeries.SeriesMetadataId;
            _bookService.AddBook(issue, doRefresh);

            return issue;
        }

        public List<Issue> AddBooks(List<Issue> issues, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var addedBooks = new List<Issue>();

            foreach (var a in issues)
            {
                a.Added = added;
                try
                {
                    addedBooks.Add(AddBook(a, doRefresh));
                }
                catch (Exception ex)
                {
                    // Could be a bad id from an import list
                    _logger.Error(ex, "Failed to import id: {0} - {1}", a.ForeignIssueId, a.Title);
                }
            }

            return addedBooks;
        }

        private Issue AddSkyhookData(Issue newBook)
        {
            Tuple<string, Issue, List<SeriesMetadata>> tuple = null;
            try
            {
                tuple = _bookInfo.GetBookInfo(newBook.ForeignIssueId);
            }
            catch (IssueNotFoundException)
            {
                _logger.Error("Issue with Foreign Id {0} was not found, it may have been removed from metadata.", newBook.ForeignIssueId);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("ForeignIssueId", "A issue with this ID was not found", newBook.ForeignIssueId)
                                              });
            }

            newBook.UseMetadataFrom(tuple.Item2);
            newBook.Added = DateTime.UtcNow;

            var metadata = tuple.Item3.FirstOrDefault(x => x.ForeignSeriesId == tuple.Item1);
            newBook.SeriesMetadata = metadata;

            return newBook;
        }
    }
}
