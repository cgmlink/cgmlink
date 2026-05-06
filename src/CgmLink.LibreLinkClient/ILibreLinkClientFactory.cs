namespace CgmLink.LibreLinkClient;

public interface ILibreLinkClientFactory
{
    ILibreLinkClient CreateLibreLinkClient(LibreRegion region);
}