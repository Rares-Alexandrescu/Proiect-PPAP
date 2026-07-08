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

  ngOnInit() {

    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/login'], { queryParams: this.route.snapshot.queryParams });
      return;
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
}
