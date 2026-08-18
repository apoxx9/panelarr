using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ParserFixture : CoreTest
    {
        private Series _series = new Series();
        private List<Issue> _books = new List<Issue> { new Issue() };

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                .CreateNew()
                .Build();
            _books = Builder<List<Issue>>
                .CreateNew()
                .Build();
        }

        private void GivenSearchCriteria(string seriesName, string bookTitle)
        {
            _series.Name = seriesName;
            var a = new Issue
            {
                Title = bookTitle
            };
            _books.Add(a);
        }

        [TestCase("Bad Format", "badformat")]
        public void should_parse_series_name(string postTitle, string title)
        {
            var result = Parser.Parser.ParseSeriesName(postTitle).CleanSeriesName();
            result.Should().Be(title.CleanSeriesName());
        }

        [Test]
        public void should_remove_accents_from_title()
        {
            const string title = "Carniv\u00E0le";

            title.CleanSeriesName().Should().Be("carnivale");
        }

        [TestCase("Batman 001 (Variant Cover)", "Batman 001")]
        [TestCase("Batman 001 (Convention Exclusive Edition)", "Batman 001")]
        [TestCase("Batman 001 [Director's Cut Edition]", "Batman 001")]
        [TestCase("X-Men 042 [Special Edition]", "X-Men 042")]
        [TestCase("Sweet Dreams (Issue)", "Sweet Dreams")]
        [TestCase("Saga 067 (Limited Edition)", "Saga 067")]
        [TestCase("Random Issue Title (Preview Copy)", "Random Issue Title")]
        [TestCase("Sandman 001 (2024 Remastered)", "Sandman 001")]
        [TestCase("Limited Edition", "Limited Edition")]
        [Ignore("Pending comic parser rewrite — test data updated to comic patterns")]
        public void should_remove_common_tags_from_book_title(string title, string correct)
        {
            var result = Parser.Parser.CleanIssueTitle(title);
            result.Should().Be(correct);
        }

        [TestCase("Batman 001 (Variant Cover)", "Batman 001")]
        [TestCase("X-Men 042 [Special Edition]", "X-Men 042")]
        [TestCase("Batman Beyond (single)", "Batman Beyond")]
        [TestCase("Dark Knights of Steel (Ft. Tom Taylor, Yasmine Putri & Joshua Williamson)", "Dark Knights of Steel")]
        [TestCase("Immortal X-Men (Feat. Kieron Gillen)", "Immortal X-Men")]
        [TestCase("Science Fiction/Double Feature", "Science Fiction/Double Feature")]
        [TestCase("Dancing Feathers", "Dancing Feathers")]
        [Ignore("Pending comic parser rewrite — test data updated to comic patterns")]
        public void should_remove_common_tags_from_track_title(string title, string correct)
        {
            var result = Parser.Parser.CleanTrackTitle(title);
            result.Should().Be(correct);
        }

        [TestCase("Image Comics - Saga : 02 Road From Hell [v04].cbz")]
        public void should_clean_up_invalid_path_characters(string postTitle)
        {
            Parser.Parser.ParseIssueTitle(postTitle);
        }

        [TestCase("[scnzbefnet][509103] Batman - Dark Knight Returns (2024) (Digital)", "Batman")]
        public void should_remove_request_info_from_title(string postTitle, string title)
        {
            Parser.Parser.ParseIssueTitle(postTitle).SeriesName.Should().Be(title);
        }

        [TestCase("002 Unchained.cbz")] // This isn't valid on any regex we have. We must always have an series
        [TestCase("Amazing Spider-Man - 002 - Title.cbz")] // This isn't valid on any regex we have. We don't support Series - Track - TrackName
        [Ignore("Ignore Test until track parsing rework")]
        public void should_parse_quality_from_extension(string title)
        {
            Parser.Parser.ParseIssueTitle(title).Quality.Quality.Should().NotBe(Quality.Unknown);
            Parser.Parser.ParseIssueTitle(title).Quality.QualityDetectionSource.Should().Be(QualityDetectionSource.Extension);
        }

        [TestCase("Batman 001 (2024) (Digital) (Zone-Empire).cbz", "Batman", "001")]
        [TestCase("Amazing Spider-Man v6 042 (2024) (Digital) (Shan-Empire).cbz", "Amazing Spider-Man", "042")]

        //[TestCase("Amazing Spider-Man v6 042 (2024) (Digital) (Shan-Empire).cbz", "Amazing Spider-Man", "042")]
        [TestCase("X-Men.Annual.001.(2024).(Digital).(Shan-Empire).cbz", "X-Men", "Annual 001")]
        [TestCase("Saga 067 (2024) (c2c) (Phillywilly-Empire).cbr", "Saga", "067")]
        [TestCase("The Walking Dead Deluxe 097 (2024) (digital) (Son of Ultron-Empire).cbz", "The Walking Dead Deluxe", "097")]

        //[TestCase("The Walking Dead Deluxe 097 (2024) (digital).cbz", "The Walking Dead Deluxe", "097")]
        [TestCase("Invincible 001 (2003) (Digital) (Minutemen-Midas).cbz", "Invincible", "001")]

        //[TestCase("Invincible 144 (2017) (Digital) (Minutemen-Midas).cbz", "Invincible", "144")]
        [TestCase("Spawn 350 (2024) (Digital) (Zone-Empire).cbz", "Spawn", "350")]

        //[TestCase("Spawn 001 (1992) (Digital).cbz", "Spawn", "001")]
        [TestCase("One Piece 001 (1997) (Digital) (aKraa).cbz", "One Piece", "001")]
        [TestCase("Teenage Mutant Ninja Turtles 001 (2024) (Digital).cbz", "Teenage Mutant Ninja Turtles", "001")]
        [TestCase("Batman - Dark Knight Returns (2024) (Digital) (Zone-Empire).cbz", "Batman", "Dark Knight Returns")]
        [TestCase("Immortal X-Men 018 (2024) (Digital) (Zone-Empire).cbz", "Immortal X-Men", "018")]
        [TestCase("DC Comics - Batman 001 (2024) (Digital).cbz", "DC Comics - Batman", "001")]
        [TestCase("Daredevil 001 (2024) (Digital) (Zone-Empire).cbz", "Daredevil", "001")]
        [TestCase("Hellboy Omnibus v01 - Seed of Destruction (2018) (Digital) (Mephisto-Empire).cbz", "Hellboy Omnibus", "Seed of Destruction")]
        [TestCase("Sandman Universe - Nightmare Country 001 (2024) (Digital).cbz", "Sandman Universe - Nightmare Country", "001")]
        [TestCase("Wonder Woman 001 (2024) (Digital) (Shan-Empire).cbz", "Wonder Woman", "001")]
        [TestCase("Thor 001 (2024) (Digital) (Zone-Empire).cbz", "Thor", "001")]
        [TestCase("Green Lantern 001 (2024) (Digital) (Phillywilly-Empire).cbz", "Green Lantern", "001")]
        [TestCase("Usagi Yojimbo 001 (2024) (Digital).cbz", "Usagi Yojimbo", "001")]
        [TestCase("Bone - Complete Collection (2004) (Digital) (Minutemen-Midas).cbz", "Bone", "Complete Collection")]
        [TestCase("Black Hammer 001 (2016) (Digital) (Mephisto-Empire).cbz", "Black Hammer", "001")]
        [TestCase("Saga Compendium v01 (2019) (Digital) (Shan-Empire).cbz", "Saga Compendium", "Complete Collection", true)]
        [TestCase("Invincible - Complete Compendium 2003-2018 (Digital)", "Invincible", "Complete Collection", true)]
        [TestCase("Walking Dead - Complete Series 2003-2019 (168 issues)(digital)", "Walking Dead", "Complete Collection", true)]
        [TestCase("Preacher 001 (1995) (Digital) (Minutemen-Midas).cbz", "Preacher", "001")]
        [TestCase("Fables-2002-The Last Castle-Digital", "Fables", "The Last Castle")]
        [TestCase("Fables-The Last Castle-2002-Digital", "Fables", "The Last Castle")]

        // GetComics
        [TestCase("Batman 001 (2024) (Webrip) (Zone-Empire).cbz", "Batman", "001")]
        [TestCase("X-Men 001 (2024) (Digital) (Shan-Empire).cbz", "X-Men", "001")]
        [TestCase("Saga 067 (2024) (c2c) by Brian K Vaughan [cbr]", "Brian K Vaughan", "Saga 067")]
        [TestCase("The Sandman Omnibus v01 by Neil Gaiman [cbz]", "Neil Gaiman", "The Sandman Omnibus v01")]
        [TestCase("Y The Last Man by Brian K Vaughan [cbz]", "Brian K Vaughan", "Y The Last Man")]

        // comictracker
        [TestCase("(Superhero) [Digital] Batman - Hush - 2024, CBZ (pages), lossless", "Batman", "Hush")]
        [TestCase("(Manga / Shonen) One Piece - Romance Dawn - 2024, CBZ, Digital", "One Piece", "Romance Dawn")]
        [TestCase("(Horror / Vertigo) [Digital] Swamp Thing - Saga of the Swamp Thing - 2024, CBZ (pages), lossless", "Swamp Thing", "Saga of the Swamp Thing")]

        //[TestCase("(Superhero) Batman(Frank Miller) - Compendium, 23 issues - 1986-2001, CBZ(digital), lossless")]
        //[TestCase("(Superhero) Spider-Man(Todd McFarlane) - Compendium(14 vols) [1990-2010], CBZ(digital), lossless")]
        [TestCase("(Superhero) [Digital] X-Men - Compendium - 1991-2015 (36 releases, 32 vols), CBZ(digital), lossless", "X-Men", "Complete Collection", true)]

        //[TestCase("(Superhero / Action) Deadpool - One of the Sta(2014) + Ocean(2014), CBZ, Digital", "Deadpool", "")]
        [TestCase("(Superhero) Spawn - Compendium(46 vols) [1992 - 2024], CBZ(digital), lossless", "Spawn", "Complete Collection", true)]
        [TestCase("(Superhero) [Digital] Savage Dragon - Compendium(6 vols), 1992-2016, CBZ(digital), lossless", "Savage Dragon", "Complete Collection", true)]
        [TestCase("Saga - The now now - 2024 [CBZ]", "Saga", "The now now")]

        //Regex Works on below, but ParseIssueMatchCollection cleans the "..." and converts it to spaces
        // [TestCase("Batman - ...The Long Halloween (2024) [CBZ Digital]", "Batman", "...The Long Halloween")]
        public void should_parse_series_name_and_book_title(string postTitle, string name, string title, bool isCollection = false)
        {
            var parseResult = Parser.Parser.ParseIssueTitle(postTitle);
            parseResult.SeriesName.Should().Be(name);
            parseResult.IssueTitle.Should().Be(title);
            parseResult.IsCollection.Should().Be(isCollection);
        }

        [TestCase("Green Lantern Corps #18 (2026)", "Green Lantern Corps", 2026)]
        [TestCase("Batman #100 (2016)", "Batman", 2016)]
        [TestCase("Batman #100", "Batman", 0)]
        public void should_capture_year_from_simple_comic_title(string postTitle, string name, int year)
        {
            var parseResult = Parser.Parser.ParseIssueTitle(postTitle);
            parseResult.SeriesName.Should().Be(name);
            parseResult.SeriesTitleInfo.Year.Should().Be(year);
        }

        [TestCase("Walking Dead - Walking Dead Digital")]
        [TestCase("Walking Dead Walking Dead Digital")]
        [TestCase("WaLkInG DeAd Walking DeAd Digital")]
        [TestCase("Walking Dead Digital Walking Dead")]
        [TestCase("Walking.Dead-Digital-Walking.Dead")]
        [TestCase("Walking_Dead-Digital-Walking_Dead")]
        public void should_parse_series_name_and_book_title_by_search_criteria(string releaseTitle)
        {
            GivenSearchCriteria("Walking Dead", "Walking Dead");
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);
            parseResult.SeriesName.ToLowerInvariant().Should().Be("walking dead");
            parseResult.IssueTitle.ToLowerInvariant().Should().Be("walking dead");
        }

        [TestCase("Iron Fist Epic Collection Vol. 01 - The Fury of Iron Fist (2015) by Marvel Comics [ENG / CBR]")]
        [TestCase("Iron Fist Epic Collection v01 - The Fury of Iron Fist (2015)")]
        [TestCase("Iron Fist Epic Collection Volume 1 - The Fury of Iron Fist")]
        public void should_match_collected_edition_with_volume_infix_by_search_criteria(string releaseTitle)
        {
            // Release convention is "<line> Vol. NN - <subtitle>" while the
            // library series is "<line>: <subtitle>" - the volume marker
            // splits the series name and must not defeat the match
            GivenSearchCriteria("Iron Fist Epic Collection: The Fury of Iron Fist", "Volume 1");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().NotBeNull();
            parseResult.SeriesName.ToLowerInvariant().Should().Contain("iron fist epic collection");
        }

        [TestCase("Iron Fist Epic Collection Vol. 03 - Something Else (2015)")]
        public void should_not_match_collected_edition_when_volume_number_disagrees(string releaseTitle)
        {
            GivenSearchCriteria("Iron Fist Epic Collection: The Fury of Iron Fist", "Volume 1");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().BeNull();
        }

        [TestCase("Amazing Spider-Man Epic Collection Vol. 11 – Nine Lives Has The Black Cat (2025) (Digital) (Asgard-Empire)")]
        [TestCase("Amazing Spider-Man Epic Collection Vol 11: Nine Lives Has the Black Cat by Marvel Comics [ENG / CBR CBZ]")]
        public void should_match_collected_edition_when_line_volume_agrees_with_issue_title(string releaseTitle)
        {
            // Per-volume series number their sole issue 1 while the release
            // carries the line volume - the issue TITLE holds that volume
            GivenSearchCriteria("Amazing Spider-Man Epic Collection: Nine Lives Has the Black Cat", "Volume 11");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().NotBeNull();
            parseResult.SeriesName.ToLowerInvariant().Should().Contain("amazing spider-man epic collection");

            // the volume marker must not leak into the series name - the
            // library lookup cleans it to a name no series has
            parseResult.SeriesName.ToLowerInvariant().Should().NotContain("vol");
        }

        [TestCase("Amazing Spider-Man Epic Collection Vol. 11 – Nine Lives Has The Black Cat (2025)")]
        public void fuzzy_matched_release_without_source_tag_should_be_archive_not_unknown(string releaseTitle)
        {
            GivenSearchCriteria("Amazing Spider-Man Epic Collection: Nine Lives Has the Black Cat", "Volume 11");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().NotBeNull();
            parseResult.Quality.Quality.Should().Be(NzbDrone.Core.Qualities.Quality.Archive);
        }

        [TestCase("Amazing Spider-Man Epic Collection Vol. 11 - Nine Lives Has The Black Cat (2025)")]
        public void should_match_collected_edition_by_subtitle_alone_for_a_single_target_issue(string releaseTitle)
        {
            // Issue title carries no volume number at all - the subtitle split
            // by the marker still uniquely identifies the sole target issue
            GivenSearchCriteria("Amazing Spider-Man Epic Collection: Nine Lives Has the Black Cat", "Nine Lives Has the Black Cat");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().NotBeNull();
        }

        [TestCase("Captain America Epic Collection Vol. 1: Captain America Lives Again by Marvel Comics [ENG / CBR]")]
        public void should_not_match_another_volumes_release_when_the_subtitle_disagrees(string releaseTitle)
        {
            // Observed cross-grab: searching "The Captain" accepted the
            // Lives Again release because lenient fuzzy matching drifted the
            // subtitle onto "...Captain America...". The stand-ins demand the
            // full series name at high confidence.
            GivenSearchCriteria("Captain America Epic Collection: The Captain", "Volume 14");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().BeNull();
        }

        [TestCase("Captain America Epic Collection Vol. 1: Captain America Lives Again by Marvel Comics [ENG / CBR]")]
        public void should_match_the_release_whose_subtitle_agrees(string releaseTitle)
        {
            GivenSearchCriteria("Captain America Epic Collection: Captain America Lives Again", "Volume 14");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().NotBeNull();
        }

        [TestCase("Saga Vol. 03 (2015) (Digital)")]
        public void should_not_match_an_ongoings_trade_as_its_first_issue(string releaseTitle)
        {
            // "Saga" matches without stripping the marker, so the
            // subtitle-identifies-the-volume fallback must not fire
            GivenSearchCriteria("Saga", "#1");
            _books[0].IssueNumber = "1";

            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);

            parseResult.Should().BeNull();
        }

        [TestCase("Walking Dead - Complete Series 2003-2019 (168 issues)(digital)", 2003, 2019)]
        [TestCase("(Superhero) Spawn - Compendium(46 vols) [1992 - 2024]", 1992, 2024)]
        [TestCase("Invincible - Complete Compendium 2003-2018 (144 issues)(digital)", 2003, 2018)]
        [TestCase("Preacher - Complete Omnibus [1995] [Anthology]", 0, 1995)]
        [TestCase("Saga Compendium v01 Completa Digital @256", 0, 0)]
        public void should_parse_year_or_year_range_from_collection(string releaseTitle, int startyear, int endyear)
        {
            var parseResult = Parser.Parser.ParseIssueTitle(releaseTitle);
            parseResult.IsCollection.Should().BeTrue();
            parseResult.CollectionStart.Should().Be(startyear);
            parseResult.CollectionEnd.Should().Be(endyear);
        }

        [TestCase("Akira", "Akira", "Walking Dead  Walking Dead Digital")]
        [TestCase("Anthony Horowitz", "Oblivion", "The Elder Scrolls IV Oblivion+Expansions")]
        [TestCase("Danielle Steel", "Zoya", "DanielleSteelZoya.zip")]
        [TestCase("Stephen King", "It", "Stephen Kingston - Spirit Doll (retail) (azw3)")]
        [TestCase("Stephen King", "It", "Stephen_Cleobury-The_Music_of_Kings_Choral_Favourites_from_Cambridge-WEB-2019-ENRiCH")]
        [TestCase("Stephen King", "Guns", "Stephen King - The Gunslinger: Dark Tower 1 MP3")]
        [TestCase("Rick Riordan", "An Interview with Rick Riordan", "AnInterviewwithRickRiordan_ep6")]
        public void should_not_parse_series_name_and_book_title_by_incorrect_search_criteria(string searchSeries, string searchIssue, string report)
        {
            GivenSearchCriteria(searchSeries, searchIssue);
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(report, _series, _books);
            parseResult.Should().BeNull();
        }

        [TestCase("George R.R. Martin", "The Hero", "The Hero George R R Martin", "George R R Martin", "The Hero")]
        [TestCase("James Herbert", "48", "James Hertbert Collection/'48 - James Herbert (epub)", "James Herbert", "48")]
        public void should_parse_with_search_criteria(string searchSeries, string searchIssue, string report, string expectedSeries, string expectedIssue)
        {
            GivenSearchCriteria(searchSeries, searchIssue);
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(report, _series, _books);

            parseResult.SeriesName.Should().Be(expectedSeries);
            parseResult.IssueTitle.Should().Be(expectedIssue);
        }

        [TestCase("Jeff Lemire", "I See Fire", "Jeff Lemire I See Fire[Micbz.eu].cbz CBZ")]
        [TestCase("Jeff Lemire", "Divide", "Jeff Lemire   ? Divide CBZ")]
        [TestCase("Jeff Lemire", "+", "Jeff Lemire + CBZ")]

        //[TestCase("Glasvegas", @"EUPHORIC /// HEARTBREAK \\\", @"EUPHORIC /// HEARTBREAK \\\ FLAC")] // slashes not being escaped properly
        [TestCase("Descender", "?", "Descender ? CBZ")]
        [TestCase("Thorgal", "BŁYSK", "Thorgal - BŁYSK CBZ")]
        public void should_escape_books(string series, string issue, string releaseTitle)
        {
            GivenSearchCriteria(series, issue);
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);
            parseResult.IssueTitle.Should().Be(issue);
        }

        [TestCase("???", "Issue", "??? Issue CBZ")]
        [TestCase("+", "Issue", "+ Issue CBZ")]
        [TestCase(@"/\", "Issue", @"/\ Issue CBZ")]
        [TestCase("+44", "When Your Heart Stops Beating", "+44 When Your Heart Stops Beating CBZ")]
        public void should_escape_authors(string series, string issue, string releaseTitle)
        {
            GivenSearchCriteria(series, issue);
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);
            parseResult.SeriesName.Should().Be(series);
        }

        [TestCase("Ren\u00E9 Goscinny", "Ren\u00E9 Goscinny", @"Rene Goscinny Rene Goscinny Digital CBZ 2003 PERFECT")]
        public void should_match_with_accent_in_author_and_book(string series, string issue, string releaseTitle)
        {
            GivenSearchCriteria(series, issue);
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(releaseTitle, _series, _books);
            parseResult.SeriesName.Should().Be("Rene Goscinny");
            parseResult.IssueTitle.Should().Be("Rene Goscinny");
        }

        [Test]
        public void should_find_result_if_multiple_books_in_searchcriteria()
        {
            GivenSearchCriteria("Ren\u00E9 Goscinny", "The Mansions of the Gods");
            GivenSearchCriteria("Ren\u00E9 Goscinny", "Ren\u00E9 Goscinny");
            GivenSearchCriteria("Ren\u00E9 Goscinny", "Asterix the Gaul");
            GivenSearchCriteria("Ren\u00E9 Goscinny", "Asterix and Cleopatra");
            GivenSearchCriteria("Ren\u00E9 Goscinny", "Asterix in Britain");
            var parseResult = Parser.Parser.ParseIssueTitleWithSearchCriteria(
                "Rene Goscinny Asterix and Cleopatra (Deluxe Special Edition) Digital CBZ 2024 UNDERTONE iNT", _series, _books);
            parseResult.SeriesName.Should().Be("Rene Goscinny");
            parseResult.IssueTitle.Should().Be("Asterix and Cleopatra");
        }

        [TestCase("Tom Clancy", "Tom Clancy: Ghost Protocol", "Ghost Protocol", "")]
        [TestCase("Andrew Steele", "Ageless: The New Science of Getting Older Without Getting Old", "Ageless", "The New Science of Getting Older Without Getting Old")]
        [TestCase("Series", "Title (Subtitle with spaces)", "Title", "Subtitle with spaces")]
        [TestCase("Series", "Title (Unabridged)", "Title (Unabridged)", "")]
        [TestCase("Series", "asdf)(", "asdf)(", "")]
        public void should_split_title_correctly(string series, string issue, string expectedTitle, string expectedSubtitle)
        {
            var (title, subtitle) = issue.SplitIssueTitle(series);

            title.Should().Be(expectedTitle);
            subtitle.Should().Be(expectedSubtitle);
        }
    }
}
