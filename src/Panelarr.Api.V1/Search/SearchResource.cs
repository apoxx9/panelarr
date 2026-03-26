using Panelarr.Api.V1.Author;
using Panelarr.Api.V1.Books;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Search
{
    public class SearchResource : RestResource
    {
        public string ForeignId { get; set; }
        public AuthorResource Author { get; set; }
        public BookResource Book { get; set; }
    }
}
