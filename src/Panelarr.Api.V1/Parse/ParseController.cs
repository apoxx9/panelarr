using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using Panelarr.Api.V1.Books;
using Panelarr.Api.V1.Series;
using Panelarr.Http;

namespace Panelarr.Api.V1.Parse
{
    [V1ApiController]
    public class ParseController : Controller
    {
        private readonly IParsingService _parsingService;

        public ParseController(IParsingService parsingService)
        {
            _parsingService = parsingService;
        }

        [HttpGet]
        public ParseResource Parse(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsedBookInfo = Parser.ParseBookTitle(title);

            if (parsedBookInfo == null)
            {
                return new ParseResource
                {
                    Title = title
                };
            }

            var remoteBook = _parsingService.Map(parsedBookInfo);

            if (remoteBook != null)
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedBookInfo = remoteBook.ParsedBookInfo,
                    Series = remoteBook.Series.ToResource(),
                    Books = remoteBook.Books.ToResource()
                };
            }
            else
            {
                return new ParseResource
                {
                    Title = title,
                    ParsedBookInfo = parsedBookInfo
                };
            }
        }
    }
}
