import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export interface ComandaLocal {
  comanda_id: number;
  documente_id: number | null;
  stadiu_finalizare: boolean;
  created_at: string;
}

export interface PiesaLocal {
  piese_Id: number;
  furnizor_Id: number;
  nume_Piesa: string;
  pret_Vanzare: number;
}

export interface FurnizorPiesaLocal {
  furnizor_Id: number;
  nume_Furnizor: string;
  numar_Telefon: string;
  email_Furnizor?: string;
}

export interface DetaliiComandaPiesaLocal {
  comanda_id: number;
  piese_id: number;
  comanda_piese_id: number;
  cantitate_comandata: number;
  detalii_piese: string | null;
}

export interface PiesaComandataLocal {
  piesa: PiesaLocal;
  furnizorPiesa: FurnizorPiesaLocal;
  detaliiComandaPiesa: DetaliiComandaPiesaLocal;
  pretTotalRand: number;
}

export interface VeziComandaResponse {
  rolUtilizator: string;
  comanda: ComandaLocal;
  totalGeneral: number;
  pieseComandate: PiesaComandataLocal[];
}



@Component({
  selector: 'app-vezi-comanda',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './vezi-comanda.html',
  styleUrl: './vezi-comanda.scss',
})




export class VeziComandaComponent implements OnInit{
  comanda = signal<ComandaLocal | null>(null);
  totalGeneral = signal<number>(0);
  pieseComandate = signal<PiesaComandataLocal[]>([]);
  rolUtilizator = signal<string | null>(null);

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  idComanda!: number;

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);
  seTrimite = signal<boolean>(false);

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('idComanda');
    if (!idParam) {
      this.alertaEroare.set('Comanda nu a fost specificata!');
      return;
    }
    this.idComanda = Number(idParam);
    this.incarcaComanda();
  }

  incarcaComanda(): void {
    this.seIncarca.set(true);
    this.http.get<VeziComandaResponse>(`${environment.apiUrl}/compania-ta/vezi-comanda/${this.idComanda}`).subscribe({
      next: (dateDinBackend) => {
        this.comanda.set((dateDinBackend as any).Comanda || dateDinBackend.comanda);
        this.totalGeneral.set((dateDinBackend as any).TotalGeneral ?? dateDinBackend.totalGeneral ?? 0);
        this.pieseComandate.set((dateDinBackend as any).PieseComandate || dateDinBackend.pieseComandate || []);
        this.rolUtilizator.set((dateDinBackend as any).RolUtilizator || dateDinBackend.rolUtilizator || null);  
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirata. Te redirectionam...');
          this.router.navigate(['/dashboard']);
        } else if (eroare.status === 400) {
          this.router.navigate(['/compania-ta/comenzi-curente'], {
            state: { mesajEroare: eroare.error?.message || 'Comanda nu a fost gasita.' }
          });
        } else {
          console.error('Eroare la preluarea comenzii:', eroare);
          this.alertaEroare.set('Nu s-au putut incarca datele comenzii.');
        }
      }
    });
  }

  plaseazaComanda(): void {
    if (!confirm('Ești sigur că vrei să plasezi această comandă? Nu vei mai putea adăuga piese după aceea.')) {
      return;
    }

    this.seTrimite.set(true);
    this.alertaEroare.set('');

    this.http.post<{ message: string; calePdf?: string }>(
      `${environment.apiUrl}/compania-ta/plaseaza-comanda/${this.idComanda}`,
      {}
    ).subscribe({
      next: (raspuns) => {
        this.seTrimite.set(false);
        this.router.navigate(['/compania-ta/comenzi-curente'], {
          state: { mesajSucces: raspuns.message || 'Comanda a fost plasata cu succes!' }
        });
      },
      error: (eroare) => {
        this.seTrimite.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          console.warn('Acces neautorizat sau sesiune expirata. Te redirectionam...');
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set(eroare.error?.message || 'Nu am putut plasa comanda.');
        }
      }
    });
  }

  inapoiLaComenzi(): void {
    this.router.navigate(['/compania-ta/comenzi-curente']);
  }

  stergeDinComanda(idComandaPiese: number): void {
    if (!confirm('Sigur dorești să ștergi această piesă din comandă?')) {
      return;
    }

    this.alertaEroare.set('');
    this.alertaSucces.set('');

    const url = `${environment.apiUrl}/compania-ta/sterge-din-comanda/${this.idComanda}/${idComandaPiese}`;

    this.http.delete<{ message: string }>(url).subscribe({
      next: (raspuns) => {
        this.alertaSucces.set(raspuns.message);
        if (raspuns.message.includes('s-a sters toata comanda')) {
          setTimeout(() => {
            this.router.navigate(['/compania-ta/comenzi-curente']);
          }, 1500);
        } else {
          this.incarcaComanda();
        }
      },
      error: (eroare) => {
        this.alertaEroare.set(eroare.error?.message || 'Nu s-a putut șterge linia din comandă.');
      }
    });
  }

  editComandaPiesa(idComandaPiesa: number): void {
    this.router.navigate(['/compania-ta/modifica-comanda', this.idComanda, idComandaPiesa]);
  }

  //receptioneazaComanda(): void {
  //    if (!confirm('Ești sigur că vrei să marchezi această comandă ca fiind recepționată?')) {
  //      return;
  //    }
 
  //    this.seTrimite.set(true);
  //    this.alertaEroare.set('');
  //    this.alertaSucces.set('');

  //    const url = `${environment.apiUrl}/compania-ta/receptioneaza-comanda/${this.idComanda}`;

  //    this.http.put<{ message: string }>(url, {}).subscribe({
  //      next: (raspuns) => {
  //        this.seTrimite.set(false);
  //        this.alertaSucces.set(raspuns.message || 'Comanda a fost recepționată cu succes!');
  //        this.incarcaComanda();
  //      },
  //      error: (eroare) => {
  //        this.seTrimite.set(false);
  //        if (eroare.status === 401 || eroare.status === 403) {
  //          console.warn('Acces neautorizat sau sesiune expirată. Te redirecționăm...');
  //          this.router.navigate(['/dashboard']);
  //        } else {
  //          this.alertaEroare.set(eroare.error?.message || 'Nu s-a putut recepționa comanda.');
  //        }
  //      }
  //    });
  //  }

}
