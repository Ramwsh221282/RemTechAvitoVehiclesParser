namespace RemTechAvitoVehiclesParser.ParserWorkStages.PendingItemPublishing.Models;

public static class PendingToPublishItemImplementation
{
    extension(PendingToPublishItem item)
    {
        public PendingToPublishItem MarkProcessed()
        {
            if (item.WasProcessed)
                throw new InvalidOperationException(
                    """
                    Cannot mark as processed.
                    Item is already processed.
                    """);
            return item with { WasProcessed = true };
        }
    }
}