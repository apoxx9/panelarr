using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Notifications.Komga;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class KomgaPushFixture : CoreTest<KomgaProxy>
    {
        private const string MatchResponse = @"{
            ""readListMatch"": {""name"":""Knightfall"",""errorCode"":""""},
            ""requests"": [
                {""request"":{""series"":[""Batman""],""number"":""492""},
                 ""matches"":[{""series"":{""seriesId"":""s1"",""title"":""Batman""},""books"":[{""bookId"":""b1"",""number"":""492"",""title"":""""}]}]},
                {""request"":{""series"":[""Detective Comics""],""number"":""659""},""matches"":[]},
                {""request"":{""series"":[""Azrael""],""number"":""1""},
                 ""matches"":[{""series"":{""seriesId"":""s2"",""title"":""Azrael""},""books"":[{""bookId"":""b2"",""number"":""1"",""title"":""""}]},
                              {""series"":{""seriesId"":""s3"",""title"":""Azrael (2009)""},""books"":[{""bookId"":""b3"",""number"":""1"",""title"":""""}]}]}
            ],
            ""errorCode"": """"}";

        private readonly byte[] _cbl = Encoding.UTF8.GetBytes("<ReadingList/>");
        private KomgaSettings _settings;
        private List<HttpRequest> _requests;

        [SetUp]
        public void Setup()
        {
            _settings = new KomgaSettings
            {
                BaseUrl = "http://localhost:25600",
                Username = "user",
                Password = "pass"
            };

            _requests = new List<HttpRequest>();

            GivenResponse(r => r.Url.Path.EndsWith("/readlists/match/comicrack"), MatchResponse);
            GivenResponse(r => r.Url.Path.EndsWith("/readlists") && r.Method == HttpMethod.Get, @"{""content"":[]}");
            GivenResponse(r => r.Url.Path.EndsWith("/readlists") && r.Method == HttpMethod.Post, "{}");
            GivenResponse(r => r.Url.Path.Contains("/readlists/") && r.Method == HttpMethod.Patch, string.Empty);
        }

        private void GivenResponse(System.Func<HttpRequest, bool> predicate, string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Execute(It.Is<HttpRequest>(r => predicate(r))))
                  .Returns<HttpRequest>(r =>
                  {
                      _requests.Add(r);
                      return new HttpResponse(r, new HttpHeader(), Encoding.UTF8.GetBytes(json), HttpStatusCode.OK);
                  });
        }

        [Test]
        public void should_create_a_readlist_from_unambiguous_matches()
        {
            var result = Subject.PushCbl(_settings, "Knightfall", _cbl);

            result.Updated.Should().BeFalse();
            result.MatchedCount.Should().Be(1);
            result.Unmatched.Should().BeEquivalentTo(new[]
            {
                "Detective Comics #659: no match",
                "Azrael #1: ambiguous (2 series match)"
            });

            var create = _requests.Find(r => r.Method == HttpMethod.Post && r.Url.Path.EndsWith("/readlists"));

            create.Should().NotBeNull();

            var body = Encoding.UTF8.GetString(create.ContentData);

            body.Should().Contain("\"name\": \"Knightfall\"");
            body.Should().Contain("\"ordered\": true");
            body.Should().Contain("\"b1\"");
            body.Should().NotContain("\"b2\"");
        }

        [Test]
        public void should_update_an_existing_readlist_with_the_same_name()
        {
            GivenResponse(r => r.Url.Path.EndsWith("/readlists") && r.Method == HttpMethod.Get,
                          @"{""content"":[{""id"":""rl1"",""name"":""Knightfall""}]}");

            var result = Subject.PushCbl(_settings, "Knightfall", _cbl);

            result.Updated.Should().BeTrue();

            var patch = _requests.Find(r => r.Method == HttpMethod.Patch);

            patch.Should().NotBeNull();
            patch.Url.Path.Should().EndWith("/readlists/rl1");
            Encoding.UTF8.GetString(patch.ContentData).Should().Contain("\"b1\"");
        }

        [Test]
        public void should_throw_when_nothing_matches()
        {
            GivenResponse(r => r.Url.Path.EndsWith("/readlists/match/comicrack"),
                          @"{""readListMatch"":{""name"":""Knightfall"",""errorCode"":""""},""requests"":[{""request"":{""series"":[""Batman""],""number"":""492""},""matches"":[]}],""errorCode"":""""}");

            Assert.Throws<KomgaException>(() => Subject.PushCbl(_settings, "Knightfall", _cbl));
        }

        [Test]
        public void should_throw_on_a_match_error_code()
        {
            GivenResponse(r => r.Url.Path.EndsWith("/readlists/match/comicrack"),
                          @"{""readListMatch"":{""name"":"""",""errorCode"":""ERR_1015""},""requests"":[],""errorCode"":""""}");

            Assert.Throws<KomgaException>(() => Subject.PushCbl(_settings, "Knightfall", _cbl));
        }
    }
}
