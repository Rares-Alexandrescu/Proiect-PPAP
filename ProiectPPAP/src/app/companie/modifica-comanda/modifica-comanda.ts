import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface PiesaInfoLocal {
  piese_Id: number;
  furnizor_Id: number;
  nume_Piesa: string;
  pret_Cumparare: number;
}

export interface FurnizorInfoLocal {
  furnizor_Id: number;
  nume_Furnizor: string;
  email: string;
  numar_Telefon: string;
}

export interface DetaliiComandaPiesaLocal {
  comanda_id: number;
  piese_id: number;
  cantitate_comandata: number;
  detalii_piese: string | null;
}

export interface ModificaComandaResponse {
  piesa: PiesaInfoLocal;
  furnizor: FurnizorInfoLocal;
  detaliiComandaPiesa: DetaliiComandaPiesaLocal;
}

@Component({
  selector: 'app-modifica-comanda',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './modifica-comanda.html',
  styleUrl: './modifica-comanda.scss',
})
export class ModificaComanda implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  idComanda: number = 0;
  idComandaPiesa: number = 0;

  editComandaForm!: FormGroup;

  piesaInfo = signal<PiesaInfoLocal | null>(null);
  furnizorInfo = signal<FurnizorInfoLocal | null>(null);

  seIncarca = signal<boolean>(false);
  mesajSucces = signal<string>('');
  eroriBackend = signal<any>({});

  ngOnInit(): void {
    this.editComandaForm = this.fb.group({
      cantitate: [1, [Validators.required, Validators.min(1)]],
      detaliiPiese: ['', [Validators.maxLength(255)]],
    });

    this.idComanda = Number(this.route.snapshot.paramMap.get('idComanda'));
    this.idComandaPiesa = Number(this.route.snapshot.paramMap.get('idComandaPiesa'));

    if (!this.idComanda || !this.idComandaPiesa) {
      this.eroriBackend.set({ eroareGenerala: ['Parametri invalizi in ruta.'] });
      return;
    }

    this.incarcaLinie();
    this.curataErorileLaTastare();
  }

  incarcaLinie(): void {
    this.seIncarca.set(true);
    const url = `${environment.apiUrl}/compania-ta/modifica-comanda/${this.idComanda}/${this.idComandaPiesa}`;

    this.http.get<ModificaComandaResponse>(url).subscribe({
      next: (date) => {
        this.piesaInfo.set(date.piesa);
        this.furnizorInfo.set(date.furnizor);
        this.editComandaForm.patchValue({
          cantitate: date.detaliiComandaPiesa.cantitate_comandata,
          detaliiPiese: date.detaliiComandaPiesa.detalii_piese ?? ''
        });
        this.seIncarca.set(false);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        if (eroare.status === 401 || eroare.status === 403) {
          this.router.navigate(['/dashboard']);
        } else {
          this.eroriBackend.set({ eroareGenerala: [eroare.error?.message || 'Nu am putut incarca linia comenzii.'] });
        }
      }
    });
  }

  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'cantitate': ['cantitate'],
      'detaliiPiese': ['detalii_piese', 'detalipiese'],
    };

    Object.keys(this.editComandaForm.controls).forEach(numeInput => {
      const control = this.editComandaForm.get(numeInput);
      if (control) {
        control.valueChanges.subscribe(() => {
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
            if (modificatCeva) this.eroriBackend.set(eroriCurente);
          }
        });
      }
    });
  }

  onSubmit(): void {
    if (this.editComandaForm.invalid) {
      this.editComandaForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.editComandaForm.value;
    const payload = {
      cantitate: Number(formData.cantitate),
      detaliiPiese: formData.detaliiPiese?.trim() ? formData.detaliiPiese.trim() : null
    };

    const url = `${environment.apiUrl}/compania-ta/modifica-comanda/${this.idComanda}/${this.idComandaPiesa}`;

    this.http.put<{ message: string }>(url, payload).subscribe({
      next: (raspuns) => {
        this.seIncarca.set(false);
        this.mesajSucces.set(raspuns.message || 'Linia comenzii a fost actualizata cu succes!');
        setTimeout(() => {
          this.router.navigate(['/compania-ta/vezi-comanda', this.idComanda]);
        }, 1500);
      },
      error: (eroare) => {
        this.seIncarca.set(false);
        const eroriPrimite = eroare.error?.eroriCampuri;
        if (eroriPrimite) {
          this.eroriBackend.set(eroriPrimite);
        } else {
          this.eroriBackend.set({ eroareGenerala: [eroare.error?.message || 'Nu am putut actualiza linia comenzii.'] });
        }
      }
    });
  }
}
