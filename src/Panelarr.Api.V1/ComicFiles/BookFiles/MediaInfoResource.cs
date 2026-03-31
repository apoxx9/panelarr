using NzbDrone.Core.Parser.Model;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.ComicFiles
{
    public class MediaInfoResource : RestResource
    {
    }

    public static class MediaInfoResourceMapper
    {
        public static MediaInfoResource ToResource(this MediaInfoModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new MediaInfoResource();
        }
    }
}
