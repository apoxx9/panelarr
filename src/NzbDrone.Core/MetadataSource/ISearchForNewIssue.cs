using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewBook
    {
        List<Issue> SearchForNewBook(string title, string author, bool getAllEditions = true);
        List<Issue> SearchByIsbn(string isbn);
        List<Issue> SearchByAsin(string asin);
        List<Issue> SearchByGoodreadsBookId(int goodreadsId, bool getAllEditions);
    }
}
