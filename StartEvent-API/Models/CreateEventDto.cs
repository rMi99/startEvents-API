using System.ComponentModel.DataAnnotations;

namespace StartEvent_API.Models;

public class CreateEventDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public DateTime EventTime { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public IFormFile? ImageFile { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public Guid VenueId { get; set; }

    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// JSON string containing price information
    /// Format: [{"name": "VIP", "price": 100.00, "availableTickets": 50}]
    /// </summary>
    public string? PricesJson { get; set; }
}

public class CreateEventPriceDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int AvailableTickets { get; set; }
}