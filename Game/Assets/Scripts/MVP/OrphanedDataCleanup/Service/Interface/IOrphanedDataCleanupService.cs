/// <summary>
/// Interface for the orphaned data cleanup service.
/// Validates all world data references against loaded catalogs
/// and removes entries whose catalog data no longer exists.
/// </summary>
public interface IOrphanedDataCleanupService
{
    CleanupReport RunCleanup();
}
