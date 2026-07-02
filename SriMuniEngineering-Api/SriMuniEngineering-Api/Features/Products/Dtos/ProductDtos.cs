using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Products.Dtos;

public class CreateProductRequest
{
    [Required]
    [MaxLength(100)]
    public string PartNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string PartName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PartDescription { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal BasePricePerUnit { get; set; }

    [Required]
    [MaxLength(20)]
    public string HsnSac { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = "Nos";
}

public class UpdateProductRequest : CreateProductRequest { }

public class ProductResponse
{
    public Guid Id { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? PartDescription { get; set; }
    public decimal BasePricePerUnit { get; set; }
    public string HsnSac { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
