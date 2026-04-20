using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]

    public class FileNameBuilderFixture : CoreTest<FileNameBuilder>
    {
        private Series _series;
        private Issue _issue;
        private ComicFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                    .CreateNew()
                    .With(s => s.Name = "Linkin Park")
                    .With(s => s.Metadata = new SeriesMetadata
                    {
                        Disambiguation = "US Rock Band",
                        Name = "Linkin Park"
                    })
                    .Build();

            var series = Builder<SeriesGroup>
                .CreateNew()
                .With(x => x.Title = "SeriesGroup Title")
                .Build();

            var seriesLink = Builder<SeriesGroupLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = "1-2")
                .With(s => s.SeriesGroup = series)
                .BuildListOfNew();

            _issue = Builder<Issue>
                .CreateNew()
                .With(s => s.Title = "Hybrid Theory")
                .With(s => s.SeriesMetadata = _series.Metadata.Value)
                .With(s => s.SeriesLinks = seriesLink)
                .Build();
            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameComics = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _trackFile = Builder<ComicFile>.CreateNew()
                .With(e => e.Part = 1)
                .With(e => e.PartCount = 1)
                .With(e => e.Quality = new QualityModel(Quality.CBR))
                .With(e => e.ReleaseGroup = "PanelarrTest")
                .Build();

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        private void GivenProper()
        {
            _trackFile.Quality.Revision.Version = 2;
        }

        private void GivenReal()
        {
            _trackFile.Quality.Revision.Real = 1;
        }

        [Test]
        public void should_replace_Series_space_Name()
        {
            _namingConfig.StandardIssueFormat = "{Series Name}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin Park");
        }

        [Test]
        public void should_replace_Series_underscore_Name()
        {
            _namingConfig.StandardIssueFormat = "{Series_Name}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin_Park");
        }

        [Test]
        public void should_replace_Series_dot_Name()
        {
            _namingConfig.StandardIssueFormat = "{Series.Name}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin.Park");
        }

        [Test]
        public void should_replace_Series_dash_Name()
        {
            _namingConfig.StandardIssueFormat = "{Series-Name}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin-Park");
        }

        [Test]
        public void should_replace_SERIES_NAME_with_all_caps()
        {
            _namingConfig.StandardIssueFormat = "{SERIES NAME}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("LINKIN PARK");
        }

        [Test]
        public void should_replace_SERIES_NAME_with_random_casing_should_keep_original_casing()
        {
            _namingConfig.StandardIssueFormat = "{sErIeS-nAmE}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(_series.Name.Replace(' ', '-'));
        }

        [Test]
        public void should_replace_series_name_with_all_lower_case()
        {
            _namingConfig.StandardIssueFormat = "{series name}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("linkin park");
        }

        [Test]
        public void should_cleanup_Series_Name()
        {
            _namingConfig.StandardIssueFormat = "{Series.CleanName}";
            _series.Name = "Linkin Park (1997)";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin.Park.1997");
        }

        [Test]
        public void should_replace_Series_Disambiguation()
        {
            _namingConfig.StandardIssueFormat = "{Series Disambiguation}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("US Rock Band");
        }

        [Test]
        public void should_replace_edition_space_Title()
        {
            _namingConfig.StandardIssueFormat = "{Issue Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Hybrid Theory");
        }

        [Test]
        public void should_replace_Series_Year()
        {
            _series.Metadata.Value.Year = 2003;
            _namingConfig.StandardIssueFormat = "{Series Year}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("2003");
        }

        [Test]
        public void should_replace_Issue_underscore_Title()
        {
            _namingConfig.StandardIssueFormat = "{Issue_Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Hybrid_Theory");
        }

        [Test]
        public void should_replace_Issue_dot_Title()
        {
            _namingConfig.StandardIssueFormat = "{Issue.Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Hybrid.Theory");
        }

        [Test]
        public void should_replace_Issue_dash_Title()
        {
            _namingConfig.StandardIssueFormat = "{Issue-Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Hybrid-Theory");
        }

        [Test]
        public void should_replace_ISSUE_TITLE_with_all_caps()
        {
            _namingConfig.StandardIssueFormat = "{ISSUE TITLE}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("HYBRID THEORY");
        }

        [Test]
        public void should_replace_ISSUE_TITLE_with_random_casing_should_keep_original_casing()
        {
            _namingConfig.StandardIssueFormat = "{iSsUe-tItLE}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(_issue.Title.Replace(' ', '-'));
        }

        [Test]
        public void should_replace_book_title_with_all_lower_case()
        {
            _namingConfig.StandardIssueFormat = "{issue title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("hybrid theory");
        }

        [Test]
        public void should_cleanup_Issue_Title()
        {
            _namingConfig.StandardIssueFormat = "{Series.CleanName}";
            _series.Name = "Hybrid Theory (2000)";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Hybrid.Theory.2000");
        }

        [Test]
        public void should_set_series()
        {
            _namingConfig.StandardIssueFormat = "{Issue SeriesGroup}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("SeriesGroup Title");
        }

        [Test]
        public void should_set_series_number()
        {
            _namingConfig.StandardIssueFormat = "{Issue SeriesPosition}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("1-2");
        }

        [Test]
        public void should_set_series_title()
        {
            _namingConfig.StandardIssueFormat = "{Issue SeriesTitle}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("SeriesGroup Title #1-2");
        }

        [Test]
        public void should_set_part_number()
        {
            _namingConfig.StandardIssueFormat = "{(PartNumber)}";
            _trackFile.PartCount = 2;
            _trackFile.Part = 1;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("(1)");
        }

        [Test]
        public void should_set_part_number_with_prefix()
        {
            _namingConfig.StandardIssueFormat = "{(ptPartNumber)}";
            _trackFile.PartCount = 2;
            _trackFile.Part = 1;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("(pt1)");
        }

        [Test]
        public void should_set_part_number_with_format()
        {
            _namingConfig.StandardIssueFormat = "{(ptPartNumber:00)}";
            _trackFile.PartCount = 2;
            _trackFile.Part = 1;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("(pt01)");
        }

        [Test]
        public void should_set_part_number_and_count_with_format()
        {
            _namingConfig.StandardIssueFormat = "{(ptPartNumber:00 of PartCount:00)}";
            _trackFile.PartCount = 2;
            _trackFile.Part = 1;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("(pt01 of 02)");
        }

        [Test]
        public void should_remove_part_token_for_single_files()
        {
            _namingConfig.StandardIssueFormat = "{(ptPartNumber:00 of PartCount:00)}";
            _trackFile.PartCount = 1;
            _trackFile.Part = 1;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("");
        }

        [Test]
        public void part_regex_should_not_gobble_others()
        {
            _namingConfig.StandardIssueFormat = "{Issue Title}{ (PartNumber)} - {Series Name}";
            _trackFile.Part = 1;
            _trackFile.PartCount = 2;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                .Should().Be("Hybrid Theory (1) - Linkin Park");
        }

        [Test]
        public void should_replace_quality_title()
        {
            _namingConfig.StandardIssueFormat = "{Quality Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("CBR");
        }

        [Test]
        public void should_replace_all_contents_in_pattern()
        {
            _namingConfig.StandardIssueFormat = "{Series Name} - {Issue Title} - [{Quality Title}]";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin Park - Hybrid Theory - [CBR]");
        }

        [Test]
        public void use_file_name_when_sceneName_is_null()
        {
            _namingConfig.RenameComics = false;
            _trackFile.Path = "Linkin Park - 06 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(Path.GetFileNameWithoutExtension(_trackFile.Path));
        }

        [Test]
        public void use_file_name_when_sceneName_is_not_null()
        {
            _namingConfig.RenameComics = false;
            _trackFile.Path = "Linkin Park - 06 - Test";
            _trackFile.SceneName = "SceneName";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(Path.GetFileNameWithoutExtension(_trackFile.Path));
        }

        [Test]
        public void use_path_when_sceneName_and_relative_path_are_null()
        {
            _namingConfig.RenameComics = false;
            _trackFile.Path = @"C:\Test\Unsorted\Series - 01 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(Path.GetFileNameWithoutExtension(_trackFile.Path));
        }

        [Test]
        public void should_should_replace_release_group()
        {
            _namingConfig.StandardIssueFormat = "{Release Group}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(_trackFile.ReleaseGroup);
        }

        [Test]
        public void should_be_able_to_use_original_title()
        {
            _series.Name = "Linkin Park";
            _namingConfig.StandardIssueFormat = "{Series Name} - {Original Title}";

            _trackFile.SceneName = "Linkin.Park.Meteora.320-LOL";
            _trackFile.Path = "30 Rock - 01 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin Park - Linkin.Park.Meteora.320-LOL");
        }

        [Test]
        public void should_replace_double_period_with_single_period()
        {
            _namingConfig.StandardIssueFormat = "{Series.Name}.{Issue.Title}";

            Subject.BuildComicFileName(new Series { Name = "In The Woods." }, new Issue { Title = "30 Rock", SeriesMetadata = new SeriesMetadata { Name = "Series" }, SeriesLinks = new List<SeriesGroupLink>() }, _trackFile)
                   .Should().Be("In.The.Woods.30.Rock");
        }

        [Test]
        public void should_replace_triple_period_with_single_period()
        {
            _namingConfig.StandardIssueFormat = "{Series.Name}.{Issue.Title}";

            Subject.BuildComicFileName(new Series { Name = "In The Woods..." }, new Issue { Title = "30 Rock", SeriesMetadata = new SeriesMetadata { Name = "Series" }, SeriesLinks = new List<SeriesGroupLink>() }, _trackFile)
                   .Should().Be("In.The.Woods.30.Rock");
        }

        [Test]
        public void should_include_affixes_if_value_not_empty()
        {
            _namingConfig.StandardIssueFormat = "{Series.Name}{_Issue.Title_}{Quality.Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin.Park_Hybrid.Theory_CBR");
        }

        [Test]
        public void should_not_include_affixes_if_value_empty()
        {
            _namingConfig.StandardIssueFormat = "{Series.Name}{_Issue.Title_}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin.Park_Hybrid.Theory");
        }

        [Test]
        public void should_remove_duplicate_non_word_characters()
        {
            _series.Name = "Venture Bros.";
            _namingConfig.StandardIssueFormat = "{Series.Name}.{Issue.Title}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Venture.Bros.Hybrid.Theory");
        }

        [Test]
        public void should_use_existing_filename_when_scene_name_is_not_available()
        {
            _namingConfig.RenameComics = true;
            _namingConfig.StandardIssueFormat = "{Original Title}";

            _trackFile.SceneName = null;
            _trackFile.Path = "existing.file.mkv";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(Path.GetFileNameWithoutExtension(_trackFile.Path));
        }

        [Test]
        public void should_be_able_to_use_only_original_title()
        {
            _series.Name = "30 Rock";
            _namingConfig.StandardIssueFormat = "{Original Title}";

            _trackFile.SceneName = "30.Rock.S01E01.xvid-LOL";
            _trackFile.Path = "30 Rock - S01E01 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("30.Rock.S01E01.xvid-LOL");
        }

        [Test]
        public void should_not_include_quality_proper_when_release_is_not_a_proper()
        {
            _namingConfig.StandardIssueFormat = "{Quality Title} {Quality Proper}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("CBR");
        }

        [Test]
        public void should_not_wrap_proper_in_square_brackets_when_not_a_proper()
        {
            _namingConfig.StandardIssueFormat = "{Series Name} - {Issue Title} [{Quality Title}] {[Quality Proper]}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin Park - Hybrid Theory [CBR]");
        }

        [Test]
        public void should_replace_quality_full_with_quality_title_only_when_not_a_proper()
        {
            _namingConfig.StandardIssueFormat = "{Series Name} - {Issue Title} [{Quality Full}]";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Linkin Park - Hybrid Theory [CBR]");
        }

        [TestCase(' ')]
        [TestCase('-')]
        [TestCase('.')]
        [TestCase('_')]
        public void should_trim_extra_separators_from_end_when_quality_proper_is_not_included(char separator)
        {
            _namingConfig.StandardIssueFormat = string.Format("{{Quality{0}Title}}{0}{{Quality{0}Proper}}", separator);

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("CBR");
        }

        [TestCase(' ')]
        [TestCase('-')]
        [TestCase('.')]
        [TestCase('_')]
        public void should_trim_extra_separators_from_middle_when_quality_proper_is_not_included(char separator)
        {
            _namingConfig.StandardIssueFormat = string.Format("{{Quality{0}Title}}{0}{{Quality{0}Proper}}{0}{{Issue{0}Title}}", separator);

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(string.Format("CBR{0}Hybrid{0}Theory", separator));
        }

        [Test]
        public void should_be_able_to_use_original_filename()
        {
            _series.Name = "30 Rock";
            _namingConfig.StandardIssueFormat = "{Series Name} - {Original Filename}";

            _trackFile.SceneName = "30.Rock.S01E01.xvid-LOL";
            _trackFile.Path = "30 Rock - S01E01 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("30 Rock - 30 Rock - S01E01 - Test");
        }

        [Test]
        public void should_be_able_to_use_original_filename_only()
        {
            _series.Name = "30 Rock";
            _namingConfig.StandardIssueFormat = "{Original Filename}";

            _trackFile.SceneName = "30.Rock.S01E01.xvid-LOL";
            _trackFile.Path = "30 Rock - S01E01 - Test";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("30 Rock - S01E01 - Test");
        }

        [Test]
        public void should_use_Panelarr_as_release_group_when_not_available()
        {
            _trackFile.ReleaseGroup = null;
            _namingConfig.StandardIssueFormat = "{Release Group}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be("Panelarr");
        }

        [TestCase("{Issue Title}{-Release Group}", "Hybrid Theory")]
        [TestCase("{Issue Title}{ Release Group}", "Hybrid Theory")]
        [TestCase("{Issue Title}{ [Release Group]}", "Hybrid Theory")]
        public void should_not_use_Panelarr_as_release_group_if_pattern_has_separator(string pattern, string expectedFileName)
        {
            _trackFile.ReleaseGroup = null;
            _namingConfig.StandardIssueFormat = pattern;

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(expectedFileName);
        }

        [TestCase("0SEC")]
        [TestCase("2HD")]
        [TestCase("IMMERSE")]
        public void should_use_existing_casing_for_release_group(string releaseGroup)
        {
            _trackFile.ReleaseGroup = releaseGroup;
            _namingConfig.StandardIssueFormat = "{Release Group}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(releaseGroup);
        }
    }
}
