using System.Linq;
using NzbDrone.Core.Indexers.Newznab;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class TorznabRequestGenerator : NewznabRequestGenerator
    {
        public TorznabRequestGenerator(INewznabCapabilitiesProvider capabilitiesProvider)
        : base(capabilitiesProvider)
        {
        }

        protected override bool SupportsComicSearch
        {
            get
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                return capabilities.SupportedComicSearchParameters != null &&
                       capabilities.SupportedComicSearchParameters.Contains("q") &&
                       capabilities.SupportedComicSearchParameters.Contains("series") &&
                       capabilities.SupportedComicSearchParameters.Contains("title");
            }
        }
    }
}
