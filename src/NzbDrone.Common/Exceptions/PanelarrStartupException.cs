using System;

namespace NzbDrone.Common.Exceptions
{
    public class PanelarrStartupException : NzbDroneException
    {
        public PanelarrStartupException(string message, params object[] args)
            : base("Panelarr failed to start: " + string.Format(message, args))
        {
        }

        public PanelarrStartupException(string message)
            : base("Panelarr failed to start: " + message)
        {
        }

        public PanelarrStartupException()
            : base("Panelarr failed to start")
        {
        }

        public PanelarrStartupException(Exception innerException, string message, params object[] args)
            : base("Panelarr failed to start: " + string.Format(message, args), innerException)
        {
        }

        public PanelarrStartupException(Exception innerException, string message)
            : base("Panelarr failed to start: " + message, innerException)
        {
        }

        public PanelarrStartupException(Exception innerException)
            : base("Panelarr failed to start: " + innerException.Message)
        {
        }
    }
}
