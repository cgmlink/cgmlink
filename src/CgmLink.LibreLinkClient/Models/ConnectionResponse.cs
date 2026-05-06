using System.Collections.Generic;

namespace CgmLink.LibreLinkClient.Models;

public sealed record ConnectionResponse : LibreLinkResponse<IReadOnlyCollection<ConnectionData>>
{
}