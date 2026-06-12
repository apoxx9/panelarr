using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.LibraryImport
{
    [TestFixture]
    public class LibraryImportServiceFixture : CoreTest<LibraryImportService>
    {
        private LibraryImportCommand _command;

        [SetUp]
        public void Setup()
        {
            _command = new LibraryImportCommand
            {
                QualityProfileId = 1,
                Monitored = true,
                MonitorNewItems = "all",
                Series = new List<LibraryImportSeries>
                {
                    new () { ForeignSeriesId = "cv:30345", Folder = @"C:\comics\The Walking Dead (2004)".AsOsAgnostic() },
                    new () { ForeignSeriesId = "cv:46568", Folder = @"C:\comics\Saga (2012)".AsOsAgnostic() }
                }
            };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById(It.IsAny<string>()))
                  .Returns((Series)null);
        }

        [Test]
        public void should_add_each_series_with_the_existing_folder_as_path()
        {
            Subject.Execute(_command);

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.Is<Series>(x =>
                      x.Metadata.Value.ForeignSeriesId == "cv:30345" &&
                      x.Path == @"C:\comics\The Walking Dead (2004)".AsOsAgnostic() &&
                      x.QualityProfileId == 1 &&
                      x.Monitored), true), Times.Once());

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.IsAny<Series>(), true), Times.Exactly(2));
        }

        [Test]
        public void should_skip_series_already_in_the_library()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:30345"))
                  .Returns(new Series { Id = 6 });

            Subject.Execute(_command);

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.IsAny<Series>(), true), Times.Once());
        }

        [Test]
        public void one_failed_add_should_not_stop_the_batch()
        {
            Mocker.GetMock<IAddSeriesService>()
                  .Setup(s => s.AddSeries(It.Is<Series>(x => x.Metadata.Value.ForeignSeriesId == "cv:30345"), true))
                  .Throws(new Exception("provider exploded"));

            Subject.Execute(_command);

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.Is<Series>(x => x.Metadata.Value.ForeignSeriesId == "cv:46568"), true), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void unmonitored_batch_should_add_unmonitored_series()
        {
            _command.Monitored = false;

            Subject.Execute(_command);

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.Is<Series>(x =>
                      !x.Monitored &&
                      x.AddOptions.Monitor == MonitorTypes.None), true), Times.Exactly(2));
        }

        [Test]
        public void empty_command_should_do_nothing()
        {
            Subject.Execute(new LibraryImportCommand { Series = new List<LibraryImportSeries>() });

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
