export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
  traceId: string;
  timestampUtc: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  expiresInSeconds: number;
}

export interface UserProfile {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  city: string;
  state: string;
  preferredCurrency: string;
  identityVerified: boolean;
  roles: string[];
}

export interface RequestSummary {
  id: string;
  title: string;
  categoryName: string;
  recipientDisplayName: string;
  location: string;
  targetAmount: number;
  fundedAmount: number;
  progressPercentage: number;
  supporterCount: number;
  daysRemaining: number;
  currency: string;
  isVerified: boolean;
}

export interface AssistanceRequest {
  id: string;
  recipientId: string;
  recipientDisplayName: string;
  city: string;
  state: string;
  categoryId: number;
  categoryName: string;
  title: string;
  story: string;
  targetAmount: number;
  fundedAmount: number;
  remainingAmount: number;
  progressPercentage: number;
  currency: string;
  status: string;
  dueDate: string;
  billerName: string;
  billerReferenceNumber: string;
  supporterCount: number;
  createdUtc: string;
  documents: RequestDocument[];
}

export interface RequestDocument {
  id: string;
  documentType: string;
  originalFileName: string;
  downloadUrl: string;
  fileSizeBytes: number;
}

export interface DonationIntentResponse {
  intentId: string;
  gatewayOrderId: string;
  providerName: string;
  assistanceAmount: number;
  tipAmount: number;
  totalAmount: number;
  currency: string;
  expiresUtc: string;
}

export interface DonationReceipt {
  donationId: string;
  requestId: string;
  requestTitle: string;
  amount: number;
  tipAmount: number;
  totalPaid: number;
  currency: string;
  paidUtc: string;
  gatewayPaymentId: string;
  status: string;
}

export interface VerificationQueueItem {
  caseId: string;
  requestId: string;
  categoryName: string;
  recipientName: string;
  title: string;
  claimedAmount: number;
  submittedUtc: string;
  documentCount: number;
  duplicateDocumentFlag: boolean;
  status: string;
}

export interface AdminMetrics {
  totalRegisteredUsers: number;
  totalAssistanceRequests: number;
  pendingVerificationCases: number;
  activeMarketplaceRequests: number;
  completedRequests: number;
  pendingPayoutsCount: number;
  totalContributionsFacilitated: number;
  totalPlatformTipsRevenue: number;
  isLedgerBalanced: boolean;
  ledgerVariance: number;
}
