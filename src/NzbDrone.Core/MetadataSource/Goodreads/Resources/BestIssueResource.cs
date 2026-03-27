using System.Diagnostics;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models the best issue in a work, as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class BestBookResource : GoodreadsResource
    {
        public override string ElementName => "best_book";

        /// <summary>
        /// The Id of this issue.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The title of this issue.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// The Goodreads id of the author.
        /// </summary>
        public long SeriesId { get; private set; }

        /// <summary>
        /// The name of the author.
        /// </summary>
        public string SeriesName { get; private set; }

        /// <summary>
        /// The cover image of this issue.
        /// </summary>
        public string ImageUrl { get; private set; }

        public string LargeImageUrl { get; private set; }

        public override void Parse(XElement element)
        {
            Id = element.ElementAsLong("id");
            Title = element.ElementAsString("title");

            var authorElement = element.Element("author");
            if (authorElement != null)
            {
                SeriesId = authorElement.ElementAsLong("id");
                SeriesName = authorElement.ElementAsString("name");
            }

            ImageUrl = element.ElementAsString("image_url");
            LargeImageUrl = element.ElementAsString("large_image_url");
        }
    }
}
