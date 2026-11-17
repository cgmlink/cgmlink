using System;
using System.Collections.Generic;
using System.Net;

namespace CgmLink.AspNetCore.Exceptions;

public class ApiException : Exception
{
    public List<string>? ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, List<string>? errors = null, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : this(message, errors, statusCode, null)
    {
    }

    public ApiException(string message, List<string>? errors, HttpStatusCode statusCode, Exception? innerException)
        : base(message, innerException)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }
}
