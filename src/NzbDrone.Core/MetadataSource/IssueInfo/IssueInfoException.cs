using System;
using System.Net;
using NzbDrone.Core.Exceptions;

namespace NzbDrone.Core.MetadataSource.IssueInfo
{
    public class IssueInfoException : NzbDroneClientException
    {
        public IssueInfoException(string message)
            : base(HttpStatusCode.ServiceUnavailable, message)
        {
        }

        public IssueInfoException(string message, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, args)
        {
        }

        public IssueInfoException(string message, Exception innerException, params object[] args)
            : base(HttpStatusCode.ServiceUnavailable, message, innerException, args)
        {
        }
    }
}
