using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Metron.Resources;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.Metron
{
    [TestFixture]
    public class MetronApiClientFixture : CoreTest<MetronApiClient>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronUsername)
                .Returns("testuser");

            Mocker.GetMock<IConfigService>()
                .Setup(s => s.MetronPassword)
                .Returns("testpass");
        }

        private HttpResponse<T> BuildHttpResponse<T>(T resource, string url = "https://metron.cloud/api/test/")
            where T : new()
        {
            var json = JsonConvert.SerializeObject(resource);
            var rawResponse = new HttpResponse(
                new HttpRequest(url),
                new HttpHeader(),
                System.Text.Encoding.UTF8.GetBytes(json),
                System.Net.HttpStatusCode.OK);

            return new HttpResponse<T>(rawResponse);
        }

        private void GivenSearchResponse(List<MetronSeriesListItem> results, string nextUrl = null)
        {
            var pagedResponse = new MetronPagedResponse<MetronSeriesListItem>
            {
                Count = results.Count,
                Results = results,
                Next = nextUrl
            };

            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(s => s.Get<MetronPagedResponse<MetronSeriesListItem>>(
                    It.Is<HttpRequest>(r => r.Url.ToString().Contains("series/")),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()))
                .Returns(BuildHttpResponse(pagedResponse));
        }

        private void GivenIssueResponse(List<MetronIssueListItem> results, string nextUrl = null)
        {
            var pagedResponse = new MetronPagedResponse<MetronIssueListItem>
            {
                Count = results.Count,
                Results = results,
                Next = nextUrl
            };

            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(s => s.Get<MetronPagedResponse<MetronIssueListItem>>(
                    It.Is<HttpRequest>(r => r.Url.ToString().Contains("issue/")),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()))
                .Returns(BuildHttpResponse(pagedResponse));
        }

        private void GivenIssuePageResponse(string url, List<MetronIssueListItem> results, string nextUrl = null)
        {
            var pagedResponse = new MetronPagedResponse<MetronIssueListItem>
            {
                Count = results.Count,
                Results = results,
                Next = nextUrl
            };

            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(s => s.Get<MetronPagedResponse<MetronIssueListItem>>(
                    It.Is<HttpRequest>(r => r.Url.ToString() == url),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()))
                .Returns(BuildHttpResponse(pagedResponse, url));
        }

        [Test]
        public void should_search_series_with_name_param()
        {
            var items = new List<MetronSeriesListItem>
            {
                new MetronSeriesListItem { Id = 1, Name = "Batman" }
            };

            GivenSearchResponse(items);

            var result = Subject.SearchSeries("Batman");

            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Batman");

            Mocker.GetMock<ICachedHttpResponseService>()
                .Verify(v => v.Get<MetronPagedResponse<MetronSeriesListItem>>(
                    It.Is<HttpRequest>(r => r.Url.ToString().Contains("series/")),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()), Times.Once());
        }

        [Test]
        public void should_paginate_all_pages_for_issues()
        {
            var page1Items = new List<MetronIssueListItem>
            {
                new MetronIssueListItem { Id = 1, Number = "1" },
                new MetronIssueListItem { Id = 2, Number = "2" }
            };

            var page2Items = new List<MetronIssueListItem>
            {
                new MetronIssueListItem { Id = 3, Number = "3" }
            };

            var page2Url = "https://metron.cloud/api/issue/?series_id=100&page=2";

            GivenIssueResponse(page1Items, page2Url);
            GivenIssuePageResponse(page2Url, page2Items);

            var result = Subject.GetIssuesBySeries(100);

            result.Should().HaveCount(3);
            result[0].Number.Should().Be("1");
            result[2].Number.Should().Be("3");
        }

        [Test]
        public void should_call_cached_http_service_for_each_request()
        {
            var items = new List<MetronSeriesListItem>
            {
                new MetronSeriesListItem { Id = 1, Name = "Spider-Man" }
            };

            GivenSearchResponse(items);

            Subject.SearchSeries("Spider-Man");

            Mocker.GetMock<ICachedHttpResponseService>()
                .Verify(v => v.Get<MetronPagedResponse<MetronSeriesListItem>>(
                    It.IsAny<HttpRequest>(),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()), Times.Once());
        }
    }
}
