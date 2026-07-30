/// <summary>
/// Implement this interface on a MonoBehaviour when a future networking
/// package is added. The host/server implementation should return true while
/// clients return false, preventing every client from spawning duplicates.
/// </summary>
public interface IRegionalSpawnAuthority
{
    bool HasRegionalSpawnAuthority { get; }
}
