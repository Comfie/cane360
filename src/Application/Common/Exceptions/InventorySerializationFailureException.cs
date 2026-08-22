namespace Cane360.Application.Common.Exceptions;

public sealed class InventorySerializationFailureException(string message, Exception innerException)
    : Exception(message, innerException);
