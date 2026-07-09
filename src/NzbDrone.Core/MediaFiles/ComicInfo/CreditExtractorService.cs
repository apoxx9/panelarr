using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NLog;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public interface ICreditExtractorService
    {
        List<Credit> ParseCredits(string comicInfoXml);
    }

    // Parses credits out of a ComicInfo.xml document. The XML itself is read
    // by ArchiveInspector during its single pass over the archive — this
    // service never opens files.
    public class CreditExtractorService : ICreditExtractorService
    {
        private readonly Logger _logger;

        public CreditExtractorService(Logger logger)
        {
            _logger = logger;
        }

        public List<Credit> ParseCredits(string comicInfoXml)
        {
            var credits = new List<Credit>();

            if (string.IsNullOrWhiteSpace(comicInfoXml))
            {
                return credits;
            }

            XElement root;

            try
            {
                root = XDocument.Parse(comicInfoXml).Root;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to parse ComicInfo.xml for credits");
                return credits;
            }

            if (root == null)
            {
                return credits;
            }

            foreach (var (elementName, role) in ComicInfoCreditRoles.ElementToRole)
            {
                var element = root.Element(elementName);
                var value = element?.Value?.Trim();

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                // ComicInfo.xml uses comma-separated names for multiple people in the same role
                var names = value.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var name in names)
                {
                    var trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        credits.Add(new Credit { PersonName = trimmed, Role = role });
                    }
                }
            }

            return credits;
        }
    }
}
