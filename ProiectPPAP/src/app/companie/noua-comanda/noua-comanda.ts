import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
//tre sa fac front-end si sa bag in app-routes si poate mai vad ce e nevoie in companie-ta/etc
export interface FurnizorCuPieseActiveLocal {
  furnizor_Id: number;
  nume_Furnizor: string;
  email: string;
  numar_Telefon: string;
  numarPieseActive: number;
}

export interface NouaComandaResponse {
  furnizori: FurnizorCuPieseActiveLocal[];
}

export interface PiesaActivaLocal {
  piese_Id: number;
  furnizor_Id: number;
  nume_Piesa: string;
  pret_Cumparare: number;
}

@Component({
  selector: 'app-noua-comanda',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './noua-comanda.html',
  styleUrl: './noua-comanda.scss',
})
export class NouaComanda implements OnInit {
  furnizori = signal<FurnizorCuPieseActiveLocal[]>([]);
  furnizorSelectat = signal<FurnizorCuPieseActiveLocal | null>(null);
  piese = signal<PiesaActivaLocal[]>([]);

  private http = inject(HttpClient);
  private router = inject(Router);

  alertaEroare = signal<string>('');
  seIncarcaFurnizori = signal<boolean>(false);
  seIncarcaPiese = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaFurnizorii();
  }

  incarcaFurnizorii(): void {
    this.seIncarcaFurnizori.set(true);
    this.http.get<NouaComandaResponse>(`${environment.apiUrl}/compania-ta/noua-comanda`).subscribe({
      next: (dateDinBackend) => {
        this.furnizori.set((dateDinBackend as any).Furnizori || dateDinBackend.furnizori || []);
        this.seIncarcaFurnizori.set(false);
      },
      error: (eroare) => {
        this.seIncarcaFurnizori.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirata. Te redirectionam...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 400) {
          this.alertaEroare.set(eroare.error?.message || 'Nu ai o companie asociata.');
        } else {
          console.error('Eroare la preluarea furnizorilor:', eroare);
          this.alertaEroare.set('Nu s-au putut incarca furnizorii.');
        }
      }
    });
  }

  selecteazaFurnizor(furnizor: FurnizorCuPieseActiveLocal): void {
    this.alertaEroare.set('');

    if (this.furnizorSelectat()?.furnizor_Id === furnizor.furnizor_Id) {
      this.furnizorSelectat.set(null);
      this.piese.set([]);
      return;
    }

    this.furnizorSelectat.set(furnizor);
    this.incarcaPiese(furnizor.furnizor_Id);
  }

  incarcaPiese(idFurnizor: number): void {
    this.seIncarcaPiese.set(true);
    this.piese.set([]);
    this.http.get<PiesaActivaLocal[]>(`${environment.apiUrl}/compania-ta/noua-comanda/${idFurnizor}`).subscribe({
      next: (dateDinBackend) => {
        this.piese.set(dateDinBackend || []);
        this.seIncarcaPiese.set(false);
      },
      error: (eroare) => {
        this.seIncarcaPiese.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirata. Te redirectionam...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea pieselor:', eroare);
          this.alertaEroare.set('Nu s-au putut incarca piesele acestui furnizor.');
        }
      }
    });
  }

  adaugaPiesaInComanda(idPiesa: number): void {
    const idFurnizor = this.furnizorSelectat()?.furnizor_Id;
    if (!idFurnizor) return;
    this.router.navigate(['/compania-ta/adauga-piesa', idFurnizor, idPiesa]);
  }

  inapoiLaComenzi(): void {
    this.router.navigate(['/compania-ta/comenzi-curente']);
  }
}
