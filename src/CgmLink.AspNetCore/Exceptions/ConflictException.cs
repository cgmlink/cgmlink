using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(message, null, HttpStatusCode.Conflict)
    {
    }
}