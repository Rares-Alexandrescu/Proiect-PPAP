import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../../environments/environment';

export interface Furnizor {
  furnizor_Id: number;
  nume_Furnizor: string;
  email_Furnizor: string;
  numar_Telefon: string;
  nume_Admin_Furnizor: string;
  prenume_Admin_Furnizor: string;
}

@Component({
  selector: 'app-vezi-furnizorii',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-furnizorii.html',
  styleUrl: './vezi-furnizorii.scss', 
})
export class VeziFurnizoriComponent implements OnInit {

  furnizori = signal<Furnizor[]>([]);
  private http = inject(HttpClient);
  private router = inject(Router);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaFurnizori();

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

  incarcaFurnizori(): void {
    this.seIncarca.set(true);

    this.http.get<Furnizor[]>(`${environment.apiUrl}/admin/vezi-furnizorii`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        console.log('2. Este listă (Array)?:', Array.isArray(dateDinBackend));

        this.furnizori.set(dateDinBackend);

        console.log('3. Variabila this.furnizori are acum:', this.furnizori().length, 'elemente.');

        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea furnizorilor:', eroare);
          this.alertaEroare.set('Nu s-au putut încărca furnizorii din baza de date.');
        }
      }
    });
  }

  editeazaFurnizor(id: number): void {
    this.router.navigate(['/admin/edit-furnizor', id]);
  }

  adaugaFurnizor(): void {
    this.router.navigate(['/admin/adauga-furnizor']);
  }

  veziPieseFurnizor(id: number): void {
    this.router.navigate(['/admin/vezi-piese-furnizor', id]);
  }


  stergeFurnizor(id: number): void {
    if (confirm('Ești sigur că vrei să ștergi acest furnizor? Toate datele și piesele lui asociate vor fi pierdute!')) {

      this.http.delete(`${environment.apiUrl}/admin/delete-furnizor/${id}`).subscribe({
        next: () => {
          this.alertaSucces.set('Furnizorul a fost șters cu succes!');
          this.incarcaFurnizori();

          setTimeout(() => {
            this.alertaSucces.set('');
          }, 3000);
        },
        error: (err) => {
          console.error('A apărut o eroare la ștergere:', err);
          this.alertaEroare.set('Nu s-a putut șterge furnizorul. Verifică consola.');
        }
      });
    }
  }
  inapoiLaDashboard(): void {
    this.router.navigate(['/admin']);
  }
}
