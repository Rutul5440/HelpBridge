import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { ApiResponse, AuthResponse, UserProfile } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7080/api/v1/auth'; // default fallback or relative

  currentUser = signal<UserProfile | null>(null);
  token = signal<string | null>(localStorage.getItem('hb_token'));

  isLoggedIn = computed(() => !!this.token());
  roles = computed(() => this.currentUser()?.roles ?? []);
  isRecipient = computed(() => this.roles().includes('Recipient'));
  isDonor = computed(() => this.roles().includes('Donor'));
  isAdmin = computed(() => this.roles().some(r => r === 'SuperAdmin' || r === 'VerificationOfficer' || r === 'FinanceManager'));

  constructor() {
    if (this.token()) {
      this.fetchCurrentUser().subscribe({
        error: () => this.logout()
      });
    }
  }

  setBaseUrl(url: string) {
    this.apiUrl = `${url}/api/v1/auth`;
  }

  login(credentials: { email: string; password: string }): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.setSession(res.data);
        }
      })
    );
  }

  register(data: any): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/register`, data).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.setSession(res.data);
        }
      })
    );
  }

  fetchCurrentUser(): Observable<ApiResponse<UserProfile>> {
    return this.http.get<ApiResponse<UserProfile>>(`${this.apiUrl}/me`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.currentUser.set(res.data);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('hb_token');
    localStorage.removeItem('hb_user');
    this.token.set(null);
    this.currentUser.set(null);
  }

  private setSession(auth: AuthResponse): void {
    localStorage.setItem('hb_token', auth.accessToken);
    this.token.set(auth.accessToken);
    this.currentUser.set({
      userId: auth.userId,
      email: auth.email,
      firstName: auth.fullName.split(' ')[0] ?? '',
      lastName: auth.fullName.split(' ')[1] ?? '',
      fullName: auth.fullName,
      city: '',
      state: '',
      preferredCurrency: 'INR',
      identityVerified: true,
      roles: auth.roles
    });
  }
}
