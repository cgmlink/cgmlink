using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class UnauthorizedException(string message, UnauthorizedSource unauthorizedSource)
    : ApiException(message, null, HttpStatusCode.Unauthorized)
{
    public UnauthorizedSource UnauthorizedSource { get; set; } = unauthorizedSource;
}