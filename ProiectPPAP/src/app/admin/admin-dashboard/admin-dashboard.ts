import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
//trebuie sa vad ce pun si aici sa verifice daca e admin ....

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.scss']
})
export class AdminDashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  ngOnInit(): void {
    this.verificaAcces();
  }

  verificaAcces(): void {
    this.http.get(`${environment.apiUrl}/admin`).subscribe({
      next: () => {
      },
      error: (eroare) => {
        console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
        this.router.navigate(['/dashboard']);
      }
    });
  }
}
