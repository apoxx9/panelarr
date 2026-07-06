using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Arcs.Cbl
{
    // ComicRack reading-list XML (.cbl) — the community interchange format
    // consumed by Kavita, Komga and Mylar. Book order IS the reading order.
    // The <Database Name="cv"> child is a community extension carrying
    // ComicVine ids (DieselTech lists all have it); we read and write it so
    // id-level matching survives the round trip (docs/story-arcs.md).
    public static class CblFormat
    {
        public static CblReadingList Parse(string xml)
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw new InvalidCblFileException($"Not a valid XML document: {ex.Message}");
            }

            var root = doc.Root;

            if (root == null || root.Name.LocalName != "ReadingList")
            {
                throw new InvalidCblFileException("Root element is not <ReadingList>");
            }

            var list = new CblReadingList
            {
                Name = root.Element("Name")?.Value
            };

            var books = root.Element("Books")?.Elements("Book") ?? Enumerable.Empty<XElement>();

            foreach (var book in books)
            {
                var entry = new CblBook
                {
                    Series = (string)book.Attribute("Series"),
                    Number = (string)book.Attribute("Number"),
                    Volume = (string)book.Attribute("Volume"),
                    Year = (string)book.Attribute("Year")
                };

                var cvDatabase = book.Elements("Database")
                    .FirstOrDefault(d => string.Equals((string)d.Attribute("Name"), "cv", StringComparison.OrdinalIgnoreCase));

                if (cvDatabase != null)
                {
                    if (int.TryParse((string)cvDatabase.Attribute("Series"), out var volumeId))
                    {
                        entry.CvVolumeId = volumeId;
                    }

                    if (int.TryParse((string)cvDatabase.Attribute("Issue"), out var issueId))
                    {
                        entry.CvIssueId = issueId;
                    }
                }

                list.Books.Add(entry);
            }

            return list;
        }

        public static string Write(string name, IEnumerable<CblBook> books)
        {
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var bookList = books.ToList();

            var root = new XElement("ReadingList",
                new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XElement("Name", name),
                new XElement("NumIssues", bookList.Count),
                new XElement("Books",
                    bookList.Select(b =>
                    {
                        var book = new XElement("Book",
                            new XAttribute("Series", b.Series ?? string.Empty),
                            new XAttribute("Number", b.Number ?? string.Empty));

                        if (b.Volume.IsNotNullOrWhiteSpace())
                        {
                            book.Add(new XAttribute("Volume", b.Volume));
                        }

                        if (b.Year.IsNotNullOrWhiteSpace())
                        {
                            book.Add(new XAttribute("Year", b.Year));
                        }

                        if (b.CvVolumeId.HasValue || b.CvIssueId.HasValue)
                        {
                            var database = new XElement("Database", new XAttribute("Name", "cv"));

                            if (b.CvVolumeId.HasValue)
                            {
                                database.Add(new XAttribute("Series", b.CvVolumeId.Value));
                            }

                            if (b.CvIssueId.HasValue)
                            {
                                database.Add(new XAttribute("Issue", b.CvIssueId.Value));
                            }

                            book.Add(database);
                        }

                        return book;
                    })),
                new XElement("Matchers"));

            return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
        }
    }

    public class InvalidCblFileException : Exception
    {
        public InvalidCblFileException(string message)
            : base(message)
        {
        }
    }
}
