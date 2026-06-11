using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFixture : CoreTest
    {
        public static object[] FromIntCases =
                {
                        new object[] { 0, Quality.Unknown },
                        new object[] { 1, Quality.PDF },
                        new object[] { 2, Quality.EPUB },
                        new object[] { 8, Quality.Scan },
                        new object[] { 9, Quality.C2C },
                        new object[] { 10, Quality.Archive },
                        new object[] { 11, Quality.WebRip },
                        new object[] { 12, Quality.Digital },
                };

        public static object[] ToIntCases =
                {
                        new object[] { Quality.Unknown, 0 },
                        new object[] { Quality.PDF, 1 },
                        new object[] { Quality.EPUB, 2 },
                        new object[] { Quality.Scan, 8 },
                        new object[] { Quality.C2C, 9 },
                        new object[] { Quality.Archive, 10 },
                        new object[] { Quality.WebRip, 11 },
                        new object[] { Quality.Digital, 12 },
                };

        [Test]
        [TestCaseSource(nameof(FromIntCases))]
        public void should_be_able_to_convert_int_to_qualityTypes(int source, Quality expected)
        {
            var quality = (Quality)source;
            quality.Should().Be(expected);
        }

        [Test]
        [TestCaseSource(nameof(ToIntCases))]
        public void should_be_able_to_convert_qualityTypes_to_int(Quality source, int expected)
        {
            var i = (int)source;
            i.Should().Be(expected);
        }

        public static List<QualityProfileQualityItem> GetDefaultQualities(params Quality[] allowed)
        {
            var qualities = new List<Quality>
            {
                Quality.Unknown,
                Quality.PDF,
                Quality.EPUB,
                Quality.Scan,
                Quality.C2C,
                Quality.Archive,
                Quality.WebRip,
                Quality.Digital
            };

            if (allowed.Length == 0)
            {
                allowed = qualities.ToArray();
            }

            var items = qualities
                .Except(allowed)
                .Concat(allowed)
                .Select(v => new QualityProfileQualityItem { Quality = v, Allowed = allowed.Contains(v) }).ToList();

            return items;
        }
    }
}
