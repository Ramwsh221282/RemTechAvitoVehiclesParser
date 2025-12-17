namespace RemTechAvitoVehiclesParser.ParserWorkStages.Common;

public sealed class AvitoVehicle
{
    public bool Processed { get; init; }
    public int RetryCount { get; init; }
    public AvitoVehicleCatalogueRepresentation CatalogueRepresentation { get; init; } = AvitoVehicleCatalogueRepresentation.Empty();
    public AvitoVehicleConcretePageRepresentation ConcretePageRepresentation { get; init; } = AvitoVehicleConcretePageRepresentation.Empty();
    
    public AvitoVehicle Transform<T>(T source, Func<T,AvitoVehicle> factory)
    {
        AvitoVehicle vehicle = factory(source);
        return new()
        {
            Processed = Processed,
            RetryCount = RetryCount,
            CatalogueRepresentation = vehicle.CatalogueRepresentation,
            ConcretePageRepresentation = vehicle.ConcretePageRepresentation,
        }; 
    }

    public AvitoVehicle CopyWith(Func<AvitoVehicle, AvitoVehicle> factory) => factory(this);
    
    public AvitoVehicle IncreaseRetryCount() =>
        CopyWith(veh => new()
        {
            Processed = veh.Processed,
            RetryCount = veh.RetryCount + 1,
            CatalogueRepresentation = veh.CatalogueRepresentation,
            ConcretePageRepresentation = veh.ConcretePageRepresentation,
        });
    
    public AvitoVehicle MarkProcessed() =>
        CopyWith(veh => new()
        {
            Processed = true,
            RetryCount = veh.RetryCount,
            CatalogueRepresentation = veh.CatalogueRepresentation,
            ConcretePageRepresentation = veh.ConcretePageRepresentation,
        });
    
    public static AvitoVehicle New() =>
        new()
        {
            RetryCount = 0,
            Processed = false,
        };
    
    public static AvitoVehicle Create(bool processed, int retryCount, AvitoVehicleCatalogueRepresentation catalogue, AvitoVehicleConcretePageRepresentation concrete) =>
        new()
        {
            Processed = processed,
            RetryCount = retryCount,
            CatalogueRepresentation = catalogue,
            ConcretePageRepresentation = concrete
        };
    
    public static AvitoVehicle Create(AvitoVehicle vehicle) =>
        new()
        {
            Processed = vehicle.Processed,
            RetryCount = vehicle.RetryCount,
            CatalogueRepresentation = vehicle.CatalogueRepresentation,
            ConcretePageRepresentation = vehicle.ConcretePageRepresentation
        };
    
    public static AvitoVehicle RepresentedByCatalogueItem<T>(
        T source,
        Func<T, string> idMap,
        Func<T, string> urlMap,
        Func<T, long> priceMap,
        Func<T, bool> isNdsMap,
        Func<T, string> addressMap,
        Func<T, string[]> photosMap
        ) where T : class =>
        new()
        {
            CatalogueRepresentation = new AvitoVehicleCatalogueRepresentation(
                idMap(source),
                urlMap(source),
                priceMap(source),
                isNdsMap(source),
                addressMap(source),
                photosMap(source)
            ),
        };

    public static AvitoVehicle RepresentedByConcretePageItem<T>(
        T source,
        Func<T, string> titleMap,
        Func<T, Dictionary<string, string>> characteristicsMap
        ) where T : class =>
        new()
        {
            Processed = false,
            RetryCount = 0,
            ConcretePageRepresentation = new AvitoVehicleConcretePageRepresentation(
                titleMap(source),
                characteristicsMap(source)
            )
        };
}