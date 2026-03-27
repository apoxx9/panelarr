using System.Diagnostics;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models the best issue in a work, as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ShowSeriesResource : GoodreadsResource
    {
        public override string ElementName => "series";

        public SeriesResource SeriesGroup { get; private set; }

        public override void Parse(XElement element)
        {
            SeriesGroup = new SeriesResource();
            SeriesGroup.Parse(element);

            SeriesGroup.Works = element.ParseChildren<WorkResource>("series_works", "series_work");
        }
    }
}
