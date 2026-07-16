import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';

import { environment } from '../../../../environments/environment';


export interface Piese {
  piese_Id: number;
  furnizor_Id: number;
  pret_Cumparare: number;
  nume_Piesa: string;
  created_at: string;
}
export interface Furnizor {
  furnizor_Id: number;
  nume_Furnizor: string;
  email_Furnizor: string;
  numar_Telefon: string;
  nume_Admin_Furnizor: string;
  prenume_Admin_Furnizor: string;
}

@Component({
  selector: 'app-vezi-piese-furnizor',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './vezi-piese-furnizor.html',
  styleUrl: './vezi-piese-furnizor.scss',
})
export class VeziPieseFurnizor implements OnInit {

  piese = signal<Piese[]>([]);
  furnizor = signal<Furnizor | null>(null);
  furnizorId!: number;

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {
      this.furnizorId = +idParam;
      this.incarcaPiese(this.furnizorId); 
    } else {
      console.warn('ID-ul lipsește din URL! Redirecționăm...');
      this.veziFurnizorii();
      return;
    }

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

  incarcaPiese(furnizorId: number): void {
    this.seIncarca.set(true);

    this.http.get<{ furnizor: Furnizor; piese: Piese[] }>(`${environment.apiUrl}/admin/vezi-piese-furnizor/${furnizorId}`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        console.log('2. Este listă (Array)?:', Array.isArray(dateDinBackend));

        this.piese.set(dateDinBackend.piese || []);
        this.furnizor.set(dateDinBackend.furnizor || null);

        console.log('3. Variabila this.furnizori are acum:', this.piese().length, 'elemente.');

        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 400 && eroare.error && eroare.error.message) {
          if (eroare.error.message.includes("ID inexistent")) {
            alert(eroare.error.message);
            this.veziFurnizorii();
            return;
          }
        }
        this.router.navigate(['/dashboard']);
      }
    });
  }

  veziFurnizorii(): void {
    this.router.navigate(['/admin/vezi-furnizorii']);
  }

  seteazaPretPiesa(furnizorId: number, piesaId: number): void {
    this.router.navigate(['/admin/seteaza-pret-piesa-furnizor', furnizorId, piesaId]);
  }
}
