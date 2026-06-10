using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;
using Panelarr.Api.V1.History;
using Panelarr.Api.V1.Indexers;
using Panelarr.Api.V1.Queue;
using Panelarr.Http.REST;

namespace NzbDrone.Api.Test.ApiContractTests
{
    [TestFixture]
    public class ResourceRemnantContractFixture : TestBase
    {
        // Property names inherited from Lidarr (music) or Readarr (books) that must not
        // appear on any API resource. Comics have no albums, artists, tracks, audio,
        // authors, books or discographies.
        private static readonly string[] ForbiddenPropertyNames =
        {
            "AudioTags",
            "AudioFormat",
            "AudioBitrate",
            "AudioChannels",
            "AudioBits",
            "AudioSampleRate",
            "Discography",
            "DiscographyStart",
            "DiscographyEnd",
            "ArtistName",
            "ArtistId",
            "AlbumId",
            "AlbumTitle",
            "AlbumCount",
            "TrackNumber",
            "TrackCount",
            "AuthorName",
            "AuthorId",
            "BookId",
            "BookTitle",
            "BookCount",
            "MusicBrainzId",
            "GoodreadsId"
        };

        private static IEnumerable<Type> AllV1ResourceTypes()
        {
            return typeof(QueueResource).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(RestResource).IsAssignableFrom(t));
        }

        [Test]
        public void no_v1_resource_should_expose_music_or_book_remnant_properties()
        {
            var violations = new List<string>();

            foreach (var type in AllV1ResourceTypes())
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (ForbiddenPropertyNames.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        violations.Add($"{type.Name}.{prop.Name}");
                    }
                }
            }

            violations.Should().BeEmpty(
                "API resources must not expose Lidarr/Readarr remnant properties the frontend never reads");
        }

        [Test]
        public void queue_resource_should_have_expected_frontend_properties()
        {
            var props = ContractTestHelpers.GetJsonPropertyNames(typeof(QueueResource)).ToList();

            // Properties QueueRow.js declares and renders
            var expectedProps = new[]
            {
                "id",
                "downloadId",
                "title",
                "status",
                "trackedDownloadStatus",
                "trackedDownloadState",
                "statusMessages",
                "errorMessage",
                "series",
                "issue",
                "quality",
                "customFormats",
                "customFormatScore",
                "protocol",
                "indexer",
                "outputPath",
                "downloadClient",
                "downloadForced",
                "estimatedCompletionTime",
                "timeleft",
                "size",
                "sizeleft"
            };

            foreach (var expected in expectedProps)
            {
                props.Should().Contain(expected,
                    $"frontend QueueRow reads '{expected}' from the queue API response");
            }
        }

        [Test]
        public void history_resource_should_have_expected_frontend_properties()
        {
            var props = ContractTestHelpers.GetJsonPropertyNames(typeof(HistoryResource)).ToList();

            // Properties HistoryRow.js declares and renders
            var expectedProps = new[]
            {
                "id",
                "issueId",
                "seriesId",
                "quality",
                "customFormats",
                "customFormatScore",
                "qualityCutoffNotMet",
                "eventType",
                "sourceTitle",
                "date",
                "data"
            };

            foreach (var expected in expectedProps)
            {
                props.Should().Contain(expected,
                    $"frontend HistoryRow reads '{expected}' from the history API response");
            }
        }

        [Test]
        public void release_resource_should_have_isCollection_not_discography()
        {
            var props = ContractTestHelpers.GetJsonPropertyNames(typeof(ReleaseResource)).ToList();

            props.Should().Contain("isCollection",
                "frontend release filters match on 'isCollection'");
            props.Should().NotContain("discography",
                "'discography' is a Lidarr remnant — renamed to 'isCollection'");
        }

        [Test]
        public void release_resource_should_have_expected_frontend_properties()
        {
            var props = ContractTestHelpers.GetJsonPropertyNames(typeof(ReleaseResource)).ToList();

            // Properties InteractiveSearchRow.tsx and release filters read
            var expectedProps = new[]
            {
                "guid",
                "title",
                "indexer",
                "indexerId",
                "size",
                "seeders",
                "leechers",
                "quality",
                "protocol",
                "age",
                "ageHours",
                "ageMinutes",
                "rejections",
                "isCollection"
            };

            foreach (var expected in expectedProps)
            {
                props.Should().Contain(expected,
                    $"frontend interactive search reads '{expected}' from the release API response");
            }
        }
    }

    internal static class ContractTestHelpers
    {
        /// <summary>
        /// Converts PascalCase to camelCase (matching System.Text.Json default behavior).
        /// </summary>
        public static string ToCamelCase(string name)
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

        public static IEnumerable<string> GetJsonPropertyNames(Type resourceType)
        {
            return resourceType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => ToCamelCase(p.Name));
        }
    }
}
