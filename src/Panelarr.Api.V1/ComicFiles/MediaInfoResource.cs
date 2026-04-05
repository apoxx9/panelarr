using NzbDrone.Core.Parser.Model;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.ComicFiles
{
    public class MediaInfoResource : RestResource
    {
        public string AudioFormat { get; set; }
        public int AudioBitrate { get; set; }
        public int AudioChannels { get; set; }
        public int AudioBits { get; set; }
        public int AudioSampleRate { get; set; }
    }

    public static class MediaInfoResourceMapper
    {
        public static MediaInfoResource ToResource(this MediaInfoModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new MediaInfoResource
            {
                AudioFormat = model.AudioFormat,
                AudioBitrate = model.AudioBitrate,
                AudioChannels = model.AudioChannels,
                AudioBits = model.AudioBits,
                AudioSampleRate = model.AudioSampleRate
            };
        }
    }
}
