using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class NotFoundException(string message) : ApiException(message, null, HttpStatusCode.NotFound);