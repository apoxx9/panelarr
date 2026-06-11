using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]
    public class CleanFilenameFixture : CoreTest
    {
        [TestCase("Batman: White Knight - 007 - Icarus [Digital]", "Batman - White Knight - 007 - Icarus [Digital]")]
        public void should_replaace_invalid_characters(string name, string expectedName)
        {
            FileNameBuilder.CleanFileName(name).Should().Be(expectedName);
        }

        [TestCase(".hack 001", "hack 001")]
        public void should_remove_periods_from_start(string name, string expectedName)
        {
            FileNameBuilder.CleanFileName(name).Should().Be(expectedName);
        }

        [TestCase(" Saga - 001 - Issue Title", "Saga - 001 - Issue Title")]
        [TestCase("Saga - 001 - Issue Title ", "Saga - 001 - Issue Title")]
        public void should_remove_spaces_from_start_and_end(string name, string expectedName)
        {
            FileNameBuilder.CleanFileName(name).Should().Be(expectedName);
        }
    }
}
