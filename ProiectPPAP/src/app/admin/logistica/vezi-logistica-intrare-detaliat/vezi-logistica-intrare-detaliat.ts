import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
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
  nume_Furnizor: string;
}

export interface StatisticiFactura {
  stadiu_logistica_factura: "Zero" | "Partial" | "Complet";
  linii_expediate: number;
  linii_total: number;
}

export interface ComandaPiese {
  comanda_piese_id: number;
  cantitate_comandata: number;
  piese_id: number;
}

export interface Piese {
  piese_Id: number;
  nume_Piesa: string;
  pret_Cumparare: number;
  pret_Vanzare: number;
}

export interface LinieFacturaDetaliata {
  comandaPiesa: ComandaPiese;
  piesa: Piese;
}

export interface FacturaIntrareDetaliata {
  factura: FacturiFurnizor;
  furnizor: Furnizor;
  statistici: StatisticiFactura;
  linii: LinieFacturaDetaliata[];
}

export interface RaspunsFacturaDetaliata {
  factura: FacturaIntrareDetaliata;
}

@Component({
  selector: 'app-vezi-logistica-intrare-detaliat',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-logistica-intrare-detaliat.html',
  styleUrl: './vezi-logistica-intrare-detaliat.scss',
})
export class VeziLogisticaIntrareDetaliatComponent implements OnInit {
  factura = signal<FacturaIntrareDetaliata | null>(null);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);
  idFactura!: number;

  ngOnInit(): void {
    this.idFactura = Number(this.route.snapshot.paramMap.get('facturaId'));
    this.incarcaFacturaDetaliata();

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

  incarcaFacturaDetaliata(): void {
    this.seIncarca.set(true);
    this.http.get<RaspunsFacturaDetaliata>(`${environment.apiUrl}/admin/vezi-logistica-intrare-detaliat/${this.idFactura}`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        this.factura.set(dateDinBackend.factura);
        console.log('2. Factura are', this.factura()?.linii.length, 'linii.');
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 404) {
          this.alertaEroare.set('Factura specificată nu există.');
        } else {
          console.error('Eroare la preluarea facturii detaliate:', eroare);
          this.alertaEroare.set('Nu s-a putut încărca factura din baza de date.');
        }
      }
    });
  }

  inapoiLaLista(): void {
    this.router.navigate(['/admin/vezi-logistica-intrare']);
  }
}
