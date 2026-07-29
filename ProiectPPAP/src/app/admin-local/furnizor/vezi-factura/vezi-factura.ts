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
export class VeziFactura implements OnInit{

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  factura = signal<FacturaFurnizor | null>(null);
  statistici = signal<StatisticiFactura | null>(null);
  linii = signal<LinieFacturaDetaliata[]>([]);

  alertaEroare = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    const idFacturaParam = this.route.snapshot.paramMap.get('idFactura');

    if (idFacturaParam) {
      const idFactura = Number(idFacturaParam);
      this.incarcaFacturaDetaliata(idFactura);
    } else {
      this.alertaEroare.set('ID-ul facturii lipsește din adresa URL.');
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
}
