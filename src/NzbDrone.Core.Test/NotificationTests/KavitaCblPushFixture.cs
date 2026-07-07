using System;
using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Notifications.Kavita;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class KavitaCblPushFixture : CoreTest<KavitaServiceProxy>
    {
        private readonly byte[] _cbl = Encoding.UTF8.GetBytes("<ReadingList/>");
        private KavitaSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new KavitaSettings
            {
                Host = "localhost",
                Port = 5000,
                ApiKey = "key"
            };

            GivenJson("plugin/authenticate", @"{""token"":""jwt"",""apiKey"":""key""}");
        }

        private void GivenJson(string pathContains, string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Execute(It.Is<HttpRequest>(r => r.Url.Path.Contains(pathContains))))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), Encoding.UTF8.GetBytes(json), HttpStatusCode.OK));
        }

        private void GivenNotFound(string pathContains)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Execute(It.Is<HttpRequest>(r => r.Url.Path.Contains(pathContains))))
                  .Throws(new HttpException(new HttpResponse(
                      new HttpRequest("http://localhost:5000/api/cbl/file-import"),
                      new HttpHeader(),
                      Array.Empty<byte>(),
                      HttpStatusCode.NotFound)));
        }

        [Test]
        public void should_use_the_multi_step_flow_on_kavita_09()
        {
            GivenJson("cbl/file-import", @"{""name"":""Knightfall"",""fileName"":""knightfall.cbl"",""provider"":1}");
            GivenJson("cbl/re-validate", @"{""fileName"":""knightfall.cbl"",""success"":2,""results"":[]}");
            GivenJson("cbl/finalize-import", @"{""cblName"":""Knightfall"",""success"":1,""isUpdate"":true,""readingListId"":9,
                ""successfulInserts"":[{""series"":""Batman"",""number"":""492"",""reason"":8},{""series"":""Batman"",""number"":""493"",""reason"":8}],
                ""results"":[{""series"":""Azrael"",""number"":""1"",""reason"":2}]}");

            var result = Subject.PushCbl(_settings, "Knightfall.cbl", _cbl);

            result.Updated.Should().BeTrue();
            result.MatchedCount.Should().Be(2);
            result.Unmatched.Should().BeEquivalentTo(new[] { "Azrael #1: series missing" });
        }

        [Test]
        public void should_send_the_jwt_as_bearer_token()
        {
            GivenJson("cbl/file-import", @"{""fileName"":""knightfall.cbl"",""provider"":1}");
            GivenJson("cbl/re-validate", @"{""success"":2,""results"":[]}");
            GivenJson("cbl/finalize-import", @"{""success"":2,""successfulInserts"":[],""results"":[]}");

            Subject.PushCbl(_settings, "Knightfall.cbl", _cbl);

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Url.Path.Contains("cbl/file-import") &&
                                                                 r.Headers["Authorization"] == "Bearer jwt")), Times.Once);
        }

        [Test]
        public void should_fall_back_to_single_shot_import_on_older_kavita()
        {
            GivenNotFound("cbl/file-import");
            GivenJson("cbl/import", @"{""cblName"":""Knightfall"",""success"":2,
                ""successfulInserts"":[{""series"":""Batman"",""number"":""492"",""reason"":8}],""results"":[]}");

            var result = Subject.PushCbl(_settings, "Knightfall.cbl", _cbl);

            result.Updated.Should().BeFalse();
            result.MatchedCount.Should().Be(1);
            result.Unmatched.Should().BeEmpty();

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Url.Path.Contains("cbl/re-validate"))), Times.Never);
        }

        [Test]
        public void should_throw_when_validation_fails()
        {
            GivenJson("cbl/file-import", @"{""fileName"":""bad.cbl"",""provider"":1}");
            GivenJson("cbl/re-validate", @"{""success"":0,""results"":[{""reason"":9}]}");

            Assert.Throws<KavitaException>(() => Subject.PushCbl(_settings, "bad.cbl", _cbl));

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Execute(It.Is<HttpRequest>(r => r.Url.Path.Contains("cbl/finalize-import"))), Times.Never);
        }
    }
}
