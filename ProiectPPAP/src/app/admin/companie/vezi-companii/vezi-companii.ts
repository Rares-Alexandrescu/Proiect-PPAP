import { Component ,OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../../environments/environment'; 

export interface Companie {
  companie_Id: number;
  nume_Companie: string;
  email: string;
  numar_Telefon: string;
  numeAdminLocal: string;
  prenumeAdminLocal: string;
}

@Component({
  selector: 'app-vezi-companii',
  imports: [CommonModule],
  templateUrl: './vezi-companii.html',
  styleUrl: './vezi-companii.scss',
})
export class VeziCompaniiComponent implements OnInit {

  companii: Companie[] = [];
  private http = inject(HttpClient);
  private router = inject(Router);


  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaCompanii();

    const stareNavigare = history.state;

    if (stareNavigare && stareNavigare.mesajSucces) {
      this.alertaSucces.set(stareNavigare.mesajSucces);
      setTimeout(() => {
        this.alertaSucces.set('');
      }, 3000);
    }

    if (stareNavigare && stareNavigare.mesajEroare) {
      this.alertaEroare.set(stareNavigare.mesajEroare);
      setTimeout(() => {
        this.alertaEroare.set('');
      }, 3000);
    }
  }

  incarcaCompanii(): void {
    this.http.get<Companie[]>(`${environment.apiUrl}/admin/vezi-companii`).subscribe({
      next: (dateDinBackend) => {
        this.companii = dateDinBackend;
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea companiilor:', eroare);
        }
      }
    });
  }

  editeazaCompanie(id: number): void {
    this.router.navigate(['/admin/edit-companie', id]);
  }

  adaugaCompanie(): void {
    this.router.navigate(['/admin/adauga-companie']);
  }

  stergeCompanie(id: number): void {
    if (confirm('Ești sigur că vrei să ștergi această companie? Toate datele vor fi pierdute!')) {

      this.http.delete(`${environment.apiUrl}/admin/delete-companie/${id}`).subscribe({
        next: () => {
          this.alertaSucces.set('Compania a fost ștearsă cu succes!');
          this.incarcaCompanii();

          setTimeout(() => {
            this.alertaSucces.set('');
          }, 3000);
        },
        error: (err) => {
          console.error('A apărut o eroare la ștergere:', err);
          this.alertaEroare.set('Nu s-a putut șterge compania. Verifică consola.');
        }
      });
    }

  }
}
