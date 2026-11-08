using FluentAssertions;
using HelpBridge.Domain.Accounting;
using HelpBridge.Domain.Assistance;
using HelpBridge.Domain.Enums;
using HelpBridge.Domain.Payouts;
using Xunit;

namespace HelpBridge.UnitTests;

public class DomainBusinessRulesTests
{
    [Fact]
    public void Request_CannotBeSubmitted_WithoutDocuments()
    {
        // Arrange
        var request = new AssistanceRequest(
            recipientId: "user-123",
            categoryId: 1,
            title: "Semester 5 Tuition Support",
            story: "Need assistance paying for my final year engineering semester.",
            targetAmount: 50000m,
            dueDate: DateTime.UtcNow.AddDays(30),
            billerName: "National Engineering Institute",
            billerRef: "REG-2026-9921",
            billerAccount: "00998811223344",
            billerIfsc: "HDFC0001234"
        );

        // Act
        var act = () => request.Submit();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*At least one supporting institutional document must be uploaded*");
    }

    [Fact]
    public void Request_WhenDocumentUploaded_CanBeSubmittedSuccessfully()
    {
        // Arrange
        var request = new AssistanceRequest(
            recipientId: "user-123",
            categoryId: 1,
            title: "Semester 5 Tuition Support",
            story: "Need assistance paying for my final year engineering semester.",
            targetAmount: 50000m,
            dueDate: DateTime.UtcNow.AddDays(30),
            billerName: "National Engineering Institute",
            billerRef: "REG-2026-9921",
            billerAccount: "00998811223344",
            billerIfsc: "HDFC0001234"
        );

        request.Documents.Add(new RequestDocument
        {
            RequestId = request.Id,
            DocumentType = DocumentType.FeeReceipt,
            BlobPath = "documents/fee-receipt-123.pdf",
            OriginalFileName = "fee_statement.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1048576,
            Sha256Checksum = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
        });

        // Act
        request.Submit();

        // Assert
        request.Status.Should().Be(RequestStatus.Submitted);
    }

    [Fact]
    public void Request_AddContribution_AdvancesToPartiallyFundedAndFullyFunded()
    {
        // Arrange
        var request = new AssistanceRequest("user-1", 1, "Tuition", "Story", 10000m, DateTime.UtcNow.AddDays(10), "College", "REF-1", "ACC-1", "IFSC-1");
        request.Documents.Add(new RequestDocument { BlobPath = "p.pdf", Sha256Checksum = "hash" });
        request.Submit();
        request.MarkUnderReview();
        request.Approve();
        request.PublishToMarketplace();

        // Act 1: ₹4,000 contribution
        request.AddContribution(4000m);
        request.FundedAmount.Should().Be(4000m);
        request.Status.Should().Be(RequestStatus.PartiallyFunded);
        request.ProgressPercentage.Should().Be(40m);

        // Act 2: ₹6,000 contribution (reaching 100%)
        request.AddContribution(6000m);
        request.FundedAmount.Should().Be(10000m);
        request.Status.Should().Be(RequestStatus.FullyFunded);
        request.ProgressPercentage.Should().Be(100m);
    }

    [Fact]
    public void Request_AddContribution_ExceedingTarget_ThrowsException()
    {
        // Arrange
        var request = new AssistanceRequest("user-1", 1, "Tuition", "Story", 10000m, DateTime.UtcNow.AddDays(10), "College", "REF-1", "ACC-1", "IFSC-1");
        request.Documents.Add(new RequestDocument { BlobPath = "p.pdf", Sha256Checksum = "hash" });
        request.Submit();
        request.MarkUnderReview();
        request.Approve();
        request.PublishToMarketplace();

        // Act: Try contributing ₹12,000 on a ₹10,000 goal
        var act = () => request.AddContribution(12000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exceeds remaining target*");
    }

    [Fact]
    public void DoubleEntryLedger_ImbalancedEntry_ThrowsException()
    {
        // Arrange
        var journalEntry = new JournalEntry("Donation", Guid.NewGuid(), "Donor contribution");
        journalEntry.AddDebit(ledgerAccountId: 1, amount: 1000m); // ₹1000 debit
        journalEntry.AddCredit(ledgerAccountId: 2, amount: 950m);  // Only ₹950 credit

        // Act & Assert
        var act = () => journalEntry.ValidateBalance();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Double-entry invariant violation*");
    }

    [Fact]
    public void DoubleEntryLedger_BalancedEntry_ValidatesSuccessfully()
    {
        // Arrange
        var journalEntry = new JournalEntry("Donation", Guid.NewGuid(), "Donor contribution with tip");
        journalEntry.AddDebit(ledgerAccountId: 1, amount: 1050m); // ₹1050 Gateway Clearing
        journalEntry.AddCredit(ledgerAccountId: 2, amount: 1000m); // ₹1000 Escrow Liability
        journalEntry.AddCredit(ledgerAccountId: 3, amount: 50m);   // ₹50 Platform Tip

        // Act & Assert
        var act = () => journalEntry.ValidateBalance();
        act.Should().NotThrow();
        journalEntry.IsBalanced.Should().BeTrue();
    }

    [Fact]
    public void MakerChecker_PayoutApprover_CannotBePayoutInitiator()
    {
        // Arrange
        var payout = new PayoutRecord(Guid.NewGuid(), 50000m, initiatedByUserId: "finance-officer-vikram");

        // Act: Vikram attempts to self-approve his own payout
        var act = () => payout.ProcessPayout(approvedByUserId: "finance-officer-vikram");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Maker-Checker violation*");
    }
}

