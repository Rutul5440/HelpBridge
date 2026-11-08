# HelpBridge — Verified Financial Assistance Platform

> **Core Promise:** *Every contribution has a verified purpose. Every transaction has a traceable outcome.*

HelpBridge is a high-assurance financial assistance marketplace connecting verified individuals facing urgent educational and medical obligations with donors. Built with Clean Architecture as a Modular Monolith in ASP.NET Core (.NET 9 C#) and Angular 20, the platform guarantees that 100% of assistance contributions are remitted directly to verified billers and institutions, tracked by an infallible double-entry accounting ledger.

---

## 🏛️ System Architecture

```
                  HELPBRIDGE PLATFORM
                           |
             ANGULAR 20 STANDALONE SPA (Signals)
                           |
       ASP.NET CORE 9 MODULAR MONOLITH (Web API)
       +-------------------+-------------------+
       |                   |                   |
    Identity           Assistance          Donations
       |                   |                   |
  Verification          Ledger             Payouts
       +-------------------+-------------------+
                           |
            SQL SERVER + DOUBLE-ENTRY LEDGER
       +-------------------+-------------------+
       |                   |                   |
   Azure Blob            SignalR           Simulated
   (Encrypted)         (Real-Time)          Gateway
```

---

## 🚀 Quick Start Guide

### 1. Prerequisites
- **.NET 9 SDK** (`dotnet --version`)
- **Node.js 20+** and **npm** (`node -v && npm -v`)
- **SQL Server / LocalDB** (included in Visual Studio)

### 2. Running the Backend API
```powershell
cd d:\HelpBridge
dotnet run --project src/HelpBridge.Api
```
- **API URL:** `http://localhost:5037` / `https://localhost:7099`
- **Swagger Documentation:** `http://localhost:5037/swagger`
- **Default Seeded Admin Account:**
  - **Email:** `admin@helpbridge.org`
  - **Password:** `HelpBridge@Admin2026!`
  - **Roles:** `SuperAdmin`, `VerificationOfficer`, `FinanceManager`

### 3. Running the Angular Client
```powershell
cd d:\HelpBridge\src\HelpBridge.WebClient
npm start
```
- **Web App URL:** `http://localhost:4200`
- API calls and SignalR WebSockets automatically proxy to `http://localhost:5037`.

### 4. Running the Automated Test Suite
```powershell
cd d:\HelpBridge
dotnet test HelpBridge.sln
```
- **`HelpBridge.UnitTests`:** 7 business rule tests verifying state transitions, funding limits, double-entry invariance ($\sum \text{Debits} = \sum \text{Credits}$), and Maker-Checker enforcement.
- **`HelpBridge.IntegrationTests`:** Full end-to-end integration test validating the entire live user journey:
  $$\text{Recipient Reg} \to \text{Create Request} \to \text{Doc Upload} \to \text{Officer Approval} \to \text{Marketplace Feed} \to \text{Donor Intent} \to \text{Simulated Capture} \to \text{Double-Entry Ledger Verified}$$

---

## 📁 Repository Structure

```
d:\HelpBridge\
│
├── docs/                                  # Foundational Specifications
│   ├── 01_PRODUCT_REQUIREMENTS_DOCUMENT_PRD.md
│   ├── 02_SOFTWARE_REQUIREMENTS_SPECIFICATION_SRS.md
│   ├── 03_TECHNICAL_ARCHITECTURE_AND_SYSTEM_DESIGN.md
│   └── 04_DEVELOPMENT_ROADMAP_AND_SPRINT_PLAN.md
│
├── src/
│   ├── HelpBridge.Domain/                 # Core Entities, Enums, Double-Entry Rules
│   ├── HelpBridge.Application/            # DTOs, CQRS Interfaces, Common Envelopes
│   ├── HelpBridge.Infrastructure/         # EF Core, Identity, Simulated Gateway, AI OCR
│   ├── HelpBridge.Api/                    # Versioned Controllers (v1), Hubs, Middleware
│   └── HelpBridge.WebClient/              # Angular 20 Standalone Application
│
└── tests/
    ├── HelpBridge.UnitTests/              # Domain Business Logic Tests
    └── HelpBridge.IntegrationTests/       # Live API Test Server Workflow Tests
```

---

## 🔒 Security & Integrity Highlights

- **Direct-to-Biller Payouts:** Funds accumulate in escrow liability accounts and disburse directly to verified college/hospital bank accounts.
- **Decoupled Financial Lifecycles:** Donor contribution payment is kept separate from institutional payout confirmation.
- **Cryptographic Webhooks:** HMAC SHA-256 validation with duplicate event rejection.
- **Double-Entry Ledger:** Every rupee collected and disbursed generates balanced journal entries.
- **Time-Limited Document Access:** Supporting documents are private with 15-minute expiring SAS URLs.
- **AI OCR & Duplicate Check:** SHA-256 document hashing flags recycled bills; regex/AI engine cross-verifies claimed amounts against invoice text.

