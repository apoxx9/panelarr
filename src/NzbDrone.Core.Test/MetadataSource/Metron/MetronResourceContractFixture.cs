using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.Metron.Resources;

namespace NzbDrone.Core.Test.MetadataSource.Metron
{
    // Locks our bindings to Metron's actual v1.0 API field names
    // (github.com/Metron-Project/metron, api/v1_0/serializers). Round-tripping
    // the C# models cannot catch a wrong [JsonProperty] name — raw JSON can.
    [TestFixture]
    public class MetronResourceContractFixture
    {
        [Test]
        public void issue_list_item_should_bind_metron_list_serializer_fields()
        {
            const string json = @"{
                ""id"": 1234,
                ""series"": { ""name"": ""Saga"", ""volume"": 1, ""year_began"": 2012 },
                ""number"": ""3"",
                ""issue"": ""Saga (2012) #3"",
                ""cover_date"": ""2012-05-01"",
                ""store_date"": ""2012-05-16"",
                ""image"": ""https://static.metron.cloud/media/issue/2019/01/01/saga-3.jpg"",
                ""cover_hash"": ""abc123"",
                ""modified"": ""2023-01-01T00:00:00Z""
            }";

            var item = JsonConvert.DeserializeObject<MetronIssueListItem>(json);

            item.Id.Should().Be(1234);
            item.Number.Should().Be("3");
            item.DisplayName.Should().Be("Saga (2012) #3");
            item.CoverDate.Should().HaveValue();
            item.Image.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void issue_detail_should_bind_metron_read_serializer_fields()
        {
            const string json = @"{
                ""id"": 1234,
                ""number"": ""3"",
                ""title"": null,
                ""name"": [""Chapter Three""],
                ""cover_date"": ""2012-05-01"",
                ""store_date"": ""2012-05-16"",
                ""page"": 32,
                ""desc"": ""The family is on the run."",
                ""image"": ""https://static.metron.cloud/media/issue/2019/01/01/saga-3.jpg"",
                ""series"": { ""id"": 99, ""name"": ""Saga"" }
            }";

            var detail = JsonConvert.DeserializeObject<MetronIssueDetail>(json);

            detail.Number.Should().Be("3");
            detail.CollectionTitle.Should().BeNull();
            detail.StoryTitles.Should().ContainSingle().Which.Should().Be("Chapter Three");
            detail.PageCount.Should().Be(32);
            detail.Description.Should().NotBeNullOrEmpty();
            detail.Series.Id.Should().Be(99);
        }

        [Test]
        public void series_detail_should_bind_metron_read_serializer_fields()
        {
            const string json = @"{
                ""id"": 99,
                ""name"": ""Saga"",
                ""sort_name"": ""Saga"",
                ""volume"": 1,
                ""year_began"": 2012,
                ""year_end"": null,
                ""status"": ""Ongoing"",
                ""series_type"": { ""id"": 13, ""name"": ""Single Issue"" },
                ""publisher"": { ""id"": 6, ""name"": ""Image"" },
                ""desc"": ""Star-crossed lovers."",
                ""issue_count"": 66,
                ""genres"": [{ ""id"": 1, ""name"": ""Science Fiction"" }],
                ""image"": ""https://static.metron.cloud/media/series/saga.jpg""
            }";

            var detail = JsonConvert.DeserializeObject<MetronSeriesDetail>(json);

            detail.Name.Should().Be("Saga");
            detail.Volume.Should().Be(1);
            detail.Status.Should().Be("Ongoing");
            detail.SeriesType.Name.Should().Be("Single Issue");
            detail.Publisher.Name.Should().Be("Image");
            detail.Genres.Should().ContainSingle();
        }

        [Test]
        public void series_list_item_should_bind_metron_list_serializer_fields()
        {
            // List serializer fields: id, series, year_began, year_end, volume,
            // issue_count, modified — note: no publisher.
            const string json = @"{
                ""id"": 99,
                ""series"": ""Saga (2012)"",
                ""year_began"": 2012,
                ""year_end"": null,
                ""volume"": 1,
                ""issue_count"": 66,
                ""modified"": ""2023-01-01T00:00:00Z""
            }";

            var item = JsonConvert.DeserializeObject<MetronSeriesListItem>(json);

            item.Id.Should().Be(99);
            item.Name.Should().Be("Saga (2012)");
            item.Volume.Should().Be(1);
            item.IssueCount.Should().Be(66);
        }
    }
}
