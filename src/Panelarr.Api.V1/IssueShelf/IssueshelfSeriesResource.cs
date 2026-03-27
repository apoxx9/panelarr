using System.Collections.Generic;
using Panelarr.Api.V1.Books;

namespace Panelarr.Api.V1.Bookshelf
{
    public class IssueshelfSeriesResource
    {
        public int Id { get; set; }
        public bool? Monitored { get; set; }
        public List<IssueResource> Books { get; set; }
    }
}
