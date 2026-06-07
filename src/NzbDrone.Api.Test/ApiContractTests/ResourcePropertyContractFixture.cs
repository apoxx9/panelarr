using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;
using Panelarr.Api.V1.ComicFiles;
using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.ManualImport;
using Panelarr.Api.V1.Series;

namespace NzbDrone.Api.Test.ApiContractTests
{
    [TestFixture]
    public class ResourcePropertyContractFixture : TestBase
    {
        /// <summary>
        /// Converts PascalCase to camelCase (matching System.Text.Json default behavior).
        /// </summary>
        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            var chars = name.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (i == 0 || (i > 0 && char.IsUpper(chars[i]) && i + 1 < chars.Length && char.IsUpper(chars[i + 1])))
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }
                else if (i > 0 && char.IsUpper(chars[i]))
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                    break;
                }
                else
                {
                    break;
                }
            }

            return new string(chars);
        }

        private static IEnumerable<string> GetJsonPropertyNames(Type resourceType)
        {
            return resourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => ToCamelCase(p.Name));
        }

        [Test]
        public void comic_file_resource_should_have_fileTags_not_audioTags()
        {
            var props = GetJsonPropertyNames(typeof(ComicFileResource)).ToList();

            props.Should().Contain("fileTags",
                "frontend reads 'fileTags' from the comic file API response");
            props.Should().NotContain("audioTags",
                "'audioTags' is a Lidarr remnant — comics don't have audio");
        }

        [Test]
        public void manual_import_resource_should_have_fileTags_not_audioTags()
        {
            var props = GetJsonPropertyNames(typeof(ManualImportResource)).ToList();

            props.Should().Contain("fileTags",
                "frontend reads 'fileTags' from the manual import API response");
            props.Should().NotContain("audioTags",
                "'audioTags' is a Lidarr remnant — comics don't have audio");
        }

        [Test]
        public void comic_file_list_resource_should_have_issueFileIds_not_comicFileIds()
        {
            var props = GetJsonPropertyNames(typeof(ComicFileListResource)).ToList();

            props.Should().Contain("issueFileIds",
                "frontend sends 'issueFileIds' for bulk file operations");
            props.Should().NotContain("comicFileIds",
                "frontend does not use 'comicFileIds' — mismatch would cause silent failure");
        }

        [Test]
        public void series_resource_should_have_expected_frontend_properties()
        {
            var props = GetJsonPropertyNames(typeof(SeriesResource)).ToList();

            // Properties the frontend actively reads
            var expectedProps = new[]
            {
                "id",
                "seriesName",
                "sortName",
                "titleSlug",
                "images",
                "statistics",
                "qualityProfileId",
                "monitored",
                "path",
                "tags",
                "added",
                "status",
                "overview",
                "year"
            };

            foreach (var expected in expectedProps)
            {
                props.Should().Contain(expected,
                    $"frontend reads '{expected}' from the series API response");
            }
        }

        [Test]
        public void issue_resource_should_have_expected_frontend_properties()
        {
            var props = GetJsonPropertyNames(typeof(IssueResource)).ToList();

            var expectedProps = new[]
            {
                "id",
                "title",
                "issueNumber",
                "releaseDate",
                "pageCount",
                "monitored"
            };

            foreach (var expected in expectedProps)
            {
                props.Should().Contain(expected,
                    $"frontend reads '{expected}' from the issue API response");
            }
        }

        [Test]
        public void primary_resources_should_not_contain_audioTags_property()
        {
            // Guard against re-introducing audioTags on resources the frontend consumes directly
            var primaryResourceTypes = new[]
            {
                typeof(ComicFileResource),
                typeof(ComicFileListResource),
                typeof(ManualImportResource)
            };

            var violations = new List<string>();

            foreach (var type in primaryResourceTypes)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.Name == "AudioTags")
                    {
                        violations.Add($"{type.Name}.{prop.Name}");
                    }
                }
            }

            violations.Should().BeEmpty(
                "primary API resources must use 'FileTags' not 'AudioTags' — frontend reads 'fileTags'");
        }
    }
}
