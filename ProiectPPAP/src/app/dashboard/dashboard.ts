import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/AuthService/auth';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  dateDashboard = signal<any>(null);
  loading = signal<boolean>(true);
  eroare = signal<string>('');

  mesajSuccesVerificat = signal(false);
  loadingEmail = signal(false);
  mesajEmail = signal<{ text: string, tip: 'succes' | 'eroare' } | null>(null);

  ngOnInit() {

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: this.route.snapshot.queryParams });
      return;
    }

    const aFostConfirmat = this.route.snapshot.queryParamMap.get('confirmat') === 'true';

    if (aFostConfirmat) {
      this.mesajSuccesVerificat.set(true);
      this.router.navigate([], { replaceUrl: true });
    }
    this.incarcaDate();
  }

  incarcaDate() {

    this.http.get(`${environment.apiUrl}/dashboard`)
      .subscribe({
        next: (response) => {
          console.log("Datele primite de la API sunt:", response);
          this.dateDashboard.set(response);
          this.loading.set(false);
        },
        error: (err) => {
          this.eroare.set('Nu ai acces sau nu ești logat. Te rugăm să te autentifici.');
          this.loading.set(false);
        }
      });
  }

  retrimiteEmail() {
    this.loadingEmail.set(true);
    this.mesajEmail.set(null);

    this.http.post(`${environment.apiUrl}/resend-confirmare`, {}).subscribe({
      next: (response: any) => {
        this.loadingEmail.set(false);
        this.mesajEmail.set({ text: response.mesaj || "Email retrimis cu succes!", tip: 'succes' });
      },
      error: (err) => {
        this.loadingEmail.set(false);
        this.mesajEmail.set({ text: "A apărut o eroare la trimiterea emailului.", tip: 'eroare' });
      }
    });
  }
}
