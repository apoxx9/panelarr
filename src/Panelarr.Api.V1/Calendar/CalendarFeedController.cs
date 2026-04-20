using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Tags;
using Panelarr.Http;

namespace Panelarr.Api.V1.Calendar
{
    [V1FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IIssueService _issueService;
        private readonly ISeriesService _seriesService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IIssueService issueService, ISeriesService seriesService, ITagService tagService)
        {
            _issueService = issueService;
            _seriesService = seriesService;
            _tagService = tagService;
        }

        [HttpGet("Panelarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tagList = "", bool unmonitored = false)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var tags = new List<int>();

            if (tagList.IsNotNullOrWhiteSpace())
            {
                tags.AddRange(tagList.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var issues = _issueService.IssuesBetweenDates(start, end, unmonitored);
            var allSeries = _seriesService.GetAllSeries().ToDictionary(s => s.Id);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//panelarr.com//Panelarr//EN"
            };

            var calendarName = "Panelarr Issue Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var issue in issues.OrderBy(v => v.ReleaseDate.Value))
            {
                if (!allSeries.TryGetValue(issue.SeriesId, out var series))
                {
                    continue;
                }

                if (tags.Any() && tags.None(series.Tags.Contains))
                {
                    continue;
                }

                var occurrence = calendar.Create<CalendarEvent>();
                occurrence.Uid = "Panelarr_issue_" + issue.Id;

                //occurrence.Status = issue.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = string.Empty;
                occurrence.Categories = issue.Genres;

                occurrence.Start = new CalDateTime(issue.ReleaseDate.Value.ToLocalTime()) { HasTime = false };
                occurrence.End = occurrence.Start;
                occurrence.IsAllDay = true;

                occurrence.Summary = $"{series.Name} - {issue.Title}";
            }

            var serializer = (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }
    }
}
