# HelpBridge — Software Requirements Specification (SRS)
**Document Version:** 1.0.0  
**Standard:** Compliant with IEEE 830 / ISO/IEC/IEEE 29148  
**System:** HelpBridge Financial Assistance Platform  

---

## 1. Introduction

### 1.1 Purpose
This Software Requirements Specification (SRS) establishes the definitive technical and functional requirements for the development of HelpBridge Version 1.0. It provides an unambiguous baseline for developers, architects, QA engineers, and system administrators.

### 1.2 System Scope
HelpBridge is a high-assurance web platform built as a Modular Monolith (.NET 8/9 Web API backend + Angular frontend) enabling verified individuals to request financial assistance for legitimate medical and educational obligations, connecting them with donors whose contributions are directly remitted to verified service providers and billers.

### 1.3 Definitions, Acronyms, and Abbreviations
- **DoD:** Definition of Done.
- **DPDP Act:** Digital Personal Data Protection Act (India, 2023).
- **FIFO:** First In, First Out (Queue processing).
- **HMAC:** Hash-Based Message Authentication Code.
- **Idempotency Key:** A unique UUID transmitted in HTTP requests to guarantee that duplicate requests do not create duplicate effects.
- **JWT:** JSON Web Token.
- **OCR:** Optical Character Recognition.
- **PBAC:** Policy-Based Access Control.
- **RBAC:** Role-Based Access Control.
- **SAS:** Shared Access Signature (Azure Blob Storage).
- **TOTP:** Time-Based One-Time Password.
- **UTR:** Unique Transaction Reference (Indian banking NEFT/RTGS/IMPS settlement identifier).

---

## 2. Overall Description

### 2.1 Product Perspective
HelpBridge operates as an independent, cloud-hosted platform running on Microsoft Azure. It interfaces with external banking/payment aggregators, Azure AI Document Intelligence for OCR parsing, Azure Blob Storage for encrypted documents, and Firebase/SendGrid for multi-channel messaging.

```
       +--------------------------------------------------------------+
       |                  HELPBRIDGE ANGULAR SPA                      |
       +--------------------------------------------------------------+
                                      |  HTTPS / WSS (SignalR)
                                      v
       +--------------------------------------------------------------+
       |             ASP.NET CORE MODULAR MONOLITH API                |
       |  +--------------------------------------------------------+  |
       |  |  Modules: Auth | Recipient | Request | Verification    |  |
       |  |           Discovery | Donation | Payment | Ledger | Admin  |  |
       |  +--------------------------------------------------------+  |
       +--------------------------------------------------------------+
             |              |               |               |
             v              v               v               v
      +------------+  +-----------+  +-------------+  +---------------+
      | SQL Server |  | Redis     |  | Azure Blob  |  | External APIs |
      | (OLTP &    |  | (Cache &  |  | (Encrypted  |  | (Razorpay,    |
      | Ledger)    |  | Sessions) |  | Documents)  |  | Azure AI, Fcm)|
      +------------+  +-----------+  +-------------+  +---------------+
```

### 2.2 User Classes & Roles
1. **Anonymous Visitor (`Guest`):** Can browse landing pages, view public verified requests, and access terms/policies.
2. **Recipient (`Recipient`):** Can maintain an individual profile, draft assistance requests, upload verification documents, monitor funding progress, and view payout evidence.
3. **Donor (`Donor`):** Can discover verified requests via list or swipe feed, bookmark requests, initiate contributions, download transaction tax receipts, and receive verified outcome notifications.
4. **Verification Officer (`VerificationOfficer`):** Has access to the verification workbench; reviews uploaded documents, inspects OCR outputs, verifies institutional credentials, and records approval/rejection decisions.
5. **Finance Manager (`FinanceManager`):** Oversees payment reconciliation, monitors webhook logs, initiates direct-to-biller payouts, logs UTR evidence, and handles approved refunds.
6. **Support Agent (`SupportAgent`):** Investigates user disputes, reviews reported suspicious requests, and handles user inquiries.
7. **Super Administrator (`SuperAdmin`):** Full system configuration, role assignment, platform revenue oversight, system audit inspection, and emergency pause switches.

---

## 3. Specific Functional Requirements (FR)

### Module 1: Authentication & Identity Management
- **FR-1.1:** The system shall enforce registration with email address, password, phone number, full legal name, and acceptance of Terms of Service & Privacy Policy.
- **FR-1.2:** Passwords must adhere to NIST 800-63B standards: minimum 8 characters, maximum 128 characters, containing at least one uppercase letter, one lowercase letter, one digit, and one special symbol. Passwords shall be hashed using ASP.NET Core Identity's PBKDF2 with HMAC-SHA512.
- **FR-1.3:** The system shall implement short-lived access JWT tokens (lifetime: 15 minutes) and long-lived refresh tokens (lifetime: 7 days) stored in HttpOnly, Secure, SameSite=Strict cookies.
- **FR-1.4:** Refresh tokens must be rotated on every use; if a previously used refresh token is presented, the system shall revoke all active sessions for that user family (Replay Detection).
- **FR-1.5:** Privileged accounts (`SuperAdmin`, `FinanceManager`, `VerificationOfficer`) must have TOTP 2FA enabled before accessing any administrative endpoints.
- **FR-1.6:** The system shall throttle failed login attempts: 5 consecutive failures from a given IP/username shall trigger an exponential backoff lock (5 minutes initial, doubling up to 1 hour).

### Module 2: Recipient Profile & Onboarding
- **FR-2.1:** A Recipient profile must record legal full name, masked government ID type/number (e.g., Aadhaar last 4 digits / PAN), residential city/state, and verified phone number.
- **FR-2.2:** Profile completion to 100% (contact, identity proof upload, address details) shall be a hard prerequisite before submitting any assistance request.
- **FR-2.3:** The system shall segregate recipient PII so that donors browsing the platform only see the recipient's first name, last initial, and general locality (City/State), preventing harassment or predatory targeting.

### Module 3: Financial Assistance Request Management
- **FR-3.1:** The system shall support request creation under two initial categories: `Education` and `Medical`.
- **FR-3.2:** Each request must capture:
  - Title (10 to 100 characters).
  - Narrative story (100 to 3,000 characters).
  - Target amount in INR (minimum ₹1,000; maximum ₹2,50,000 for V1).
  - Institution/Biller details: Official Name, Biller Category, Department/Registration Number, Biller Bank Account / IFSC or Verified UPI ID.
  - Payment Due Date.
- **FR-3.3:** The system shall enforce the strict request state lifecycle defined in Section 5.1 of the PRD.
- **FR-3.4:** State transitions from `Draft` $\to$ `Submitted` $\to$ `UnderReview` $\to$ `Verified` $\to$ `Active` must be strictly server-controlled. Any client attempt to bypass verification shall return `403 Forbidden`.
- **FR-3.5:** An active request shall automatically close to new contributions once `FundedAmount >= TargetAmount` and transition to `FullyFunded`.

### Module 4: Document Upload & Verification Engine
- **FR-4.1:** The backend shall validate uploaded files via magic number byte verification:
  - PDF: `%PDF-` (`25 50 44 46`)
  - JPEG: `FF D8 FF`
  - PNG: `89 50 4E 47 0D 0A 1A 0A`
- **FR-4.2:** Documents must be encrypted at rest using AES-256 in Azure Blob Storage under private containers. Public anonymous read access is completely disabled at the storage account level.
- **FR-4.3:** Document viewing URLs must be generated as Azure Blob Storage Shared Access Signatures (SAS) with an expiry time of exactly 15 minutes, issued only to authorized roles.
- **FR-4.4:** The system shall compute a SHA-256 hash of each uploaded document. If an identical hash exists on any past or present request, the verification officer shall be alerted with a high-priority `DUPLICATE_DOCUMENT_FLAG`.
- **FR-4.5:** The system shall integrate with an OCR pipeline (Azure AI Document Intelligence) to extract:
  - Biller Name.
  - Total Invoice/Fee Amount.
  - Invoice / Roll Number.
  - Bill Date and Due Date.
- **FR-4.6:** A human Verification Officer must explicitly confirm or reject the request with a mandatory audit rationale. Automated AI approval is strictly prohibited.

### Module 5: Donor Discovery & Interaction
- **FR-5.1:** The system shall expose a paginated, filterable API returning `Active` and `PartiallyFunded` requests, supporting filters: Category, Location (State/City), Funding Percentage (e.g., `<25%`, `25-75%`, `>75%`), and Urgency (due date).
- **FR-5.2:** The system shall support a swipe discovery endpoint returning recommended request cards.
- **FR-5.3:** A donor swipe right shall persist an association in the `SavedRequests` table without creating any financial obligation or checkout intent.
- **FR-5.4:** The system shall provide a search query interface with text tokenization indexing titles, categories, and verified institutional tags.

### Module 6: Donation Intent & Checkout Management
- **FR-6.1:** When a donor clicks "Contribute", the client must invoke `POST /api/v1/donations/intents` containing:
  - `RequestId` (GUID)
  - `AssistanceAmount` (decimal)
  - `PlatformTipAmount` (decimal, default 5%, optional 0)
  - `IdempotencyKey` (UUID v4)
- **FR-6.2:** The backend shall validate that:
  $$\text{AssistanceAmount} \le (\text{TargetAmount} - \text{FundedAmount})$$
  If the remaining amount is less than the requested contribution, the system shall reject the intent with error code `EXCEEDS_REMAINING_FUNDING` and propose the exact remaining cap.
- **FR-6.3:** The system creates a `DonationIntent` record with state `Pending` and a 15-minute expiration timestamp.

### Module 7: Payment Gateway & Webhook Processing
- **FR-7.1:** The system shall support payment creation via approved payment gateway (Razorpay / Cashfree) and an internal `SimulatedPaymentProvider` for automated testing and sandbox environments.
- **FR-7.2:** Webhook ingestion (`POST /api/v1/payments/webhook`) must execute the following protocol:
  1. Capture raw payload and request header signature (`X-Razorpay-Signature` or provider equivalent).
  2. Compute expected HMAC-SHA256 signature using the secret key stored in Azure Key Vault.
  3. If signature mismatch: Log security alert and return `401 Unauthorized`.
  4. Check `PaymentWebhooks` table for `WebhookEventId`. If already processed, return `200 OK` immediately (Idempotency).
  5. If new: Begin database transaction, transition `PaymentTransaction` to `Success`, update `Donation` to `Confirmed`, increment `AssistanceRequest.FundedAmount`, post journal entries to Ledger, and commit transaction.
- **FR-7.3:** Client-side return URL redirects shall only display pending UI; under no circumstance shall client redirection update the funding balance.

### Module 8: Direct Payout & Disbursement
- **FR-8.1:** Once an assistance request achieves `FullyFunded`, the system shall queue a `PayoutRecord` in state `ReadyForPayout`.
- **FR-8.2:** The system enforces Maker-Checker separation: The Verification Officer who approved the request cannot be the Finance Manager who approves the payout.
- **FR-8.3:** When the institutional payout is completed, the Finance Manager must record:
  - Disbursement Date & Time.
  - Bank Account / Virtual Account reference.
  - Payment Mode (NEFT / RTGS / IMPS).
  - Bank UTR (Unique Transaction Reference) number.
  - Scanned bank advice / institutional receipt document.
- **FR-8.4:** Upon UTR recording, the request status moves to `Completed` and triggers asynchronous notification to all contributing donors.

### Module 9: Double-Entry Financial Ledger
- **FR-9.1:** The system shall maintain an immutable double-entry ledger consisting of `LedgerAccounts`, `JournalEntries`, and `JournalLineItems`.
- **FR-9.2:** For every verified donor payment, the system creates balanced debit and credit entries:
  - **Debit:** Gateway Clearing Account (Asset)
  - **Credit:** Recipient Escrow Allocation (Liability)
  - **Credit (if applicable):** Platform Tip / Revenue (Equity/Revenue)
  - **Debit (if gateway deducts at source):** Gateway Processing Fee (Expense)
- **FR-9.3:** Total Debits must equal Total Credits for every `JournalEntry`. Any imbalance shall throw a `LedgerImbalanceException` and abort the database transaction.

### Module 10: Administration, Fraud & Audit
- **FR-10.1:** The system shall log every administrative and verification state change into an append-only `AuditLogs` table capturing: `ActorUserId`, `ActionType`, `EntityName`, `EntityId`, `OldValueJson`, `NewValueJson`, `ClientIpAddress`, and `UtcTimestamp`.
- **FR-10.2:** The admin dashboard shall display real-time telemetry: Total Facilitated Volume, Pending Verification Count, Payout Queue Aging, and System Health.
- **FR-10.3:** The platform SuperAdmin shall have access to an emergency circuit-breaker toggle: "Pause New Contributions" and "Pause Payouts" without stopping read traffic.

---

## 4. External Interface Requirements

### 4.1 User Interfaces (UI)
- The user interface shall be a Responsive Single Page Application (SPA) written in Angular (v18+ with Standalone Components & Signals).
- Desktop resolutions $\ge 1280 \times 800$, Tablet $\ge 768 \times 1024$, Mobile $\ge 375 \times 667$.
- Design Tokens: Primary Navy `#14213D`, Royal Blue `#3157D5`, Mint Green `#31C48D`, Background `#F7F8FC`, Cards `#FFFFFF`, Primary Text `#172033`, Secondary Text `#758096`.

### 4.2 API & Communication Interfaces
- RESTful JSON APIs adhering to OpenAPI 3.0 (Swagger).
- Real-time updates delivered via SignalR WebSocket Hubs (`/hubs/request-updates`, `/hubs/verification-queue`).
- TLS 1.3 enforced for all external transit; HTTP traffic redirected to HTTPS with HSTS (`max-age=31536000; includeSubDomains; preload`).

---

## 5. Non-Functional Requirements (NFR)

### 5.1 Performance & Latency
- **NFR-1.1:** 95% of read API requests (`GET /api/v1/requests/*`) shall respond within $\le 200\text{ ms}$ under baseline load (1,000 concurrent users).
- **NFR-1.2:** Redis caching shall be employed for request detail views with cache invalidation triggered on new contributions or status changes.
- **NFR-1.3:** Webhook processing endpoint must return `200 OK` within $\le 500\text{ ms}$ to prevent gateway retry timeouts.

### 5.2 Security & Data Protection (DPDP Act Compliance)
- **NFR-2.1:** All personal identification documents and medical health records are classified as *Strictly Confidential Sensitive Personal Data*.
- **NFR-2.2:** Database connection strings, API secrets, and encryption keys must be pulled dynamically from Azure Key Vault or Environment Secrets; zero hardcoded credentials in source control.
- **NFR-2.3:** SQL Injection prevention via parameterized Entity Framework Core queries; raw dynamic SQL concatenation is prohibited.
- **NFR-2.4:** Content Security Policy (CSP), X-Frame-Options (`DENY`), and X-Content-Type-Options (`nosniff`) headers must be attached to all responses.

### 5.3 Reliability, Availability & Fault Tolerance
- **NFR-3.1:** System availability target: 99.9% uptime during operational hours.
- **NFR-3.2:** Database automated backups: Daily differential backups, weekly full backups, with 5-minute Point-In-Time Recovery (PITR) enabled via Azure SQL.
- **NFR-3.3:** Graceful degradation: If Azure AI Document Intelligence is offline, the document upload and manual verification flow must continue unobstructed with OCR flagged as `ManualReviewRequired`.

