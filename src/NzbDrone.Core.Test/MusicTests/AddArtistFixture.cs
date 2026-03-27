using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AddSeriesFixture : CoreTest<AddSeriesService>
    {
        private Series _fakeSeries;

        [SetUp]
        public void Setup()
        {
            _fakeSeries = Builder<Series>
                .CreateNew()
                .With(s => s.Path = null)
                .Build();
            _fakeSeries.Books = new List<Issue>();

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()))
                .Returns<Series, bool>((author, _) => author);
        }

        private void GivenValidSeries(string panelarrId)
        {
            Mocker.GetMock<IProvideSeriesInfo>()
                .Setup(s => s.GetSeriesInfo(panelarrId, false))
                .Returns(_fakeSeries);
        }

        private void GivenValidPath()
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetSeriesFolder(It.IsAny<Series>(), null))
                  .Returns<Series, NamingConfig>((c, n) => c.Name);

            Mocker.GetMock<IAddSeriesValidator>()
                  .Setup(s => s.Validate(It.IsAny<Series>()))
                  .Returns(new ValidationResult());
        }

        [Test]
        public void should_be_able_to_add_a_author_without_passing_in_name()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                RootFolderPath = @"C:\Test\Music"
            };

            GivenValidSeries(newSeries.ForeignSeriesId);
            GivenValidPath();

            var author = Subject.AddSeries(newSeries);

            author.Name.Should().Be(_fakeSeries.Name);
        }

        [Test]
        public void should_have_proper_path()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                RootFolderPath = @"C:\Test\Music"
            };

            GivenValidSeries(newSeries.ForeignSeriesId);
            GivenValidPath();

            var author = Subject.AddSeries(newSeries);

            author.Path.Should().Be(Path.Combine(newSeries.RootFolderPath, _fakeSeries.Name));
        }

        [Test]
        public void should_throw_if_author_validation_fails()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1"
            };

            GivenValidSeries(newSeries.ForeignSeriesId);

            Mocker.GetMock<IAddSeriesValidator>()
                  .Setup(s => s.Validate(It.IsAny<Series>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddSeries(newSeries));
        }

        [Test]
        public void should_throw_if_author_cannot_be_found()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1"
            };

            Mocker.GetMock<IProvideSeriesInfo>()
                  .Setup(s => s.GetSeriesInfo(newSeries.ForeignSeriesId, false))
                  .Throws(new SeriesNotFoundException(newSeries.ForeignSeriesId));

            Mocker.GetMock<IAddSeriesValidator>()
                  .Setup(s => s.Validate(It.IsAny<Series>()))
                  .Returns(new ValidationResult(new List<ValidationFailure>
                                                {
                                                    new ValidationFailure("Path", "Test validation failure")
                                                }));

            Assert.Throws<ValidationException>(() => Subject.AddSeries(newSeries));

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_disambiguate_if_author_folder_exists()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeSeries.Metadata = Builder<SeriesMetadata>.CreateNew().With(x => x.Disambiguation = "Disambiguation").Build();

            GivenValidSeries(newSeries.ForeignSeriesId);
            GivenValidPath();

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path))
                .Returns(true);

            var author = Subject.AddSeries(newSeries);
            author.Path.Should().Be(newSeries.Path + " (Disambiguation)");
        }

        [Test]
        public void should_disambiguate_with_numbers_if_author_folder_still_exists()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeSeries.Metadata = Builder<SeriesMetadata>.CreateNew().With(x => x.Disambiguation = "Disambiguation").Build();

            GivenValidSeries(newSeries.ForeignSeriesId);
            GivenValidPath();

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path))
                .Returns(true);

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path + " (Disambiguation)"))
                .Returns(true);

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path + " (Disambiguation) (1)"))
                .Returns(true);

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path + " (Disambiguation) (2)"))
                .Returns(true);

            var author = Subject.AddSeries(newSeries);
            author.Path.Should().Be(newSeries.Path + " (Disambiguation) (3)");
        }

        [Test]
        public void should_disambiguate_with_numbers_if_author_folder_exists_and_no_disambiguation()
        {
            var newSeries = new Series
            {
                ForeignSeriesId = "ce09ea31-3d4a-4487-a797-e315175457a0",
                Path = @"C:\Test\Music\Name1",
            };

            _fakeSeries.Metadata = Builder<SeriesMetadata>.CreateNew().With(x => x.Disambiguation = string.Empty).Build();

            GivenValidSeries(newSeries.ForeignSeriesId);
            GivenValidPath();

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path))
                .Returns(true);

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path + " (1)"))
                .Returns(true);

            Mocker.GetMock<ISeriesService>()
                .Setup(x => x.SeriesPathExists(newSeries.Path + " (2)"))
                .Returns(true);

            var author = Subject.AddSeries(newSeries);
            author.Path.Should().Be(newSeries.Path + " (3)");
        }
    }
}
