using System.Net;
using Panelarr.Http.Exceptions;

namespace Panelarr.Http.REST
{
    public class BadRequestException : ApiException
    {
        public BadRequestException(object content = null)
            : base(HttpStatusCode.BadRequest, content)
        {
        }
    }
}
