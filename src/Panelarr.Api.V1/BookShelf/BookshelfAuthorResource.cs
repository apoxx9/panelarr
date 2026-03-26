using System.Collections.Generic;
using Panelarr.Api.V1.Books;

namespace Panelarr.Api.V1.Bookshelf
{
    public class BookshelfAuthorResource
    {
        public int Id { get; set; }
        public bool? Monitored { get; set; }
        public List<BookResource> Books { get; set; }
    }
}
