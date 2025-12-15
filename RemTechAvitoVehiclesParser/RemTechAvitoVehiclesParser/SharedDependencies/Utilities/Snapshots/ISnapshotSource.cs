namespace RemTechAvitoVehiclesParser.SharedDependencies.Utilities.Snapshots;

public interface ISnapshotSource<TSource, out TUSnapshot>
    where TSource : class
    where TUSnapshot : ISnapshot
{
    TUSnapshot GetSnapshot();
}