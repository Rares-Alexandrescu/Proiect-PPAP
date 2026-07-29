import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';


import { environment } from '../../../../environments/environment';


export interface FacturaFurnizor {
  facturi_id: number;
  furnizor_id: number;
  pret_total_brut: number;
  stadiu_plata: boolean;     
  path_factura_pdf?: string;   
  created_at: Date | string;
}

export interface StatisticiFactura {
  stadiu_logistica_factura: string;
  linii_expediate: number;
  linii_total: number;
}


export interface Furnizor {
  furnizor_Id: number;
  nume_Furnizor: string;
  email_Furnizor: string;
  numar_Telefon: string;
  nume_Admin_Furnizor: string;
  prenume_Admin_Furnizor: string;
}
export interface FacturaMasterItem {
  factura: FacturaFurnizor;
  statisticiFactura: StatisticiFactura;
}
export interface VeziFacturiResponse {
  listaFacturi: FacturaMasterItem[];
  furnizor: Furnizor;
}

@Component({
  selector: 'app-vezi-facturi',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './vezi-facturi.html',
  styleUrl: './vezi-facturi.scss',
})
export class VeziFacturi implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  facturi = signal<FacturaMasterItem[]>([]);
  furnizor = signal<Furnizor | null>(null);
  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaFacturi();

    const stareNavigare = history.state;

    if(stareNavigare && stareNavigare.mesajSucces) {
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

  incarcaFacturi() {
    this.seIncarca.set(true);
    this.alertaEroare.set('');

    this.http.get<VeziFacturiResponse>(`${environment.apiUrl}/admin-furnizor/vezi-facturi`)
      .subscribe({
        next: (response) => {
          this.facturi.set(response.listaFacturi);
          this.furnizor.set(response.furnizor);
          this.seIncarca.set(false);
        },
        error: (err) => {
          console.error(err);
          this.alertaEroare.set(err.error?.message || 'A apărut o eroare la aducerea facturilor.');
          this.seIncarca.set(false);
        }
      });
  }

  veziDetaliiFactura(idFactura: number) {
    this.router.navigate(['/admin-furnizor/vezi-factura', idFactura]);
  }

  //imi trebuie si ceva de incarcafacturi etc samd
}
