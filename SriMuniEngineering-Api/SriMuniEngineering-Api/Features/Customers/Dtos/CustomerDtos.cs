using System.ComponentModel.DataAnnotations;

namespace SriMuniEngineering_Api.Features.Customers.Dtos;

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string BillingAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string GSTIN { get; set; } = string.Empty;

    [Required]
    public int StateCode { get; set; }

    [MaxLength(100)]
    public string StateName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? VendorCode { get; set; }
}

public class UpdateCustomerRequest : CreateCustomerRequest { }

public class CustomerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public int StateCode { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? VendorCode { get; set; }
}
