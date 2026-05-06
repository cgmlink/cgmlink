using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class UnauthorizedException(string message) : ApiException(message, null, HttpStatusCode.Unauthorized);