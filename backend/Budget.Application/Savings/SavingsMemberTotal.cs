namespace Budget.Application.Savings;

/// <summary>Суммарные накопления участника за период сводки.</summary>
/// <param name="UserId">Идентификатор участника.</param>
/// <param name="UserName">Отображаемое имя участника.</param>
/// <param name="Amount">Сумма накоплений участника.</param>
public sealed record SavingsMemberTotal(int UserId, string UserName, decimal Amount);
