using System;
using System.IO;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using NLog;

namespace NzbDrone.Core.Download.Clients.GetComics
{
    public interface IMegaDownloader
    {
        /// <summary>
        /// Downloads and decrypts a public mega.nz file link into
        /// destinationFolder as fileNameWithoutExtension plus the extension
        /// the Mega node really carries. Returns the written path.
        /// </summary>
        Task<string> DownloadFileAsync(string megaUrl, string destinationFolder, string fileNameWithoutExtension);
    }

    // Mega streams are AES-CTR encrypted with the key in the URL fragment, so
    // a link can never resolve to a plainly downloadable URL - the client
    // library decrypts while downloading (the same model as Mylar's mega.py).
    public class MegaDownloader : IMegaDownloader
    {
        private static readonly string[] AllowedExtensions = { ".cbz", ".cbr", ".pdf", ".zip", ".rar" };

        private readonly Logger _logger;

        public MegaDownloader(Logger logger)
        {
            _logger = logger;
        }

        public async Task<string> DownloadFileAsync(string megaUrl, string destinationFolder, string fileNameWithoutExtension)
        {
            var client = new MegaApiClient();
            await client.LoginAnonymousAsync();

            try
            {
                var uri = new Uri(megaUrl);
                var node = await client.GetNodeFromLinkAsync(uri);

                var extension = Path.GetExtension(node.Name ?? string.Empty).ToLowerInvariant();

                if (!Array.Exists(AllowedExtensions, x => x == extension))
                {
                    extension = ".cbz";
                }

                var destinationPath = Path.Combine(destinationFolder, fileNameWithoutExtension + extension);

                _logger.Info("Mega: downloading '{0}' ({1} bytes) to '{2}'", node.Name, node.Size, destinationPath);
                await client.DownloadFileAsync(uri, destinationPath);

                return destinationPath;
            }
            finally
            {
                try
                {
                    await client.LogoutAsync();
                }
                catch (Exception ex)
                {
                    _logger.Trace(ex, "Mega: logout failed");
                }
            }
        }
    }
}
