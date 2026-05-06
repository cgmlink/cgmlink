using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class BadRequestException(string message) : ApiException(message, null, HttpStatusCode.BadRequest)
{
}
