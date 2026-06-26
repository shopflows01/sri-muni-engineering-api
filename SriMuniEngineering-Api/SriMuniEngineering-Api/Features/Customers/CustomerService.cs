using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Customers.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Customers;

public class CustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.GSTIN == request.GSTIN);
        if (existing is not null)
            throw new InvalidOperationException($"Customer with GSTIN '{request.GSTIN}' already exists.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BillingAddress = request.BillingAddress,
            ShippingAddress = request.ShippingAddress,
            Pincode = request.Pincode,
            GSTIN = request.GSTIN,
            StateCode = request.StateCode,
            StateName = request.StateName,
            Phone = request.Phone,
            Email = request.Email,
            VendorCode = request.VendorCode
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToResponse(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        customer.Name = request.Name;
        customer.BillingAddress = request.BillingAddress;
        customer.ShippingAddress = request.ShippingAddress;
        customer.Pincode = request.Pincode;
        customer.GSTIN = request.GSTIN;
        customer.StateCode = request.StateCode;
        customer.StateName = request.StateName;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.VendorCode = request.VendorCode;

        await _context.SaveChangesAsync();
        return MapToResponse(customer);
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        return MapToResponse(customer);
    }

    public async Task<PaginatedResponse<CustomerResponse>> GetAllAsync(PaginatedRequest filter)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(c =>
                c.Name.Contains(filter.Search) ||
                c.GSTIN.Contains(filter.Search) ||
                c.VendorCode!.Contains(filter.Search) ||
                c.Phone.Contains(filter.Search));

        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "name" => isAsc ? query.OrderBy(c => c.Name) : query.OrderByDescending(c => c.Name),
            "gstin" => isAsc ? query.OrderBy(c => c.GSTIN) : query.OrderByDescending(c => c.GSTIN),
            "statecode" => isAsc ? query.OrderBy(c => c.StateCode) : query.OrderByDescending(c => c.StateCode),
            _ => query.OrderBy(c => c.Name)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<CustomerResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        BillingAddress = c.BillingAddress,
        ShippingAddress = c.ShippingAddress,
        Pincode = c.Pincode,
        GSTIN = c.GSTIN,
        StateCode = c.StateCode,
        StateName = c.StateName,
        Phone = c.Phone,
        Email = c.Email,
        VendorCode = c.VendorCode
    };
}
