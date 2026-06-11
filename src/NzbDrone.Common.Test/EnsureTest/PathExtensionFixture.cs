using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.EnsureTest
{
    [TestFixture]
    public class PathExtensionFixture : TestBase
    {
        [TestCase(@"p:\Comics\file with, comma.cbz")]
        [TestCase(@"\\serer\share\file with, comma.cbz")]
        public void EnsureWindowsPath(string path)
        {
            WindowsOnly();
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);
        }

        [TestCase(@"/var/user/file with, comma.cbz")]
        public void EnsureLinuxPath(string path)
        {
            PosixOnly();
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);
        }
    }
}
