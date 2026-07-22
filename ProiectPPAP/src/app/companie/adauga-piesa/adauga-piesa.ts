import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PiesaDetaliiResponse {
  piese_Id: number;
  furnizor_Id: number;
  nume_Piesa: string;
  pret_Cumparare: number;
}

@Component({
  selector: 'app-adauga-piesa',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './adauga-piesa.html',
  styleUrl: './adauga-piesa.scss',
})
export class AdaugaPiesa implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  idFurnizor: number = 0;
  idPiesa: number = 0;

  formAdaugaPiesa!: FormGroup;

  piesaInfo = signal<PiesaDetaliiResponse | null>(null);
  seIncarca = signal<boolean>(false);
  seTrimite = signal<boolean>(false);
  alertaEroare = signal<string>('');
  mesajSucces = signal<string>('');

  eroriBackend = signal<any>({});

  private subscriptions: Subscription = new Subscription();

  ngOnInit(): void {
    this.formAdaugaPiesa = this.fb.group({
      cantitate: [1],
      detaliiPiese: [''],
      comandaId: [null]
    });

    this.route.paramMap.subscribe(params => {
      this.idFurnizor = Number(params.get('idFurnizor'));
      this.idPiesa = Number(params.get('idPiesa'));

      if (this.idFurnizor && this.idPiesa) {
        this.verificaSiIncarcaPiesa();
      } else {
        this.alertaEroare.set('Parametri invalizi in ruta.');
      }
    });
    this.curataErorileLaTastare();
  }

  verificaSiIncarcaPiesa(): void {
    this.seIncarca.set(true);
    this.alertaEroare.set('');

    const url = `${environment.apiUrl}/compania-ta/adauga-piesa/${this.idFurnizor}/${this.idPiesa}`;

    this.http.get<PiesaDetaliiResponse>(url).subscribe({
      next: (data) => {
        this.piesaInfo.set(data);
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set(eroare.error?.message || 'Eroare la validarea piesei.');
        }
      }
    });
  }
  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'cantitate': ['cantitate'],
      'detaliiPiese': ['detalii_piese', 'detalipiese'],
      'comandaId': ['comanda_id', 'comandaId']
    };

    Object.keys(this.formAdaugaPiesa.controls).forEach(numeInput => {
      const control = this.formAdaugaPiesa.get(numeInput);

      if (control) {
        const sub = control.valueChanges.subscribe(() => {
          this.mesajSucces.set('');
          const cheiBackend = mapareCampuri[numeInput];

          if (cheiBackend) {
            const eroriCurente = { ...this.eroriBackend() };
            let modificatCeva = false;

            cheiBackend.forEach(cheie => {
              if (eroriCurente[cheie]) {
                delete eroriCurente[cheie];
                modificatCeva = true;
              }
            });

            if (modificatCeva) {
              this.eroriBackend.set(eroriCurente);
            }
          }
        });

        this.subscriptions.add(sub);
      }
    });
  }

  trimiteAdaugarePiesa(): void {
    this.seTrimite.set(true);
    this.alertaEroare.set('');
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.formAdaugaPiesa.value;
    const payload = {
      comanda_Id: formData.comandaId ? Number(formData.comandaId) : null,
      cantitate: Number(formData.cantitate),
      detaliiPiese: formData.detaliiPiese?.trim() ? formData.detaliiPiese : null
    };

    const url = `${environment.apiUrl}/compania-ta/adauga-piesa/${this.idFurnizor}/${this.idPiesa}`;

    this.http.post<{ message: string; errors?: any }>(url, payload).subscribe({
      next: (raspuns) => {
        this.seTrimite.set(false);
        this.mesajSucces.set(raspuns.message || 'Piesa a fost adaugata cu succes!');

        const targetComandaId = payload.comanda_Id;
        setTimeout(() => {
          if (targetComandaId) {
            this.router.navigate(['/compania-ta/vezi-comanda', targetComandaId]);
          } else {
            this.router.navigate(['/compania-ta/comenzi-curente']);
          }
        }, 1500);
      },
      error: (eroare) => {
        this.seTrimite.set(false);
        const eroriPrimite = eroare.error?.errors || eroare.error?.eroriCampuri;
        if (eroriPrimite) {
          this.eroriBackend.set(eroriPrimite);
        }

        this.alertaEroare.set(eroare.error?.message || 'Nu am putut adauga piesa in comanda.');
      }
    });
  }

  inapoiLaComenzi(): void {
    this.router.navigate(['/compania-ta/noua-comanda']);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
