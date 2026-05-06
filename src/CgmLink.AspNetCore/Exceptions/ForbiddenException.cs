using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(message, null, HttpStatusCode.Forbidden)
    {
    }
}