using System;

namespace NzbDrone.Core.Notifications.Komga
{
    public class KomgaException : ApplicationException
    {
        public KomgaException(string message)
            : base(message)
        {
        }

        public KomgaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
