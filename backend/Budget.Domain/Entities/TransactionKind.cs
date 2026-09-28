namespace Budget.Domain.Entities;

/// <summary>Direction of a transaction. Stored on the transaction itself so history
/// survives category reorganisation.</summary>
public enum TransactionKind
{
    Income,
    Expense
}
