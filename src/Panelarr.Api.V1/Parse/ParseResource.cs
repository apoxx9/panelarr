using System.Collections.Generic;
using NzbDrone.Core.Parser.Model;
using Panelarr.Api.V1.Author;
using Panelarr.Api.V1.Books;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Parse
{
    public class ParseResource : RestResource
    {
        public string Title { get; set; }
        public ParsedBookInfo ParsedBookInfo { get; set; }
        public AuthorResource Author { get; set; }
        public List<BookResource> Books { get; set; }
    }
}
