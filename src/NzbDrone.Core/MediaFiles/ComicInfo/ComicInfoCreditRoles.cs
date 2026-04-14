using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    /// <summary>
    /// Shared mapping between ComicInfo.xml element names and display role names.
    /// Used by both CreditExtractorService (read) and ComicInfoGenerator (write).
    /// </summary>
    public static class ComicInfoCreditRoles
    {
        /// <summary>
        /// Maps ComicInfo.xml element name → display role name.
        /// E.g. "CoverArtist" (XML) → "Cover Artist" (display).
        /// </summary>
        public static readonly Dictionary<string, string> ElementToRole = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Writer", "Writer" },
            { "Penciller", "Penciller" },
            { "Inker", "Inker" },
            { "Colorist", "Colorist" },
            { "Letterer", "Letterer" },
            { "CoverArtist", "Cover Artist" },
            { "Editor", "Editor" }
        };

        /// <summary>
        /// Maps display role name → ComicInfo.xml element name.
        /// E.g. "Cover Artist" (display) → "CoverArtist" (XML).
        /// </summary>
        public static readonly Dictionary<string, string> RoleToElement;

        static ComicInfoCreditRoles()
        {
            RoleToElement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (element, role) in ElementToRole)
            {
                RoleToElement[role] = element;
            }
        }
    }
}
