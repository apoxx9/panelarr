using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class UpdateCleanTitleForSeriesFixture : CoreTest<UpdateCleanTitleForSeries>
    {
        [Test]
        public void should_update_clean_title()
        {
            var author = Builder<Series>.CreateNew()
                                        .With(s => s.Name = "Full Name")
                                        .With(s => s.CleanName = "unclean")
                                        .Build();

            Mocker.GetMock<ISeriesRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { author });

            Subject.Clean();

            Mocker.GetMock<ISeriesRepository>()
                .Verify(v => v.Update(It.Is<Series>(s => s.CleanName == "fullname")), Times.Once());
        }

        [Test]
        public void should_not_update_unchanged_title()
        {
            var author = Builder<Series>.CreateNew()
                                        .With(s => s.Name = "Full Name")
                                        .With(s => s.CleanName = "fullname")
                                        .Build();

            Mocker.GetMock<ISeriesRepository>()
                 .Setup(s => s.All())
                 .Returns(new[] { author });

            Subject.Clean();

            Mocker.GetMock<ISeriesRepository>()
                .Verify(v => v.Update(It.Is<Series>(s => s.CleanName == "fullname")), Times.Never());
        }
    }
}
