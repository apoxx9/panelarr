using System.Collections.Generic;

namespace VersOne.Epub
{
    public class EpubBook
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Series { get; set; }
        public List<string> SeriesList { get; set; }
        public EpubSchema Schema { get; set; }
    }
}
