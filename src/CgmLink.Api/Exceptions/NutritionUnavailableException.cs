using CgmLink.AspNetCore.Exceptions;
using System;
using System.Net;

namespace CgmLink.Api.Exceptions;

public sealed class NutritionUnavailableException(Exception innerException)
    : ApiException("NUTRITION_UNAVAILABLE", null, HttpStatusCode.ServiceUnavailable, innerException);
