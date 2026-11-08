namespace HelpBridge.Domain.Enums;

public enum RequestStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    InfoRequired = 3,
    Verified = 4,
    Rejected = 5,
    Active = 6,
    PartiallyFunded = 7,
    FullyFunded = 8,
    ReadyForPayout = 9,
    PayoutProcessing = 10,
    PayoutFailed = 11,
    Completed = 12,
    Suspended = 13,
    Cancelled = 14,
    Expired = 15
}

public enum AssistanceCategoryType
{
    Education = 1,
    Medical = 2,
    EssentialBills = 3,
    Housing = 4,
    Emergency = 5
}

public enum DocumentType
{
    FeeReceipt = 1,
    AdmissionLetter = 2,
    HospitalBill = 3,
    TreatmentEstimate = 4,
    IdentityProof = 5,
    SalarySlip = 6,
    RentAgreement = 7,
    Other = 8
}

public enum VerificationActionType
{
    Claimed = 1,
    InformationRequested = 2,
    Approved = 3,
    Rejected = 4,
    Escalated = 5,
    NotesAdded = 6
}

public enum DonationStatus
{
    Pending = 0,
    Confirmed = 1,
    Failed = 2,
    Refunded = 3
}

public enum PaymentTransactionStatus
{
    Created = 0,
    Pending = 1,
    Success = 2,
    Failed = 3,
    Reconciled = 4
}

public enum PayoutStatus
{
    ReadyForPayout = 1,
    PayoutProcessing = 2,
    PayoutSuccess = 3,
    PayoutFailed = 4,
    Completed = 5
}

public enum LedgerAccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum UserRoleType
{
    Recipient = 1,
    Donor = 2,
    VerificationOfficer = 3,
    FinanceManager = 4,
    SupportAgent = 5,
    SuperAdmin = 6
}

