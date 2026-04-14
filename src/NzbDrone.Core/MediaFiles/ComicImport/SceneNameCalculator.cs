using System.IO;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport
{
    public static class SceneNameCalculator
    {
        public static string GetSceneName(LocalIssue localIssue)
        {
            var downloadClientInfo = localIssue.DownloadClientIssueInfo;

            if (downloadClientInfo != null && !downloadClientInfo.Discography)
            {
                return Parser.Parser.RemoveFileExtension(downloadClientInfo.ReleaseTitle);
            }

            var fileName = Path.GetFileNameWithoutExtension(localIssue.Path.CleanFilePath());

            if (SceneChecker.IsSceneTitle(fileName))
            {
                return fileName;
            }

            var folderTitle = localIssue.FolderTrackInfo?.ReleaseTitle;

            if (localIssue.FolderTrackInfo?.Discography == false &&
                folderTitle.IsNotNullOrWhiteSpace() &&
                SceneChecker.IsSceneTitle(folderTitle))
            {
                return folderTitle;
            }

            return null;
        }
    }
}
