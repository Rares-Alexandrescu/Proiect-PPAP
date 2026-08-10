import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

export interface Comanda {
  comanda_id: number;
  created_at: string;
  documente_id: number;
  stadiu_finalizare: boolean;
}

export interface Companie {
  companie_Id: number;
  nume_Companie: string;
  email: string;
  numar_Telefon: string;
}

export interface DocumenteComandaId {
  factura_id: number;
  documente_id: number;
}
export interface ComandaPiese {
  comanda_piese_id: number;
  cantitate_comandata: number;
  piese_id: number;
  stadiu_intern: number;
}

export interface Piese {
  piese_Id: number;
  nume_Piesa: string;
  pret_Cumparare: number;
  pret_Vanzare: number;
}

export interface LinieComandaDetaliata {
  comandaPiesa: ComandaPiese;
  piesa: Piese;
}

export interface ComandaIesireDetaliata {
  comanda: Comanda;
  companie: Companie;
  documenteComandaId: DocumenteComandaId;
  linii: LinieComandaDetaliata[];
}

export interface RaspunsComandaDetaliata {
  comanda: ComandaIesireDetaliata;
}

@Component({
  selector: 'app-vezi-logistica-iesire-detaliat',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-logistica-iesire-detaliat.html',
  styleUrl: './vezi-logistica-iesire-detaliat.scss',
})
export class VeziLogisticaIesireDetaliatComponent implements OnInit {
  comanda = signal<ComandaIesireDetaliata | null>(null);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);
  idComanda!: number;

  ngOnInit(): void {
    this.idComanda = Number(this.route.snapshot.paramMap.get('comandaId'));
    this.incarcaComandaDetaliata();

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

  incarcaComandaDetaliata(): void {
    this.seIncarca.set(true);
    this.http.get<RaspunsComandaDetaliata>(`${environment.apiUrl}/admin/vezi-logistica-iesire-detaliat/${this.idComanda}`).subscribe({
      next: (dateDinBackend) => {
        console.log('1. Date primite de la server:', dateDinBackend);
        this.comanda.set(dateDinBackend.comanda);
        console.log('2. Comanda are', this.comanda()?.linii.length, 'linii.');
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401) {
          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 404 || eroare.status === 400) {
          this.alertaEroare.set('Comanda specificată nu există.');
        } else {
          console.error('Eroare la preluarea comenzii detaliate:', eroare);
          this.alertaEroare.set('Nu s-a putut încărca comanda din baza de date.');
        }
      }
    });
  }

  inapoiLaLista(): void {
    this.router.navigate(['/admin/vezi-logistica-iesire']);
  }

  proceseazaComanda(idComanda: number | undefined, idComandaPiese: number | undefined): void {
    if (!idComanda || !idComandaPiese) {
      this.alertaEroare.set("Nu am Comanda ID sau ID-ul linie specifice pe care vrei sa o trimiti");
      return;
    }
    this.http.put<{ message: string }>(
      `${environment.apiUrl}/admin/proceseaza-comanda/${idComanda}/${idComandaPiese}`,
      null
    ).subscribe({
      next: (res) => {
        this.alertaSucces.set(res.message);
        this.incarcaComandaDetaliata();
      },
      error: (err) => {
        this.alertaEroare.set(err.error?.message ?? 'Eroare la procesarea comenzii.');
      }
    });
  }

  trimiteComanda(idComanda: number | undefined, idComandaPiese: number | undefined): void {
    if (!idComanda || !idComandaPiese) {
      this.alertaEroare.set("Nu am Comanda ID sau ID-ul linie specifice pe care vrei sa o trimiti");
      return;
    }

    this.http.put<{ message: string }>(
      `${environment.apiUrl}/admin/trimite-comanda/${idComanda}/${idComandaPiese}`,
      null
    ).subscribe({
      next: (res) => {
        this.alertaSucces.set(res.message);
        this.incarcaComandaDetaliata();
      },
      error: (err) => {
        this.alertaEroare.set(err.error?.message ?? 'Eroare la procesarea comenzii.');
      }
    });
  }


  descarcaDocumentatia(idCompanie: number, idDocumenteComanda: number): void {
    this.http.get(`${environment.apiUrl}/admin/download-documentatie-companie/${idCompanie}/${idDocumenteComanda}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (raspuns) => {
        console.log(raspuns.headers.get('content-disposition'));
        this.salveazaFisierDinRaspuns(raspuns, `DocumentatieCompanieAdmin_${idCompanie}_${idDocumenteComanda}.pdf`);
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 404) {
          this.alertaEroare.set('Documentul nu a fost gasit.');
        } else {
          console.error('Eroare la descarcarea documentatiei:', eroare);
          this.alertaEroare.set('Nu s-a putut descarca documentul.');
        }
      }
    });
  }

  descarcaFactura(idCompanie: number, idFactura: number): void {
    this.http.get(`${environment.apiUrl}/admin/download-factura-companie/${idCompanie}/${idFactura}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (raspuns) => {
        console.log(raspuns.headers.get('content-disposition'));
        this.salveazaFisierDinRaspuns(raspuns, `FacturaCompanie_Muie_${idFactura}_${idCompanie}.pdf`);
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 404) {
          this.alertaEroare.set('Documentul nu a fost gasit.');
        } else {
          console.error('Eroare la descarcarea facturii:', eroare);
          this.alertaEroare.set('Nu s-a putut descarca factura.');
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
