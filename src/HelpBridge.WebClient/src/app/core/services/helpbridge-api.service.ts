import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiResponse,
  RequestSummary,
  AssistanceRequest,
  DonationIntentResponse,
  DonationReceipt,
  VerificationQueueItem,
  AdminMetrics
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class HelpBridgeApiService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7080/api/v1';

  // --- Discovery ---
  getDiscoveryFeed(categoryId?: number, search?: string): Observable<ApiResponse<RequestSummary[]>> {
    let params = new HttpParams();
    if (categoryId) params = params.set('categoryId', categoryId.toString());
    if (search) params = params.set('search', search);

    return this.http.get<ApiResponse<RequestSummary[]>>(`${this.baseUrl}/discovery/feed`, { params });
  }

  getSwipeDeck(limit: number = 10): Observable<ApiResponse<RequestSummary[]>> {
    return this.http.get<ApiResponse<RequestSummary[]>>(`${this.baseUrl}/discovery/swipe?limit=${limit}`);
  }

  saveRequest(requestId: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/discovery/${requestId}/save`, {});
  }

  // --- Recipient Requests ---
  createRequest(data: any): Observable<ApiResponse<AssistanceRequest>> {
    return this.http.post<ApiResponse<AssistanceRequest>>(`${this.baseUrl}/requests`, data);
  }

  getRequestById(id: string): Observable<ApiResponse<AssistanceRequest>> {
    return this.http.get<ApiResponse<AssistanceRequest>>(`${this.baseUrl}/requests/${id}`);
  }

  getMyRequests(): Observable<ApiResponse<AssistanceRequest[]>> {
    return this.http.get<ApiResponse<AssistanceRequest[]>>(`${this.baseUrl}/requests/my`);
  }

  uploadDocument(requestId: string, file: File, documentType: number): Observable<ApiResponse<any>> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType.toString());

    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/requests/${requestId}/documents`, formData);
  }

  submitRequest(requestId: string): Observable<ApiResponse<AssistanceRequest>> {
    return this.http.post<ApiResponse<AssistanceRequest>>(`${this.baseUrl}/requests/${requestId}/submit`, {});
  }

  // --- Donations & Payments ---
  createDonationIntent(data: {
    requestId: string;
    assistanceAmount: number;
    platformTipAmount: number;
    idempotencyKey?: string;
  }): Observable<ApiResponse<DonationIntentResponse>> {
    return this.http.post<ApiResponse<DonationIntentResponse>>(`${this.baseUrl}/donations/intents`, {
      ...data,
      idempotencyKey: data.idempotencyKey || crypto.randomUUID().replace(/-/g, '')
    });
  }

  captureSimulatedPayment(intentId: string, gatewayOrderId: string): Observable<ApiResponse<DonationReceipt>> {
    return this.http.post<ApiResponse<DonationReceipt>>(`${this.baseUrl}/payments/simulated/capture`, {
      intentId,
      gatewayOrderId
    });
  }

  getMyDonations(): Observable<ApiResponse<DonationReceipt[]>> {
    return this.http.get<ApiResponse<DonationReceipt[]>>(`${this.baseUrl}/donations/my`);
  }

  // --- Verification ---
  getVerificationQueue(): Observable<ApiResponse<VerificationQueueItem[]>> {
    return this.http.get<ApiResponse<VerificationQueueItem[]>>(`${this.baseUrl}/verification/queue`);
  }

  recordVerificationDecision(caseId: string, decision: 'Approve' | 'Reject' | 'RequestInfo', notes: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/verification/${caseId}/decision`, {
      caseId,
      decision,
      notes
    });
  }

  // --- Payouts ---
  getPayoutQueue(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(`${this.baseUrl}/payouts/queue`);
  }

  confirmPayout(payoutId: string, bankUtrNumber: string, paymentMode: string = 'NEFT'): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/payouts/${payoutId}/confirm`, {
      payoutId,
      bankUtrNumber,
      paymentMode
    });
  }

  // --- Admin ---
  getAdminDashboardMetrics(): Observable<ApiResponse<AdminMetrics>> {
    return this.http.get<ApiResponse<AdminMetrics>>(`${this.baseUrl}/admin/dashboard`);
  }
}
