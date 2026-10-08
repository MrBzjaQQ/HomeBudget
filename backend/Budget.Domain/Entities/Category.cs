namespace Budget.Domain.Entities;

/// <summary>Категория доходов или расходов, являющаяся группой либо подкатегорией.</summary>
public class Category
{
    /// <summary>Уникальный идентификатор категории.</summary>
    public int Id { get; set; }

    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Направление операций; подкатегория наследует его от группы.</summary>
    public TransactionKind Kind { get; set; }

    /// <summary>Идентификатор родительской группы; null для корневой категории.</summary>
    public int? ParentId { get; set; }

    /// <summary>Родительская группа, если категория является подкатегорией.</summary>
    public Category? Parent { get; set; }

    /// <summary>Необязательный цвет категории в шестнадцатеричном формате RGB.</summary>
    public string? Color { get; set; }

    /// <summary>Порядок отображения категории среди соседних категорий.</summary>
    public int SortOrder { get; set; }

    /// <summary>Признак архивации категории без удаления истории операций.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Дата и время создания категории.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Подкатегории, принадлежащие этой группе.</summary>
    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>Операции, отнесённые к этой категории.</summary>
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>Плановые позиции бюджета, связанные с категорией.</summary>
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
