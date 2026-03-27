using System;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models areas of the API where Goodreads returns
    /// information about an user owned issues.
    /// </summary>
    public sealed class OwnedBookResource : GoodreadsResource
    {
        public override string ElementName => "owned_book";

        /// <summary>
        /// The owner issue id.
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// The owner id.
        /// </summary>
        public long OwnerId { get; private set; }

        /// <summary>
        /// The original date when owner has bought a issue.
        /// </summary>
        public DateTime? OriginalPurchaseDate { get; private set; }

        /// <summary>
        /// The original location where owner has bought a issue.
        /// </summary>
        public string OriginalPurchaseLocation { get; private set; }

        /// <summary>
        /// The owned issue condition.
        /// </summary>
        public string Condition { get; private set; }

        /// <summary>
        /// The traded count.
        /// </summary>
        public int TradedCount { get; private set; }

        /// <summary>
        /// The link to the owned issue.
        /// </summary>
        public string Link { get; private set; }

        /// <summary>
        /// The issue.
        /// </summary>
        public IssueSummaryResource Issue { get; private set; }

        /// <summary>
        /// The owned issue review.
        /// </summary>
        public ReviewResource Review { get; private set; }

        public override void Parse(XElement element)
        {
            Id = element.ElementAsLong("id");
            OwnerId = element.ElementAsLong("current_owner_id");
            OriginalPurchaseDate = element.ElementAsDateTime("original_purchase_date");
            OriginalPurchaseLocation = element.ElementAsString("original_purchase_location");
            Condition = element.ElementAsString("condition");

            var review = element.Element("review");
            if (review != null)
            {
                Review = new ReviewResource();
                Review.Parse(review);
            }

            var issue = element.Element("issue");
            if (issue != null)
            {
                Issue = new IssueSummaryResource();
                Issue.Parse(issue);
            }
        }
    }
}
