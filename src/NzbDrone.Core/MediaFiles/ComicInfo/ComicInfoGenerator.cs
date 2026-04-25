using System.Linq;
using System.Text;
using System.Xml;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public interface IComicInfoGenerator
    {
        string Generate(Issue issue, SeriesMetadata seriesMetadata, Publisher publisher);
    }

    public class ComicInfoGenerator : IComicInfoGenerator
    {
        public string Generate(Issue issue, SeriesMetadata seriesMetadata, Publisher publisher)
        {
            var sb = new StringBuilder();

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("ComicInfo");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");

                WriteElement(writer, "Series", seriesMetadata?.Name);
                WriteElement(writer, "Number", issue?.IssueNumber.ToString("0.##"));
                WriteElement(writer, "Year", seriesMetadata?.Year?.ToString());
                WriteElement(writer, "Publisher", publisher?.Name);
                WriteElement(writer, "PageCount", issue?.PageCount > 0 ? issue.PageCount.ToString() : null);
                WriteElement(writer, "Title", issue?.Title);
                WriteElement(writer, "Genre", seriesMetadata?.Genres != null && seriesMetadata.Genres.Any()
                    ? string.Join(", ", seriesMetadata.Genres)
                    : null);
                WriteElement(writer, "Summary", !string.IsNullOrWhiteSpace(issue?.Overview) ? issue.Overview : seriesMetadata?.Overview);
                WriteElement(writer, "Web", issue?.CoverArtUrl);

                // Credits — ComicInfo.xml uses one element per role with comma-separated names
                if (issue?.Credits != null && issue.Credits.Any())
                {
                    foreach (var (role, elementName) in ComicInfoCreditRoles.RoleToElement)
                    {
                        var names = issue.Credits
                            .Where(c => c.Role == role)
                            .Select(c => c.PersonName)
                            .ToList();

                        if (names.Any())
                        {
                            WriteElement(writer, elementName, string.Join(", ", names));
                        }
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return sb.ToString();
        }

        private static void WriteElement(XmlWriter writer, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            writer.WriteElementString(name, value);
        }
    }
}
