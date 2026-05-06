using CgmLink.Api.Models;
using System;

namespace CgmLink.Api.Endpoints.Pens.NewPen
{
    public record NewPenResponse
    {
        public Guid Id { get; set; }
        public Guid InsulinId { get; set; }
        public PenModel Model { get; set; }
        public PenColour Colour { get; set; }
        public string Serial { get; set; } = string.Empty;
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset StartTime { get; set; }
    }
}