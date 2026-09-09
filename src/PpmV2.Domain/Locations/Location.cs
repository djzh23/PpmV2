namespace PpmV2.Domain.Locations;

public class Location
{
    public Guid Id { get; set; }

    // Display in the drop-down
    public string Name { get; set; } = default!;
    public string District { get; set; } = default!;

    // Optional, helpful later (Route/Google Maps)
    public string? Address { get; set; }

    // Soft-delete / deactivate without losing history
    public bool IsActive { get; set; } = true;

    // Optional: Text for special features (parking, access, contact persons)
    public string? Notes { get; set; }

    // Location detail page fields
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ContactPerson { get; set; }
    public int? Capacity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
