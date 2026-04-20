using System.Net;
using Panelarr.Http.Exceptions;

namespace Panelarr.Http.REST
{
    public class NotFoundException : ApiException
    {
        public NotFoundException(object content = null)
            : base(HttpStatusCode.NotFound, content)
        {
        }
    }
}
