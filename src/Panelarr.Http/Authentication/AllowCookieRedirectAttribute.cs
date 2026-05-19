using System;
using Microsoft.AspNetCore.Http.Metadata;

namespace Panelarr.Http.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowCookieRedirectAttribute : Attribute, IAllowCookieRedirectMetadata
    {
    }
}
