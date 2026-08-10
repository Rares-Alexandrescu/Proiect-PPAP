import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HttpClient, HttpResponse } from '@angular/common/http';
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
export class VeziFacturiComponent implements OnInit {
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

  genereazaFacturi() {
    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');

    this.http.post<{ message: string }>(`${environment.apiUrl}/admin-furnizor/genereaza-facturi`, {})
      .subscribe({
        next: (response) => {
          this.alertaSucces.set(response.message);

          setTimeout(() => {
            this.alertaSucces.set('');
          }, 4000);

          this.incarcaFacturi();
        },
        error: (err) => {
          console.error(err);
          this.alertaEroare.set(err.error?.message || 'A apărut o eroare la generarea facturilor.');
          this.seIncarca.set(false);
        }
      });
  }



  //imi trebuie si ceva de incarcafacturi etc samd

  descarcaFactura(idFacturi: number): void {
    this.http.get(`${environment.apiUrl}/admin-furnizor/download-factura/${idFacturi}`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (raspuns) => {
        console.log(raspuns.headers.get('content-disposition'));
        this.salveazaFisierDinRaspuns(raspuns, `Documentatie_Muie_${idFacturi}.pdf`);
      },
      error: (eroare) => {
        if (eroare.status === 401) {
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 404) {
          this.alertaEroare.set('Documentul nu a fost gasit.');
        } else {
          console.error('Eroare la descarcarea documentatiei:', eroare);
          this.alertaEroare.set('Nu s-a putut descarca documentul.' + eroare.error.message);
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
