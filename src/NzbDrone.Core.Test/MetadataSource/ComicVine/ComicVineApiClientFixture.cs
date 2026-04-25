using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.ComicVine
{
    [TestFixture]
    public class ComicVineApiClientFixture : CoreTest<ComicVineApiClient>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.ComicVineApiKey)
                .Returns("test-cv-api-key");
        }

        private HttpResponse<T> BuildHttpResponse<T>(T resource, string url = "https://comicvine.gamespot.com/api/search/")
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

        private void GivenSearchResponse(List<ComicVineVolumeSummary> results)
        {
            var response = new ComicVineResponse<List<ComicVineVolumeSummary>>
            {
                StatusCode = 1,
                Error = "OK",
                Results = results
            };

            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(s => s.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(
                    It.IsAny<HttpRequest>(),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()))
                .Returns(BuildHttpResponse(response));
        }

        [Test]
        public void should_search_with_correct_endpoint_and_resource_type()
        {
            var volumes = new List<ComicVineVolumeSummary>
            {
                new ComicVineVolumeSummary { Id = 1, Name = "Batman", StartYear = "2016", CountOfIssues = 50 }
            };

            GivenSearchResponse(volumes);

            var result = Subject.SearchSeries("Batman");

            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Batman");

            Mocker.GetMock<ICachedHttpResponseService>()
                .Verify(v => v.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(
                    It.Is<HttpRequest>(r =>
                        r.Url.ToString().Contains("search") &&
                        r.Url.ToString().Contains("resources=volume") &&
                        r.Url.ToString().Contains("limit=25")),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()), Times.Once());
        }

        [Test]
        public void should_include_api_key_in_request()
        {
            GivenSearchResponse(new List<ComicVineVolumeSummary>());

            Subject.SearchSeries("Test");

            Mocker.GetMock<ICachedHttpResponseService>()
                .Verify(v => v.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(
                    It.Is<HttpRequest>(r => r.Url.ToString().Contains("api_key=test-cv-api-key")),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()), Times.Once());
        }

        [Test]
        public void should_return_empty_list_when_no_results()
        {
            GivenSearchResponse(new List<ComicVineVolumeSummary>());

            var result = Subject.SearchSeries("NonExistentComic");

            result.Should().BeEmpty();
        }

        [Test]
        public void should_return_empty_list_when_results_null()
        {
            var response = new ComicVineResponse<List<ComicVineVolumeSummary>>
            {
                StatusCode = 1,
                Error = "OK",
                Results = null
            };

            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(s => s.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(
                    It.IsAny<HttpRequest>(),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()))
                .Returns(BuildHttpResponse(response));

            var result = Subject.SearchSeries("Test");

            result.Should().BeEmpty();
        }
    }
}
