using RentBridge.Domain.Common;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Owned child entity of the Listing aggregate. Holds a hosted URL for a
/// listing photograph. The first image in position order becomes the cover.
/// </summary>
public class ListingImage
{
    public Guid Id { get; private set; }
    public Guid ListingId { get; private set; }
    public string Url { get; private set; }
    public int Position { get; private set; }

    public ListingImage(Guid listingId, string url, int position)
    {
        Id = Guid.NewGuid();
        ListingId = listingId;
        Url = url;
        Position = position;
    }

    private ListingImage() { }
}