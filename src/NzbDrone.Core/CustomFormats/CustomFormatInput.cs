using NzbDrone.Core.Books;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public class CustomFormatInput
    {
        public ParsedBookInfo BookInfo { get; set; }
        public Series Series { get; set; }
        public long Size { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public string Filename { get; set; }

        // public CustomFormatInput(ParsedEpisodeInfo episodeInfo, SeriesGroup series)
        // {
        //     EpisodeInfo = episodeInfo;
        //     SeriesGroup = series;
        // }
        //
        // public CustomFormatInput(ParsedEpisodeInfo episodeInfo, SeriesGroup series, long size, List<Language> languages)
        // {
        //     EpisodeInfo = episodeInfo;
        //     SeriesGroup = series;
        //     Size = size;
        //     Languages = languages;
        // }
        //
        // public CustomFormatInput(ParsedEpisodeInfo episodeInfo, SeriesGroup series, long size, List<Language> languages, string filename)
        // {
        //     EpisodeInfo = episodeInfo;
        //     SeriesGroup = series;
        //     Size = size;
        //     Languages = languages;
        //     Filename = filename;
        // }
    }
}
