using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.Accounts.Dtos;

public class CreateCustomerLedgerRequest
{
    public Guid CustomerId { get; set; }
    public decimal OpeningBalance { get; set; }
    public BalanceType OpeningBalanceType { get; set; }
}
