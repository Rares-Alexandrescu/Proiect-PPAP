import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

export interface Comanda {
  comanda_id: number;
  created_at: string;
  documente_id: number;
  stadiu_finalizare: number;
}

export interface Companie {
  companie_id: number;
  nume_companie: string;
  email: string;
  numar_telefon: string;
}

export interface ComandaPiese {
  comanda_piese_id: number;
  cantitate_comandata: number;
  piese_id: number;
}

export interface Piese {
  piese_id: number;
  nume_piesa: string;
  pret_cumparare: number;
  pret_vanzare: number;
}

export interface LinieComandaDetaliata {
  comandaPiesa: ComandaPiese;
  piesa: Piese;
}

export interface ComandaIesireDetaliata {
  comanda: Comanda;
  companie: Companie;
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
}
