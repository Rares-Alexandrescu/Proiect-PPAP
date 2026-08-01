import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { environment } from '../../../../environments/environment';


export interface FacturiFurnizor {
  facturi_id: number;
  furnizor_id: number;
  created_at: string; 
  pret_total_brut: number;
  path_factura_pdf: string | null;
}


export interface Furnizor {
  furnizor_id: number;
  nume_furnizor: string;
}

export interface StatisticiFactura {
  stadiu_logistica_factura: "Zero" | "Partial" | "Complet";
  linii_expediate: number;
  linii_total: number;
}

export interface FacturaIntrareSumar {
  factura: FacturiFurnizor;
  furnizor: Furnizor;
  statistici: StatisticiFactura;
}

export interface RaspunsListaFacturiIntrare {
  listaFacturiIntrare: FacturaIntrareSumar[];
}

@Component({
  selector: 'app-vezi-logistica-intrare',
  imports: [CommonModule],
  templateUrl: './vezi-logistica-intrare.html',
  styleUrl: './vezi-logistica-intrare.scss',
})
export class VeziLogisticaIntrareComponent {
  facturi = signal<FacturaIntrareSumar[]>([]);
  private http = inject(HttpClient);
  private router = inject(Router);
  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaFacturi();

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

  incarcaFacturi(): void {
    this.seIncarca.set(true);
    this.http.get<RaspunsListaFacturiIntrare>(`${environment.apiUrl}/admin/vezi-logistica-intrare`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        console.log('2. Este listă (Array)?:', Array.isArray(dateDinBackend.listaFacturiIntrare));
        this.facturi.set(dateDinBackend.listaFacturiIntrare);
        console.log('3. Variabila this.facturi are acum:', this.facturi().length, 'elemente.');
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea logisticii de intrare:', eroare);
          this.alertaEroare.set('Nu s-a putut încărca lista de facturi din baza de date.');
        }
      }
    });
  }

  veziFacturaIntrareDetaliata(idFactura: number): void {
    this.router.navigate(['/admin/vezi-logistica-intrare-detaliat', idFactura]);
  }
  inapoiLaDashboard(): void {
    this.router.navigate(['/admin/vezi-logistica-intrare']);
  }

  //pare ok asta, mai imi trebuie poate niste metode facute etc
}
