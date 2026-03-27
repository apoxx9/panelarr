using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Books
{
    public class AddIssueOptions : IEmbeddedDocument
    {
        public AddIssueOptions()
        {
            // default in case not set in db
            AddType = IssueAddType.Automatic;
        }

        public IssueAddType AddType { get; set; }
        public bool SearchForNewBook { get; set; }
    }

    public enum IssueAddType
    {
        Automatic,
        Manual
    }
}
