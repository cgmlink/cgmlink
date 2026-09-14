using System;

namespace CgmLink.Identity.Models;

public sealed record MeResponse(
    Guid Id,
    string Email,
    bool IsVerified,
    UserType UserType,
    GlucoseProvider? GlucoseProvider
);