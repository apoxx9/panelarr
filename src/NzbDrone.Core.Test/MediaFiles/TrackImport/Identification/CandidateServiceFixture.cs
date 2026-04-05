using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class CandidateServiceFixture : CoreTest<CandidateService>
    {
        [Test]
        public void should_not_throw_on_search_exception()
        {
            Mocker.GetMock<ISearchForNewIssue>()
                .Setup(s => s.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), true))
                .Throws(new Exception("Bad search"));

            var edition = new LocalEdition
            {
                LocalIssues = new List<LocalIssue>
                {
                    new LocalIssue
                    {
                        FileTrackInfo = new ParsedTrackInfo
                        {
                            Series = new List<string> { "Series" },
                            IssueTitle = "Issue"
                        }
                    }
                }
            };

            Subject.GetRemoteCandidates(edition, null).Should().BeEmpty();
        }
    }
}
