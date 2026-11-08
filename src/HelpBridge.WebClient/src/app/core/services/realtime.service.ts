import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface ContributionBroadcast {
  requestId: string;
  contributionAmount: number;
  newFundedAmount: number;
  progressPercentage: number;
  supporterCount: number;
  status: string;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private hubConnection: signalR.HubConnection | null = null;
  isConnected = signal<boolean>(false);
  latestContribution = signal<ContributionBroadcast | null>(null);

  constructor() {
    this.startConnection();
  }

  private startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/funding')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        console.log('[HelpBridge SignalR] Connected to FundingHub');
      })
      .catch((err) => {
        this.isConnected.set(false);
        console.warn('[HelpBridge SignalR] Could not connect to FundingHub (Backend may be offline):', err.message);
      });

    this.hubConnection.on('ContributionReceived', (data: ContributionBroadcast) => {
      this.latestContribution.set(data);
    });

    this.hubConnection.onclose(() => {
      this.isConnected.set(false);
    });
  }

  subscribeToRequest(requestId: string): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('SubscribeToRequest', requestId);
    }
  }

  unsubscribeFromRequest(requestId: string): void {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.hubConnection.invoke('UnsubscribeFromRequest', requestId);
    }
  }
}

