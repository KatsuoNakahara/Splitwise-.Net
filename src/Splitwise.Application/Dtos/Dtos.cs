namespace Splitwise.Application.Dtos;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string Token, string UserId, string Email, string DisplayName);

public sealed record CreateGroupRequest(string Name, IReadOnlyList<string>? MemberEmails);

public sealed record AddMemberRequest(string Email);

public sealed record MemberDto(string UserId, string DisplayName);

public sealed record GroupSummaryDto(int Id, string Name, DateTime CreatedAt, int MemberCount);

public sealed record GroupDetailsDto(int Id, string Name, string CreatedByUserId, DateTime CreatedAt, IReadOnlyList<MemberDto> Members);

public enum SplitType
{
    Equal = 0,
    Exact = 1,
}

public sealed record ExactSplitInput(string UserId, decimal Amount);

public sealed record CreateExpenseRequest(
    string Description,
    decimal Amount,
    string PaidByUserId,
    SplitType SplitType,
    IReadOnlyList<string>? ParticipantUserIds,
    IReadOnlyList<ExactSplitInput>? ExactSplits);

public sealed record ExpenseSplitDto(string UserId, string DisplayName, decimal Amount);

public sealed record ExpenseDto(
    int Id,
    int GroupId,
    string Description,
    decimal Amount,
    string PaidByUserId,
    string PaidByName,
    DateTime CreatedAt,
    IReadOnlyList<ExpenseSplitDto> Splits);

public sealed record CreateSettlementRequest(string ToUserId, decimal Amount);

public sealed record SettlementDto(int Id, int GroupId, string FromUserId, string ToUserId, decimal Amount, DateTime CreatedAt);

public sealed record UserBalanceDto(string UserId, string DisplayName, decimal Net);

public sealed record DebtDto(string FromUserId, string FromName, string ToUserId, string ToName, decimal Amount);

public sealed record BalanceReportDto(IReadOnlyList<UserBalanceDto> Balances, IReadOnlyList<DebtDto> Debts);
