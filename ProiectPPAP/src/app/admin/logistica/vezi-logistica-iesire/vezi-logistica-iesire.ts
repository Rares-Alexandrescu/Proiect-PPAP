import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

export interface Comanda {
  comanda_id: number;
  created_at: string;
  documente_id: number;
  stadiu_finalizare: boolean;
}

export interface StatisticiFactura {
  stadiu_logistica_factura: string;
  linii_expediate: number;
  linii_total: number;
  receptie_comanda: boolean;
}

export interface DocumenteComanda{
  documente_id: number;
  compania_id: number;
  stadiu_acceptare: boolean | null;
  path_documente_pdf: string | null;
  created_at: string;
}

export interface Companie {
  companie_Id: number;
  nume_Companie: string;
  email: string;
  numar_Telefon: string;
}

export interface FacturaCompanie{
  factura_id: number;
  comanda_id: number;
  companie_id: number;
  pret_brut: number;
  path_factura_pdf: string;
  stadiu_plata: boolean;
  created_at: string;
}

export interface ComandaIesireSumar {
  comanda: Comanda;
  companie: Companie;
  statisticiFactura: StatisticiFactura;
  documenteComanda: DocumenteComanda;
  facturaCompanie: FacturaCompanie;
}

export interface RaspunsListaComenziIesire {
  listaComenziIesire: ComandaIesireSumar[];
}

@Component({
  selector: 'app-vezi-logistica-iesire',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-logistica-iesire.html',
  styleUrl: './vezi-logistica-iesire.scss',
})
export class VeziLogisticaIesireComponent implements OnInit {
  comenzi = signal<ComandaIesireSumar[]>([]);
  private http = inject(HttpClient);
  private router = inject(Router);
  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaComenzi();

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

  incarcaComenzi(): void {
    this.seIncarca.set(true);
    this.http.get<RaspunsListaComenziIesire>(`${environment.apiUrl}/admin/vezi-logistica-iesire`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        this.comenzi.set(dateDinBackend.listaComenziIesire);
        console.log('2. Variabila this.comenzi are acum:', this.comenzi().length, 'elemente.');
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          console.error('Eroare la preluarea logisticii de ieșire:', eroare);
          this.alertaEroare.set('Nu s-a putut încărca lista de comenzi din baza de date.');
        }
      }
    });
  }

  veziFacturaIesireDetaliata(idComanda: number): void {
    this.router.navigate(['/admin/vezi-logistica-iesire-detaliat', idComanda]);
  }

  inapoiLaDashboard(): void {
    this.router.navigate(['/admin']);
  }
}
