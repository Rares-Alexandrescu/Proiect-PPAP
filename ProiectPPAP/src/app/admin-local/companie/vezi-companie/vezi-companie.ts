import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';


import { environment } from '../../../../environments/environment';

export interface CompanieLocal {
  companie_Id: number;
  nume_Companie: string;
  email: string;
  numar_Telefon: string;
}

export interface Angajat {
  id: number;
  nume: string;
  prenume: string;
  email: string;
  numar_Telefon: string;
}

export interface VeziCompanieResponse {
  companie: CompanieLocal;
  angajati: Angajat[];
}

@Component({
  selector: 'app-vezi-companie',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-companie.html',
  styleUrl: './vezi-companie.scss',
})
export class VeziCompanieComponent implements OnInit {
  companie = signal<CompanieLocal | null>(null);
  angajati = signal<Angajat[]>([]);

  private http = inject(HttpClient);
  private router = inject(Router);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaDateleCompaniei();

    const stareNavigare = history.state;

    if (stareNavigare && stareNavigare.mesajSucces) {
      this.alertaSucces.set(stareNavigare.mesajSucces);
      setTimeout(() => {
        this.alertaSucces.set('');
      }, 3000);
      window.history.replaceState({}, document.title);
    }

    if (stareNavigare && stareNavigare.mesajEroare) {
      this.alertaEroare.set(stareNavigare.mesajEroare);
      setTimeout(() => {
        this.alertaEroare.set('');
      }, 3000);
      window.history.replaceState({}, document.title);
    }
  }

  incarcaDateleCompaniei(): void {
    this.http.get<VeziCompanieResponse>(`${environment.apiUrl}/admin-companie/vezi-companie`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        this.companie.set(dateDinBackend.companie);
        this.angajati.set(dateDinBackend.angajati || []);
        console.log('2. Angajați găsiți:', this.angajati().length);
      },
      error: (eroare) => {
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea datelor companiei:', eroare);
          this.alertaEroare.set('Nu s-au putut încărca datele companiei.');
        }
      }
    });
  }

  adaugaAngajat(): void {
    this.router.navigate(['/admin-companie/adauga-angajat']);
  }

  stergeAngajat(id: number): void {
    if (confirm('Ești sigur că vrei să elimini acest angajat din compania ta?')) {

      this.http.delete<any>(`${environment.apiUrl}/admin-companie/sterge-angajat/${id}`).subscribe({
        next: (raspuns) => {
          this.alertaSucces.set(raspuns.message || 'Angajatul a fost eliminat cu succes!');
          this.incarcaDateleCompaniei();

          setTimeout(() => {
            this.alertaSucces.set('');
          }, 3000);
        },
        error: (err) => {
          console.error('A apărut o eroare la ștergere:', err);
          if (err.error?.eroriIdentificator?.mesajEroare) {
            this.alertaEroare.set(err.error.eroriIdentificator.mesajEroare[0]);
          } else {
            this.alertaEroare.set('Nu s-a putut elimina angajatul. Verifică consola.');
          }
          setTimeout(() => this.alertaEroare.set(''), 4000);
        }
      });
    }
  }
}
