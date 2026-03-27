using System.Collections.Generic;
using System.Collections.ObjectModel;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class BookInfoRefreshedEvent : IEvent
    {
        public Series Series { get; set; }
        public ReadOnlyCollection<Issue> Added { get; private set; }

        public ReadOnlyCollection<Issue> Updated { get; private set; }
        public ReadOnlyCollection<Issue> Removed { get; private set; }

        public BookInfoRefreshedEvent(Series author, IList<Issue> added, IList<Issue> updated, IList<Issue> removed)
        {
            Series = author;
            Added = new ReadOnlyCollection<Issue>(added);
            Updated = new ReadOnlyCollection<Issue>(updated);
            Removed = new ReadOnlyCollection<Issue>(removed);
        }
    }
}
