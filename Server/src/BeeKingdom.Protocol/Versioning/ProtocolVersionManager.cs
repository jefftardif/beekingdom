namespace BeeKingdom.Protocol.Versioning;

public sealed class ProtocolVersionManager
{
    public ProtocolVersion Current => ProtocolVersion.Current;

    public bool IsSupported(ProtocolVersion version)
    {
        return version.Major == Current.Major && version.Minor <= Current.Minor;
    }

    public ProtocolVersion NegotiateVersion(IEnumerable<ProtocolVersion> clientSupportedVersions)
    {
        return clientSupportedVersions
            .Where(IsSupported)
            .OrderByDescending(version => version.Major)
            .ThenByDescending(version => version.Minor)
            .FirstOrDefault();
    }
}
