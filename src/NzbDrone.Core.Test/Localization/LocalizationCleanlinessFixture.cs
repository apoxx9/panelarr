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
        public void frontend_translate_keys_should_exist_in_english()
        {
            // Guard for the Session 21 field bug: a dead-key sweep removed
            // keys still referenced from .ts files, so the UI rendered raw
            // key names (StatusEndedContinuing). Every translate('Key')
            // literal in the frontend must have an en.json entry.
            var repoRoot = FindRepoRoot();

            if (repoRoot == null)
            {
                Assert.Ignore("frontend sources not available next to the test directory");
            }

            var translateCall = new Regex(@"translate\(\s*'(?<key>[A-Za-z0-9_]+)'", RegexOptions.Compiled);
            var enKeys = ReadLocale(Path.Combine(LocalizationFolder, "en.json")).Keys.ToHashSet(StringComparer.Ordinal);
            var frontendSrc = Path.Combine(repoRoot, "frontend", "src");

            var missing = Directory.EnumerateFiles(frontendSrc, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".js") || f.EndsWith(".ts") || f.EndsWith(".tsx"))
                .SelectMany(file => translateCall.Matches(File.ReadAllText(file))
                    .Select(m => m.Groups["key"].Value)
                    .Where(key => !enKeys.Contains(key))
                    .Select(key => $"{Path.GetRelativePath(frontendSrc, file)}: {key}"))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            missing.Should().BeEmpty("every translate('Key') literal needs an en.json entry, otherwise the UI shows the raw key name");
        }

        [Test]
        public void backend_localized_string_keys_should_exist_in_english()
        {
            var repoRoot = FindRepoRoot();

            if (repoRoot == null)
            {
                Assert.Ignore("backend sources not available next to the test directory");
            }

            var localizedCall = new Regex(@"GetLocalizedString\(\s*""(?<key>[A-Za-z0-9_]+)""", RegexOptions.Compiled);
            var enKeys = ReadLocale(Path.Combine(LocalizationFolder, "en.json")).Keys.ToHashSet(StringComparer.Ordinal);
            var sourceRoot = Path.Combine(repoRoot, "src");

            var missing = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                            !f.Contains(".Test"))
                .SelectMany(file => localizedCall.Matches(File.ReadAllText(file))
                    .Select(m => m.Groups["key"].Value)
                    .Where(key => !enKeys.Contains(key))
                    .Select(key => $"{Path.GetRelativePath(sourceRoot, file)}: {key}"))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            missing.Should().BeEmpty("every GetLocalizedString(\"Key\") needs an en.json entry, otherwise the UI shows the raw key name");
        }

        [Test]
        public void dynamic_prefix_translate_keys_should_be_complete()
        {
            // The frontend builds these keys at runtime (translate(`Prefix_${value}`)),
            // which the literal scan can't see. Enum-backed sets come from the
            // enums; the status sets are frontend literals kept in sync here
            // (PullListPage.js, ReadingListController ToSlotResources).
            var enKeys = ReadLocale(Path.Combine(LocalizationFolder, "en.json")).Keys.ToHashSet(StringComparer.Ordinal);

            var expected = new List<string>();

            expected.AddRange(Enum.GetNames(typeof(NzbDrone.Core.ReadingLists.ReadingListType))
                .Select(n => $"ReadingListType_{CamelCase(n)}"));
            expected.AddRange(Enum.GetNames(typeof(NzbDrone.Core.Issues.Relations.SeriesRelationType))
                .Select(n => $"SeriesRelationType_{CamelCase(n)}"));
            expected.AddRange(new[] { "have", "missing", "notInLibrary" }.Select(s => $"ReadingListStatus_{s}"));
            expected.AddRange(new[] { "have", "grabbed", "missing", "unreleased", "unmonitored" }.Select(s => $"PullListStatus_{s}"));

            expected.Where(k => !enKeys.Contains(k)).Should().BeEmpty();
        }

        private static string CamelCase(string name)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "frontend", "src")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName;
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
