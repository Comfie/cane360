using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record CreateStockReceiptCommand(
    string ReceiptType,
    Guid? SupplierId,
    DateOnly ReceiptDate,
    Guid? ReceivedByPersonId,
    string SourceReference,
    string? Reason,
    string? LateEntryReason,
    IReadOnlyList<CreateStockReceiptLineCommand> Lines) : IRequest<StockReceiptDto>;
