import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface ComandaLocal {
  comanda_id: number;
  documente_id: number | null;
  stadiu_finalizare: boolean;
  created_at: string;
}

export interface DocumentComandaLocal {
  documente_id: number;
  stadiu_acceptare: boolean | null;
  path_documente_pdf: string | null;
  created_at: string;
}

export interface FacturaLocal {
  factura_id: number;
  comanda_id: number;
  companie_id: number;
  pret_brut: number;
  path_factura_pdf: string | null;
  stadiu_plata: boolean | null;
  created_at: string;
}

export interface ComandaCompletaLocal {
  comanda: ComandaLocal;
  documentComanda: DocumentComandaLocal | null;
  factura: FacturaLocal | null;
}

//desi merge sa nu l bag, vedem...
export interface UtilizatorLocal {
  id: number;
  email: string;
  nume: string;
  prenume: string;
}
export interface CompanieLocal {
  companie_Id: number;
  email: string;
  cnpAdminLocal: string;
  numeAdminLocal: string;
  prenumeAdminLocal: string;
  nume_Companie: string;
  numar_Telefon: string;
}
export interface ComenziCurenteResponse {
  utilizator: UtilizatorLocal;
  rol: string;
  companie: CompanieLocal;
  comenzi: ComandaCompletaLocal[];
}

@Component({
  selector: 'app-comenzi-curente',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './comenzi-curente.html',
  styleUrl: './comenzi-curente.scss',
})
export class ComenziCurenteComponent implements OnInit {
  utilizator = signal<UtilizatorLocal | null>(null);
  rol = signal<string>('');
  companie = signal<CompanieLocal | null>(null);
  comenzi = signal<ComandaCompletaLocal[]>([]);

  private http = inject(HttpClient);
  private router = inject(Router);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  ngOnInit(): void {
    this.incarcaComenzile();

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

  incarcaComenzile(): void {
    this.seIncarca.set(true);
    this.http.get<ComenziCurenteResponse>(`${environment.apiUrl}/compania-ta/comenzi-curente`).subscribe({
      next: (dateDinBackend) => {
        this.utilizator.set((dateDinBackend as any).Utilizator || dateDinBackend.utilizator);
        this.rol.set((dateDinBackend as any).Rol || dateDinBackend.rol || '');
        this.companie.set((dateDinBackend as any).Companie || dateDinBackend.companie);
        this.comenzi.set((dateDinBackend as any).Comenzi || dateDinBackend.comenzi || []);
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirata. Te redirectionam...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 400) {
          this.alertaEroare.set(eroare.error?.message || 'Nu ai o companie asociata.');
        } else {
          console.error('Eroare la preluarea comenzilor:', eroare);
          this.alertaEroare.set('Nu s-au putut incarca comenzile.');
        }
      }
    });
  }

  vezComanda(idComanda: number): void {
    this.router.navigate(['/compania-ta/vezi-comanda', idComanda]);
  }

  comandaNoua(): void {
    this.router.navigate(['/compania-ta/noua-comanda']);
  }
}
