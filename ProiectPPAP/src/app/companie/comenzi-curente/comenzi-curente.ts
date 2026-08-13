import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpResponse } from '@angular/common/http';
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

  veziComanda(idComanda: number): void {
    this.router.navigate(['/compania-ta/vezi-comanda', idComanda]);
  }

  stergeComanda(idComanda: number): void {
    if (!confirm(`Sigur dorești să ștergi definitiv comanda #${idComanda}? Această acțiune nu poate fi anulată.`)) {
      return;
    }

    this.alertaEroare.set('');
    this.alertaSucces.set('');

    this.http.delete<{ message: string }>(`${environment.apiUrl}/compania-ta/sterge-comanda/${idComanda}`).subscribe({
      next: (raspuns) => {
        console.log(raspuns.message);
        this.alertaSucces.set(raspuns.message);
        this.incarcaComenzile();

        setTimeout(() => {
          this.alertaSucces.set('');
        }, 4000);

      },
      error: (eroare) => {
        if (eroare.status === 401 || eroare.status === 403) {
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set(eroare.error?.message || 'Nu s-a putut șterge comanda. Încearcă din nou.');
        }
      }
    });
  }

  comandaNoua(): void {
    this.router.navigate(['/compania-ta/noua-comanda']);
  }

  receptioneazaComanda(idComanda: number): void {
    if (!confirm('Ești sigur că vrei să marchezi această comandă ca fiind recepționată?')) {
      return;
    }

    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');

    const url = `${environment.apiUrl}/compania-ta/receptioneaza-comanda/${idComanda}`;

    this.http.put<{ message: string }>(url, {}).subscribe({
      next: (raspuns) => {
        this.seIncarca.set(false);
        this.alertaSucces.set(raspuns.message || 'Comanda a fost recepționată cu succes!');
        this.incarcaComenzile();
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set(eroare.error?.message || 'Nu s-a putut recepționa comanda.');
        }
      }
    });
  }

  platesteFactura(idFactura: number): void {
    if (!confirm('Ești sigur că vrei să platesti această comandă?')) {
      return;
    }

    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');

    this.http.put<{ message: string }>(
      `${environment.apiUrl}/compania-ta/plateste-factura/${idFactura}`,
      null
    ).subscribe({
      next: (res) => {
        this.seIncarca.set(false);
        this.alertaSucces.set(res.message);
        this.incarcaComenzile();
        setTimeout(() => this.alertaSucces.set(''), 2000);
      },
      error: (err) => {
        this.seIncarca.set(false);
        this.alertaEroare.set(err.error?.message ?? 'Eroare la plata facturii!');
        setTimeout(() => this.alertaEroare.set(''), 2000);
      }
    });

  }

  descarcaFactura(idFactura: number): void {
    this.http.get(`${environment.apiUrl}/compania-ta/download-factura/${idFactura}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (raspuns) => {
        this.salveazaFisierDinRaspuns(raspuns, `Factura_${idFactura}.pdf`);
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          this.router.navigate(['/dashboard']);
          {
            console.error('Eroare la descarcarea facturii:', eroare);
            this.alertaEroare.set('Nu s-a putut descarca factura.' + eroare.error.message);
          }
        }
      }
    });
  }

  descarcaDocumentatia(idDocumenteComanda: number): void {
    this.http.get(`${environment.apiUrl}/compania-ta/download-documentatie/${idDocumenteComanda}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (raspuns) => {
        this.salveazaFisierDinRaspuns(raspuns, `DocumenteComanda_${idDocumenteComanda}.pdf`);
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          this.router.navigate(['/dashboard']);
          {
            console.error('Eroare la descarcarea documentului:', eroare);
            this.alertaEroare.set('Nu s-a putut descarca factura.' + eroare.error.message);
          }
        }
      }
    });
  }

  private salveazaFisierDinRaspuns(raspuns: HttpResponse<Blob>, numeImplicit: string): void {
    const blob = raspuns.body;
    if (!blob) return;

    const numeFisier = this.extrageNumeDinHeader(raspuns.headers.get('content-disposition')) ?? numeImplicit;

    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = numeFisier;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }

  private extrageNumeDinHeader(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;

    const matchUtf8 = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (matchUtf8) {
      return decodeURIComponent(matchUtf8[1]);
    }

    const matchSimplu = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (matchSimplu) {
      return matchSimplu[1];
    }

    return null;
  }


}
