namespace Budget.Domain.Entities;

/// <summary>Направление операции, сохраняемое в истории независимо от изменения категорий.</summary>
public enum TransactionKind
{
    /// <summary>Поступление денежных средств.</summary>
    Income,

    /// <summary>Расход денежных средств.</summary>
    Expense
}
