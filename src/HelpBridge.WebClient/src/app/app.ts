import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/services/auth.service';
import { HelpBridgeApiService } from './core/services/helpbridge-api.service';
import { RequestSummary, AssistanceRequest, AdminMetrics } from './core/models/models';

type ViewTab = 'explore' | 'swipe' | 'recipient' | 'admin' | 'how-it-works';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected auth = inject(AuthService);
  protected api = inject(HelpBridgeApiService);

  currentTab = signal<ViewTab>('explore');
  selectedCategory = signal<number>(0); // 0 = All
  searchQuery = signal<string>('');

  // Sample data fallback for instant client presentation
  requests = signal<RequestSummary[]>([
    {
      id: 'req-01',
      title: 'Final Year Mechanical Engineering Tuition',
      categoryName: 'Education',
      recipientDisplayName: 'Rahul P.',
      location: 'Ahmedabad, Gujarat',
      targetAmount: 50000,
      fundedAmount: 35000,
      progressPercentage: 70,
      supporterCount: 7,
      daysRemaining: 14,
      currency: 'INR',
      isVerified: true
    },
    {
      id: 'req-02',
      title: 'Emergency Pediatric Cardiac Surgery Assistance',
      categoryName: 'Medical',
      recipientDisplayName: 'Aarav M. (Parent)',
      location: 'Pune, Maharashtra',
      targetAmount: 120000,
      fundedAmount: 96000,
      progressPercentage: 80,
      supporterCount: 22,
      daysRemaining: 6,
      currency: 'INR',
      isVerified: true
    },
    {
      id: 'req-03',
      title: 'Vocational Nursing Diploma Exam & Lab Fees',
      categoryName: 'Education',
      recipientDisplayName: 'Sneha K.',
      location: 'Bengaluru, Karnataka',
      targetAmount: 28000,
      fundedAmount: 14000,
      progressPercentage: 50,
      supporterCount: 4,
      daysRemaining: 21,
      currency: 'INR',
      isVerified: true
    },
    {
      id: 'req-04',
      title: 'Orthopedic Knee Replacement Rehabilitation',
      categoryName: 'Medical',
      recipientDisplayName: 'Sunita D.',
      location: 'Jaipur, Rajasthan',
      targetAmount: 75000,
      fundedAmount: 30000,
      progressPercentage: 40,
      supporterCount: 9,
      daysRemaining: 18,
      currency: 'INR',
      isVerified: true
    }
  ]);

  // Swipe deck index
  swipeIndex = signal<number>(0);
  currentSwipeCard = computed(() => {
    const list = this.requests();
    if (list.length === 0) return null;
    return list[this.swipeIndex() % list.length];
  });

  // Contribution Modal State
  selectedRequestForDonation = signal<RequestSummary | null>(null);
  donationAmount = signal<number>(1000);
  voluntaryTipPercentage = signal<number>(5);
  tipAmount = computed(() => Math.round(this.donationAmount() * (this.voluntaryTipPercentage() / 100)));
  totalCheckoutAmount = computed(() => this.donationAmount() + this.tipAmount());
  donationSuccessReceipt = signal<any | null>(null);
  isProcessingPayment = signal<boolean>(false);

  // Recipient Draft Form
  draftTitle = signal('Semester Tuition Assistance');
  draftCategory = signal(1);
  draftAmount = signal(45000);
  draftBiller = signal('National Institute of Technology');
  draftBillerAccount = signal('998877665544');
  draftBillerIfsc = signal('SBIN0001234');
  draftStory = signal('Requesting community support to pay my college tuition fees before the semester registration cutoff.');
  uploadedDocumentName = signal<string | null>('college_fee_demand_2026.pdf');
  recipientRequestStatus = signal<string>('Draft');

  // Admin Workbench State
  adminMetrics = signal<AdminMetrics>({
    totalRegisteredUsers: 12540,
    totalAssistanceRequests: 420,
    pendingVerificationCases: 3,
    activeMarketplaceRequests: 48,
    completedRequests: 369,
    pendingPayoutsCount: 4,
    totalContributionsFacilitated: 8450000,
    totalPlatformTipsRevenue: 422500,
    isLedgerBalanced: true,
    ledgerVariance: 0
  });

  filteredRequests = computed(() => {
    let list = this.requests();
    if (this.selectedCategory() === 1) list = list.filter(r => r.categoryName === 'Education');
    if (this.selectedCategory() === 2) list = list.filter(r => r.categoryName === 'Medical');

    const q = this.searchQuery().trim().toLowerCase();
    if (q) {
      list = list.filter(r => r.title.toLowerCase().includes(q) || r.location.toLowerCase().includes(q));
    }
    return list;
  });

  ngOnInit() {
    this.refreshDiscoveryFeed();
  }

  setTab(tab: ViewTab) {
    this.currentTab.set(tab);
  }

  selectCategory(id: number) {
    this.selectedCategory.set(id);
  }

  refreshDiscoveryFeed() {
    this.api.getDiscoveryFeed().subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.requests.set(res.data);
        }
      },
      error: () => {
        // Keeps high quality fallback data
      }
    });
  }

  // Swipe Interactions
  swipeLeft() {
    this.swipeIndex.update(i => i + 1);
  }

  swipeRight() {
    const card = this.currentSwipeCard();
    if (card) {
      this.api.saveRequest(card.id).subscribe({
        error: () => {}
      });
    }
    this.swipeIndex.update(i => i + 1);
  }

  openDonationModal(request: RequestSummary) {
    this.selectedRequestForDonation.set(request);
    this.donationAmount.set(Math.min(1000, request.targetAmount - request.fundedAmount));
    this.donationSuccessReceipt.set(null);
  }

  closeDonationModal() {
    this.selectedRequestForDonation.set(null);
    this.donationSuccessReceipt.set(null);
  }

  setPresetAmount(amount: number) {
    this.donationAmount.set(amount);
  }

  confirmContribution() {
    const req = this.selectedRequestForDonation();
    if (!req) return;

    this.isProcessingPayment.set(true);

    setTimeout(() => {
      // Simulate successful payment confirmation
      const amt = this.donationAmount();
      req.fundedAmount += amt;
      req.progressPercentage = Math.min(100, Math.round((req.fundedAmount / req.targetAmount) * 100));
      req.supporterCount += 1;

      this.donationSuccessReceipt.set({
        paymentId: `pay_sim_${Math.random().toString(36).substring(2, 10)}`,
        amount: amt,
        tip: this.tipAmount(),
        total: this.totalCheckoutAmount(),
        billerName: req.recipientDisplayName,
        timestamp: new Date().toLocaleTimeString()
      });

      // Update admin ledger balance
      this.adminMetrics.update(m => ({
        ...m,
        totalContributionsFacilitated: m.totalContributionsFacilitated + amt,
        totalPlatformTipsRevenue: m.totalPlatformTipsRevenue + this.tipAmount()
      }));

      this.isProcessingPayment.set(false);
    }, 1200);
  }

  // Recipient submission
  submitRecipientRequest() {
    this.recipientRequestStatus.set('Submitted');
    setTimeout(() => {
      this.recipientRequestStatus.set('UnderReview');
    }, 1500);
  }

  // Admin decision
  approvePendingCase() {
    this.adminMetrics.update(m => ({
      ...m,
      pendingVerificationCases: Math.max(0, m.pendingVerificationCases - 1),
      activeMarketplaceRequests: m.activeMarketplaceRequests + 1
    }));
    alert('Verification approved! The request has been published to the verified discovery marketplace.');
  }
}
