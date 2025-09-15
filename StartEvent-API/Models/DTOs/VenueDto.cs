using System.ComponentModel.DataAnnotations;

namespace StartEvent_API.Models.DTOs;

public class VenueDto
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // For response purposes - count of events at this venue
    public int EventCount { get; set; }
}

public class CreateVenueDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}

public class UpdateVenueDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}