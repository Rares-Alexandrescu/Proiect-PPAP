import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';


export interface FurnizorLocal {
  furnizor_Id: number;
  nume_Furnizor: string;
  email: string;
  numar_Telefon: string;
}


export interface PiesaFurnizor {
  piese_Id: number;
  furnizor_Id: number;
  pret_Cumparare: number;
  nume_Piesa: string;
}


export interface VeziPieseResponse {
  furnizor: FurnizorLocal;
  piese: PiesaFurnizor[];
}

@Component({
  selector: 'app-vezi-piese',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-piese.html',
  styleUrl: './vezi-piese.scss',
})
export class VeziPieseComponent implements OnInit {
  furnizor = signal<FurnizorLocal | null>(null);
  piese = signal<PiesaFurnizor[]>([]);

  private http = inject(HttpClient);
  private router = inject(Router);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaDatelePieselor();

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

  incarcaDatelePieselor(): void {
    this.seIncarca.set(true);

    this.http.get<VeziPieseResponse>(`${environment.apiUrl}/admin-furnizor/vezi-piese`).subscribe({
      next: (dateDinBackend) => {

        this.furnizor.set((dateDinBackend as any).Furnizor || dateDinBackend.furnizor);
        this.piese.set((dateDinBackend as any).Piese || dateDinBackend.piese || []);

        console.log('2. Piese găsite:', this.piese().length);
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea datelor furnizorului:', eroare);
          this.alertaEroare.set('Nu s-au putut încărca piesele din baza de date.');
        }
      }
    });
  }

  adaugaPiesa(): void {
    this.router.navigate(['/admin-furnizor/adauga-piesa']);
  }

  editeazaPiesa(idPiesa: number): void {
    this.router.navigate(['/admin-furnizor/edit-piesa', idPiesa]);
  }

  stergePiesa(idPiesa: number): void {
    if (confirm('Ești sigur că vrei să ștergi această piesă din portofoliul tău?')) {

      this.http.delete<any>(`${environment.apiUrl}/admin-furnizor/delete-piesa/${idPiesa}`).subscribe({
        next: (raspuns) => {
          this.alertaSucces.set(raspuns.message || 'Piesa a fost ștearsă cu succes!');
          this.incarcaDatelePieselor();

          setTimeout(() => {
            this.alertaSucces.set('');
          }, 3000);
        },
        error: (err) => {
          console.error('A apărut o eroare la ștergere:', err);
          if (err.error?.eroriCampuri?.mesajEroare) {
            this.alertaEroare.set(err.error.eroriCampuri.mesajEroare[0]);
          } else if (err.error?.message) {
            this.alertaEroare.set(err.error.message);
          } else {
            this.alertaEroare.set('Nu s-a putut șterge piesa. Verifică consola.');
          }
          setTimeout(() => this.alertaEroare.set(''), 4000);
        }
      });
    }
  }
}
