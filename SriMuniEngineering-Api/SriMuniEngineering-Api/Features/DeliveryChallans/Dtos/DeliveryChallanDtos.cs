namespace SriMuniEngineering_Api.Features.DeliveryChallans.Dtos;

public record DeliveryChallanResponse(
    Guid Id,
    string DcNo,
    Guid CustomerId,
    string CustomerName,
    DateTime DcDate,
    string? YourDcNo,
    DateTime? YourDcDate,
    string? PoNo,
    string? Remarks,
    List<DeliveryChallanItemResponse> Items
);

public record DeliveryChallanItemResponse(
    Guid Id,
    Guid ProductId,
    string PartNo,
    string PartName,
    int Quantity,
    string? Unit,
    string? Remarks
);

public record CreateDeliveryChallanRequest(
    Guid CustomerId,
    DateTime DcDate,
    string? YourDcNo,
    DateTime? YourDcDate,
    string? PoNo,
    string? Remarks,
    List<CreateDeliveryChallanItemRequest> Items
);

public record CreateDeliveryChallanItemRequest(
    Guid ProductId,
    int Quantity,
    string? Unit,
    string? Remarks
);
