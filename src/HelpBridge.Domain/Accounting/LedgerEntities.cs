using HelpBridge.Domain.Common;
using HelpBridge.Domain.Enums;

namespace HelpBridge.Domain.Accounting;

public class LedgerAccount : BaseEntity<int>
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public LedgerAccountType AccountType { get; set; }
    public decimal Balance { get; set; } = 0.0000m;

    public ICollection<JournalLineItem> LineItems { get; set; } = [];

    public LedgerAccount() { }

    public LedgerAccount(int id, string code, string name, LedgerAccountType type)
    {
        Id = id;
        AccountCode = code;
        AccountName = name;
        AccountType = type;
        CreatedUtc = DateTime.UtcNow;
    }
}

public class JournalEntry : BaseEntity
{
    public string ReferenceType { get; set; } = string.Empty; // Donation, Payout, Refund, GatewayFee
    public Guid ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;

    public ICollection<JournalLineItem> LineItems { get; set; } = [];

    public decimal TotalDebits => LineItems.Sum(li => li.DebitAmount);
    public decimal TotalCredits => LineItems.Sum(li => li.CreditAmount);
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.0001m;

    public JournalEntry() { }

    public JournalEntry(string referenceType, Guid referenceId, string description)
    {
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Description = description;
        CreatedUtc = DateTime.UtcNow;
    }

    public void AddDebit(int ledgerAccountId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Debit amount must be greater than zero.");
        LineItems.Add(new JournalLineItem(Id, ledgerAccountId, debitAmount: amount, creditAmount: 0m));
    }

    public void AddCredit(int ledgerAccountId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount must be greater than zero.");
        LineItems.Add(new JournalLineItem(Id, ledgerAccountId, debitAmount: 0m, creditAmount: amount));
    }

    public void ValidateBalance()
    {
        if (!IsBalanced)
        {
            throw new InvalidOperationException($"Double-entry invariant violation: Debits (₹{TotalDebits:N2}) != Credits (₹{TotalCredits:N2}) for journal entry {Id}.");
        }
    }
}

public class JournalLineItem : BaseEntity<long>
{
    public Guid JournalEntryId { get; set; }
    public int LedgerAccountId { get; set; }
    public decimal DebitAmount { get; set; } = 0.0000m;
    public decimal CreditAmount { get; set; } = 0.0000m;

    public JournalEntry JournalEntry { get; set; } = null!;
    public LedgerAccount LedgerAccount { get; set; } = null!;

    public JournalLineItem() { }

    public JournalLineItem(Guid entryId, int accountId, decimal debitAmount, decimal creditAmount)
    {
        JournalEntryId = entryId;
        LedgerAccountId = accountId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        CreatedUtc = DateTime.UtcNow;
    }
}

