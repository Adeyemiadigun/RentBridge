using RentBridge.Domain.Enums;

namespace RentBridge.Application.Dtos.Properties;

public sealed record OwnershipDocumentItem(
    Guid Id,
    OwnershipDocStatus Status,
    string FileKey);

public sealed record PropertyItem(
    Guid Id,
    string Street,
    string City,
    string Area,
    string State,
    bool IsVerified,
    IReadOnlyList<OwnershipDocumentItem> Documents);