using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues
{
    public class Credit : MemberwiseEquatable<Credit>, IEmbeddedDocument
    {
        public string PersonName { get; set; }
        public string Role { get; set; }
    }
}
