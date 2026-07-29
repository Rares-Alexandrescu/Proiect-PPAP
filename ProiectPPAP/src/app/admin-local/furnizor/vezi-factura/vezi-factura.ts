import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';


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

export interface FacturiFurnizorLinie {
  facturi_linie_id: number;
  facturi_id: number;
  comanda_piese_id: number;
  pret_brut: number;
  stadiu_logistica: boolean;
}

export interface Piesa {
  piese_Id: number;
  furnizor_Id: number;
  pret_Cumparare: number;
  nume_Piesa: string;
}


export interface LinieFacturaDetaliata {
  detaliiFactura: FacturiFurnizorLinie;
  piesa: Piesa;
  cantitate: number;
}

export interface VeziFacturaDetaliataResponse {
  factura: FacturaFurnizor;
  statistici: StatisticiFactura;
  linii: LinieFacturaDetaliata[];
}


@Component({
  selector: 'app-vezi-factura',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './vezi-factura.html',
  styleUrl: './vezi-factura.scss',
})
export class VeziFacturaComponent implements OnInit{

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  factura = signal<FacturaFurnizor | null>(null);
  statistici = signal<StatisticiFactura | null>(null);
  linii = signal<LinieFacturaDetaliata[]>([]);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    const idFacturaParam = this.route.snapshot.paramMap.get('idFactura');

    if (idFacturaParam) {
      const idFactura = Number(idFacturaParam);
      this.incarcaFacturaDetaliata(idFactura);
    } else {
      this.alertaEroare.set('ID-ul facturii lipsește din adresa URL. Veți fi redirecționat...');
      setTimeout(() => {
        this.router.navigate(['/admin-furnizor/vezi-facturi']);
      }, 3000);
    }
  }

  incarcaFacturaDetaliata(idFactura: number) {
    this.seIncarca.set(true);
    this.alertaEroare.set('');

    this.http.get<VeziFacturaDetaliataResponse>(`${environment.apiUrl}/admin-furnizor/vezi-factura/${idFactura}`)
      .subscribe({
        next: (response) => {
          this.factura.set(response.factura);
          this.statistici.set(response.statistici);
          this.linii.set(response.linii);

          this.seIncarca.set(false);
        },
        error: (err) => {
          console.error('Eroare la obținerea detaliilor:', err);
          this.alertaEroare.set(err.error?.message || 'A apărut o eroare la încărcarea facturii.');
          this.seIncarca.set(false);
        }
      });
  }

  inapoiLaFacturi() {
    this.router.navigate(['/admin-furnizor/vezi-facturi']);
  }

  trimiteToataFactura() {
    const id = this.factura()?.facturi_id;
    if (!id) return;

    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');

    this.http.post<{ message: string }>(`${environment.apiUrl}/admin-furnizor/trimite-comanda/${id}`, {})
      .subscribe({
        next: (res) => {
          this.alertaSucces.set(res.message);
          this.incarcaFacturaDetaliata(id);
        },
        error: (err) => {
          this.alertaEroare.set(err.error?.message || 'Eroare la trimiterea facturii.');
          this.seIncarca.set(false);
        }
      });
  }
  trimiteLinie(idFacturaLinie: number) {
    const idFactura = this.factura()?.facturi_id;
    if (!idFactura) return;

    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');

    this.http.post<{ message: string }>(`${environment.apiUrl}/admin-furnizor/trimite-linia-comanda/${idFactura}/${idFacturaLinie}`, {})
      .subscribe({
        next: (res) => {
          this.alertaSucces.set(res.message);
          this.incarcaFacturaDetaliata(idFactura);
        },
        error: (err) => {
          this.alertaEroare.set(err.error?.message || 'Eroare la trimiterea liniei.');
          this.seIncarca.set(false);
        }
      });
  }
  //si aici trebuie la psutrile alea de trimite linie si alte d alea, sa va aca au achitat aia factura sau nu,
  //nici nu mai stiu dac am erificat in api sau in db sau daca am verificat
  //uof
}
