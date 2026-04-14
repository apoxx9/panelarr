using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsIndexer : HttpIndexerBase<GetComicsSettings>
    {
        public override string Name => "GetComics";

        public override DownloadProtocol Protocol => DownloadProtocol.DirectDownload;

        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public GetComicsIndexer(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GetComicsRequestGenerator { Settings = Settings };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GetComicsParser();
        }
    }
}
