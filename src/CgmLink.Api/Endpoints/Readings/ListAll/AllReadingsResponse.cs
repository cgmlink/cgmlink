using System;
using CgmLink.Api.Models;

namespace CgmLink.Api.Endpoints.Readings.ListAll;

public record AllReadingsResponse
{
    public Guid UserId { get; init; }
    public Guid Id { get; init; }
    public DateTimeOffset Created { get; init; }
    public double GlucoseLevel { get; init; }
    public ReadingDirection Direction { get; init; }
}