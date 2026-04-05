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
                        new object[] { 3, Quality.CBR },
                        new object[] { 4, Quality.CBZ },
                        new object[] { 5, Quality.CB7 },
                        new object[] { 6, Quality.CBZ_Web },
                        new object[] { 7, Quality.CBZ_HD },
                };

        public static object[] ToIntCases =
                {
                        new object[] { Quality.Unknown, 0 },
                        new object[] { Quality.PDF, 1 },
                        new object[] { Quality.EPUB, 2 },
                        new object[] { Quality.CBR, 3 },
                        new object[] { Quality.CBZ, 4 },
                        new object[] { Quality.CB7, 5 },
                        new object[] { Quality.CBZ_Web, 6 },
                        new object[] { Quality.CBZ_HD, 7 },
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
                Quality.CBR,
                Quality.CBZ_Web,
                Quality.CBZ,
                Quality.CB7,
                Quality.CBZ_HD
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
