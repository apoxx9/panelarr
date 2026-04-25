using System.Linq;
using System.Text;
using System.Xml;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public interface IMetronInfoGenerator
    {
        string Generate(Issue issue, SeriesMetadata seriesMetadata, Publisher publisher);
    }

    public class MetronInfoGenerator : IMetronInfoGenerator
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
                writer.WriteStartElement("MetronInfo");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "https://metron.cloud/schemas/MetronInfo.xsd");

                // Source / ID block
                if (!string.IsNullOrWhiteSpace(issue?.ForeignIssueId))
                {
                    writer.WriteStartElement("IDs");
                    writer.WriteStartElement("ID");
                    writer.WriteAttributeString("source", "metron");
                    writer.WriteValue(issue.ForeignIssueId);
                    writer.WriteEndElement(); // ID
                    writer.WriteEndElement(); // IDs
                }

                // Publisher
                if (publisher != null && !string.IsNullOrWhiteSpace(publisher.Name))
                {
                    writer.WriteStartElement("Publisher");
                    WriteElement(writer, "Name", publisher.Name);
                    writer.WriteEndElement();
                }

                // Series
                if (seriesMetadata != null)
                {
                    writer.WriteStartElement("Series");
                    WriteElement(writer, "Name", seriesMetadata.Name);
                    if (seriesMetadata.Year.HasValue)
                    {
                        WriteElement(writer, "StartYear", seriesMetadata.Year.Value.ToString());
                    }

                    writer.WriteEndElement(); // Series
                }

                // Issue details
                if (issue != null)
                {
                    WriteElement(writer, "Number", issue.IssueNumber.ToString("0.##"));
                    WriteElement(writer, "Title", issue.Title);

                    if (seriesMetadata?.Genres != null && seriesMetadata.Genres.Any())
                    {
                        writer.WriteStartElement("Genres");
                        foreach (var genre in seriesMetadata.Genres)
                        {
                            WriteElement(writer, "Genre", genre);
                        }

                        writer.WriteEndElement(); // Genres
                    }

                    WriteElement(writer, "Summary", !string.IsNullOrWhiteSpace(issue?.Overview) ? issue.Overview : seriesMetadata?.Overview);

                    if (issue.PageCount > 0)
                    {
                        WriteElement(writer, "PageCount", issue.PageCount.ToString());
                    }

                    // Credits
                    if (issue.Credits != null && issue.Credits.Any())
                    {
                        writer.WriteStartElement("Credits");
                        foreach (var credit in issue.Credits)
                        {
                            writer.WriteStartElement("Credit");
                            WriteElement(writer, "Creator", credit.PersonName);
                            writer.WriteStartElement("Roles");
                            WriteElement(writer, "Role", credit.Role);
                            writer.WriteEndElement(); // Roles
                            writer.WriteEndElement(); // Credit
                        }

                        writer.WriteEndElement(); // Credits
                    }
                }

                writer.WriteEndElement(); // MetronInfo
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
