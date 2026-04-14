using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaInfoFormatter
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(MediaInfoFormatter));

        public static string FormatAudioBitrate(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioBitrate + " kbps";
        }

        public static string FormatAudioBitsPerSample(MediaInfoModel mediaInfo)
        {
            if (mediaInfo.AudioBits == 0)
            {
                return string.Empty;
            }

            return mediaInfo.AudioBits + "bit";
        }

        public static string FormatAudioSampleRate(MediaInfoModel mediaInfo)
        {
            return $"{(double)mediaInfo.AudioSampleRate / 1000:0.#}kHz";
        }

        public static decimal FormatAudioChannels(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioChannels;
        }

        public static readonly Dictionary<Codec, string> CodecNames = new Dictionary<Codec, string>
        {
            { Codec.CBZ, "CBZ" },
            { Codec.CBZ_HD, "CBZ HD" },
            { Codec.CBZ_Web, "CBZ Web" },
            { Codec.CBR, "CBR" },
            { Codec.CB7, "CB7" },
            { Codec.PDF, "PDF" },
            { Codec.EPUB, "EPUB" },
            { Codec.MOBI, "MOBI" },
            { Codec.AZW3, "AZW3" }
        };

        public static string FormatAudioCodec(MediaInfoModel mediaInfo)
        {
            var codec = QualityParser.ParseCodec(mediaInfo.AudioFormat, null);

            if (CodecNames.ContainsKey(codec))
            {
                return CodecNames[codec];
            }
            else
            {
                Logger.ForDebugEvent()
                    .Message("Unknown audio format: '{0}'.", string.Join(", ", mediaInfo.AudioFormat))
                    .WriteSentryWarn("UnknownAudioFormat", mediaInfo.AudioFormat)
                    .Log();

                return "Unknown";
            }
        }
    }
}
