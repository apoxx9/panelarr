using System;
using System.IO;
using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IBuildSeriesPaths
    {
        string BuildPath(Series series, bool useExistingRelativeFolder);
    }

    public class SeriesPathBuilder : IBuildSeriesPaths
    {
        private readonly IBuildFileNames _fileNameBuilder;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskProvider _diskProvider;

        public SeriesPathBuilder(IBuildFileNames fileNameBuilder,
                                 IRootFolderService rootFolderService,
                                 IDiskProvider diskProvider)
        {
            _fileNameBuilder = fileNameBuilder;
            _rootFolderService = rootFolderService;
            _diskProvider = diskProvider;
        }

        public string BuildPath(Series series, bool useExistingRelativeFolder)
        {
            if (series.RootFolderPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Root folder was not provided", nameof(series));
            }

            if (useExistingRelativeFolder && series.Path.IsNotNullOrWhiteSpace())
            {
                var relativePath = GetExistingRelativePath(series);
                return Path.Combine(series.RootFolderPath, relativePath);
            }

            var seriesFolder = NormalizeParentFolders(series.RootFolderPath, _fileNameBuilder.GetSeriesFolder(series));

            return Path.Combine(series.RootFolderPath, seriesFolder);
        }

        private string GetExistingRelativePath(Series series)
        {
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(series.Path);

            return rootFolderPath.GetRelativePath(series.Path);
        }

        // Metadata-derived parent folders (e.g. the publisher level of
        // "{Series Publisher}/{Series Title}") can differ from what is already
        // on disk in case or punctuation ("Boom! Studios" vs an existing
        // "Boom Studios"), silently splitting the library. Adopt the existing
        // folder's spelling when one matches. The series folder itself (the
        // last component) is never rewritten: equating two similarly named
        // series folders could merge distinct series.
        private string NormalizeParentFolders(string rootFolderPath, string relativeFolder)
        {
            var components = relativeFolder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var currentPath = rootFolderPath;

            for (var i = 0; i < components.Length - 1; i++)
            {
                var existing = FindEquivalentFolder(currentPath, components[i]);

                if (existing != null)
                {
                    components[i] = existing;
                }

                currentPath = Path.Combine(currentPath, components[i]);
            }

            return Path.Combine(components);
        }

        private string FindEquivalentFolder(string parentPath, string folderName)
        {
            var key = FolderComparisonKey(folderName);

            if (key.IsNullOrWhiteSpace() || !_diskProvider.FolderExists(parentPath))
            {
                return null;
            }

            return _diskProvider.GetDirectories(parentPath)
                .Select(Path.GetFileName)
                .Where(name => FolderComparisonKey(name) == key)
                .OrderBy(name => name != folderName)
                .ThenBy(name => name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static string FolderComparisonKey(string folderName)
        {
            return new string(folderName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        }
    }
}
