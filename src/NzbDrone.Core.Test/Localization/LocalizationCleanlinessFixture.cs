using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Core.Test.Localization
{
    [TestFixture]
    public class LocalizationCleanlinessFixture
    {
        // Lidarr/Readarr leftovers that must not appear in any locale value.
        // Comics have no albums, artists, tracks, audio or Calibre/MusicBrainz/Goodreads integrations.
        private static readonly Regex ForbiddenTerms = new Regex(
            @"album|artist|musicbrainz|goodreads|\btracks?\b|audio|calibre|discograph",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string LocalizationFolder => Path.Combine(TestContext.CurrentContext.TestDirectory, "Localization", "Core");

        private static Dictionary<string, string> ReadLocale(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));

            return doc.RootElement.EnumerateObject()
                .Where(p => p.Value.ValueKind == JsonValueKind.String)
                .ToDictionary(p => p.Name, p => p.Value.GetString());
        }

        private static IEnumerable<string> LocaleFiles => Directory.GetFiles(LocalizationFolder, "*.json");

        [Test]
        public void english_should_not_contain_music_or_book_terms()
        {
            var en = ReadLocale(Path.Combine(LocalizationFolder, "en.json"));

            var offenders = en.Where(kv => ForbiddenTerms.IsMatch(kv.Value))
                              .Select(kv => $"{kv.Key} = {kv.Value}")
                              .ToList();

            offenders.Should().BeEmpty();
        }

        [Test]
        public void locales_should_not_contain_music_or_book_terms()
        {
            var offenders = new List<string>();

            foreach (var file in LocaleFiles)
            {
                var name = Path.GetFileName(file);

                if (name == "en.json")
                {
                    continue;
                }

                foreach (var kv in ReadLocale(file))
                {
                    if (ForbiddenTerms.IsMatch(kv.Value))
                    {
                        offenders.Add($"{name}: {kv.Key} = {kv.Value}");
                    }
                }
            }

            offenders.Should().BeEmpty("stale Lidarr/Readarr translations should be deleted so the value falls back to en.json");
        }

        [Test]
        public void locales_should_not_contain_keys_missing_from_english()
        {
            var enKeys = ReadLocale(Path.Combine(LocalizationFolder, "en.json")).Keys.ToHashSet(StringComparer.Ordinal);
            var offenders = new List<string>();

            foreach (var file in LocaleFiles)
            {
                var name = Path.GetFileName(file);

                if (name == "en.json")
                {
                    continue;
                }

                offenders.AddRange(ReadLocale(file).Keys
                    .Where(k => !enKeys.Contains(k))
                    .Select(k => $"{name}: {k}"));
            }

            offenders.Should().BeEmpty("en.json is the source of truth; orphan keys are dead Lidarr/Readarr translations");
        }
    }
}
