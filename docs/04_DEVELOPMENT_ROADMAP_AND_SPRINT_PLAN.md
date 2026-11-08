# HelpBridge — Master Development Roadmap & Execution Blueprint
**Document Version:** 1.0.0  
**Timeline:** 15–17 Weeks (MVP Release Target)  
**Execution Methodology:** Iterative Milestones with Continuous Verification  

---

## 1. Master Phase Overview

```
Week  1: [Phase 1: Architecture & Specifications]
Week 2-3: [Phase 2: UI/UX Design System & Prototypes]
Week 4-5: [Phase 3: Backend Foundation & Identity Engine]
Week 6-7: [Phase 4: Recipient Portal & Request Management]
Week 8-9: [Phase 5: Verification Workbench & Admin Ops]
Week 10-11: [Phase 6: Donor Experience & Swipe Discovery]
Week 12-14: [Phase 7: Payment Engine, Webhooks & Ledger]
Week 15-17: [Phase 8: Security Hardening, Audit & Go-Live]
```

---

## 2. Phase-by-Phase Sprint Breakdown

### Phase 1: Product Planning, Specifications & Architecture (Week 1)
*Goal: Establish absolute consensus on data models, business rules, API contracts, and fund flow.*
- [x] Author Product Requirements Document (PRD v1.0).
- [x] Author Software Requirements Specification (SRS v1.0).
- [x] Author Technical Architecture & System Design Document (v1.0).
- [x] Establish Master Development Roadmap & Execution Blueprint.
- [ ] Define API contracts via Swagger/OpenAPI spec definition.
- [ ] Set up Git repository branching strategy (`main`, `develop`, `feature/*`) and commit conventions.
- **Phase Exit Gate:** All foundational documents reviewed, signed off, and versioned in repository `/docs`.

---

### Phase 2: UI/UX & Design System (Weeks 2–3)
*Goal: Create a pixel-perfect, accessible, and responsive visual design system in Angular.*
- **Sprint 2.1: Foundations & Token Library**
  - Implement CSS Custom Properties / Tailwind configuration for color palette (`#14213D`, `#3157D5`, `#31C48D`, `#F7F8FC`).
  - Configure typography (`Inter`), typography scale, spacing tokens, and card elevation shadows.
  - Create reusable atomic components: Button, Input, Select, Badge, Card, Modal, Progress Bar, Toast Notification.
- **Sprint 2.2: Core Screen Layouts & Interactive Shell**
  - Public Shell: Navigation bar, Hero section, Category filters, How It Works, Footer.
  - Recipient Shell: Authenticated sidebar, Request Progress cards, Document upload dropzone.
  - Donor Shell: Discovery list feed, Touch/pointer-enabled Swipe Deck card stack.
  - Admin Shell: Operational KPI counter grid, Split-screen Document Verification workbench.
- **Phase Exit Gate:** Complete responsive mockups and component showcase running locally in Angular.

---

### Phase 3: Backend Foundation & Identity Engine (Weeks 4–5)
*Goal: Stand up the ASP.NET Core solution, database migrations, and bulletproof authentication.*
- **Sprint 3.1: Solution Scaffolding & Database Setup**
  - Initialize .NET solution with Clean Architecture projects (`Domain`, `Application`, `Infrastructure`, `Api`).
  - Configure Entity Framework Core with SQL Server provider.
  - Implement base entities (`BaseEntity`, `IAggregateRoot`), audit fields, and UTC datetime interceptor.
  - Run initial EF Core migration creating Identity tables (`Users`, `Roles`, `UserRoles`, `UserProfiles`).
- **Sprint 3.2: Authentication & Security Middleware**
  - Configure ASP.NET Core Identity with PBKDF2 HMAC-SHA512 hashing.
  - Implement JWT access token generator and secure HttpOnly cookie refresh token rotation.
  - Enforce TOTP 2FA for administrative and finance roles.
  - Implement global exception handling middleware and standard envelope response filter (`ApiResponse<T>`).
  - Configure ASP.NET Core RateLimiter middleware to protect login and registration endpoints.
- **Phase Exit Gate:** End-to-end integration test verifying registration, login, token refresh, and role authorization.

---

### Phase 4: Recipient & Request Management (Weeks 6–7)
*Goal: Enable recipients to draft assistance requests, upload documents, and track approval status.*
- **Sprint 4.1: Request Lifecycle & Domain Logic**
  - Implement `AssistanceRequest` aggregate root with private setters and domain state transition methods.
  - Implement CQRS handlers: `CreateRequestCommand`, `UpdateRequestCommand`, `SubmitRequestCommand`.
  - Enforce business validation: Category constraints, target amount thresholds, institutional biller details.
- **Sprint 4.2: Document Upload & Azure Blob Integration**
  - Implement `AzureBlobStorageService` with private container isolation.
  - Enforce file security: MIME type verification, magic number header validation, 10MB size restriction.
  - Generate SHA-256 hash on upload and check against existing hashes to detect recycled documents.
  - Implement time-limited SAS token generator (15-minute expiration) for authorized document retrieval.
- **Phase Exit Gate:** Recipient can create a draft request, upload tuition or medical bills, and transition to `Submitted`.

---

### Phase 5: Verification Engine & Admin Dashboard (Weeks 8–9)
*Goal: Equip Verification Officers and Admins with a rapid, fraud-resilient verification workbench.*
- **Sprint 5.1: Verification Workbench & OCR Pipeline**
  - Implement `VerificationCases` and `VerificationActions` domain logic.
  - Implement `IVerificationService` integrating Azure AI Document Intelligence for automated field extraction.
  - Build split-pane Verification Officer UI: Document PDF/Image viewer on the left, OCR comparison and decision controls on the right.
- **Sprint 5.2: Maker-Checker Approval & Fraud Flagging**
  - Enforce role security: Only authenticated users with `Role(VerificationOfficer)` can view or act on cases.
  - Implement decision actions: `Approve` (transitions request to `Verified` $\to$ `Active`), `Reject`, and `RequestAdditionalInfo`.
  - Implement immutable `AuditLog` interceptor capturing every state transition with officer ID, IP address, and rationale.
- **Phase Exit Gate:** Submitted request can be inspected, compared against OCR findings, approved by an officer, and published to the marketplace.

---

### Phase 6: Donor Experience & Discovery (Weeks 10–11)
*Goal: Build an engaging, transparent discovery feed and interactive swipe card experience.*
- **Sprint 6.1: Discovery Engine & Public Feed**
  - Implement cached discovery query (`GetDiscoveryFeedQuery`) with Redis backing.
  - Support category filters (Education, Medical), progress filters, and search indexing.
  - Render high-conversion verified request cards with verified badges, real-time progress bars, and days remaining.
- **Sprint 6.2: Swipe Discovery Deck**
  - Build touch/mouse-gesture swipe card deck component in Angular.
  - Swipe Right: Persists to `SavedRequests` table via `SaveRequestCommand` without charging.
  - Swipe Left: Advances card stack.
  - Tap: Opens Transparency Modal displaying verified biller details and verification audit trail.
- **Phase Exit Gate:** Donors can discover verified requests smoothly on mobile and desktop and bookmark causes.

---

### Phase 7: Payment Engine, Webhooks & Ledger (Weeks 12–14)
*Goal: Build the financial backbone: donation intents, webhook verification, ledger accounting, and direct-to-biller payouts.*
- **Sprint 7.1: Donation Intents & Idempotency**
  - Implement `CreateDonationIntentCommand` enforcing remaining funding cap check.
  - Implement `IdempotencyFilter` using UUID v4 keys to prevent duplicate checkout submissions.
  - Create transparent checkout modal with explicit breakdown (Assistance + 0% hidden fee + optional tip).
- **Sprint 7.2: Payment Gateway Integration & Simulated Provider**
  - Implement `IPaymentProvider` abstraction with two implementations:
    1. `SimulatedPaymentProvider` (instant test transitions for local development & automated CI/CD).
    2. `RazorpayPaymentProvider` (production-grade webhook and order creation).
  - Implement HMAC SHA-256 signature verification in webhook controller (`/api/v1/payments/webhook`).
  - Ensure idempotent webhook handling via unique `EventId` logging in `PaymentWebhooks`.
- **Sprint 7.3: Double-Entry Ledger & Direct Payouts**
  - Implement `LedgerEngine` posting balanced journal entries on successful payments.
  - Implement `PayoutsQueue` for `FullyFunded` requests with Maker-Checker dual authorization.
  - Record bank UTR number and institutional receipt; transition request to `Completed`.
  - Broadcast real-time funding updates via SignalR hub.
- **Phase Exit Gate:** End-to-end contribution completed via simulated/gateway payment, ledger balanced to ₹0.00 variance, and direct payout confirmed with bank UTR.

---

### Phase 8: Production Hardening, Security, QA & Deployment (Weeks 15–17)
*Goal: Rigorous penetration testing, performance benchmarking, legal compliance review, and cloud deployment.*
- **Sprint 8.1: Security Hardening & Vulnerability Mitigation**
  - Execute automated test suite covering OWASP Top 10: IDOR checks, SQL injection, XSS, CSRF, broken access control.
  - Validate short-lived SAS token timeouts and verify no public blob access.
  - Ensure zero PII leakage in public API responses (DPDP Act 2023 compliance).
- **Sprint 8.2: Load Testing & Performance Optimization**
  - Load test critical endpoints using k6 (1,000 concurrent virtual users hitting discovery and webhook endpoints).
  - Benchmark database indexes and optimize EF Core query execution plans.
  - Test Redis cache eviction and failover behavior.
- **Sprint 8.3: Infrastructure Deployment & Production Launch**
  - Provision Azure App Service (Linux, .NET runtime), Azure SQL Database, Azure Blob Storage, Azure Key Vault, and Application Insights.
  - Set up CI/CD GitHub Actions pipelines for automated test execution, build, and zero-downtime deployment.
  - Perform dress-rehearsal deployment in staging environment with simulated payment end-to-end verification.
- **Phase Exit Gate:** All DoD criteria satisfied; production environment operational, verified, and ready for pilot users.

---

## 3. Risk Management & Mitigation Matrix

| Risk Event | Severity | Likelihood | Mitigation Strategy |
|---|---|---|---|
| **Payment Gateway Delays in Approving Fund Flow** | High | Medium | Built-in `SimulatedPaymentProvider` allows 100% of functional development, QA, and UI testing to complete without blocking on external banking approvals. |
| **Recycled or Forged Bills Submitted by Bad Actors** | High | High | Mandatory SHA-256 document hashing flags exact duplicates instantly; OCR cross-verifies biller names and amounts; human verification officer must sign off. |
| **Race Condition on Final Donation Exceeding Goal** | Medium | Medium | DB-level row versioning and serializable database transaction ensure the final contribution is capped exactly to remaining balance. |
| **Regulatory Misclassification (FCRA Violations)** | Critical | Low | Hardcode platform restriction to INR domestic transactions only in V1; international donor cards and currencies strictly disabled until V2. |

