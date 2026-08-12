using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Contracts;

public interface IContract
{
    ContractVersion ContractVersion { get; }
}
