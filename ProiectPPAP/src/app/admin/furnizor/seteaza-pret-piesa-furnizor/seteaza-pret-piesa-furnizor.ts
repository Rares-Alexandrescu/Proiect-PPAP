import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { environment } from '../../../../environments/environment';

export interface Piese {
  piese_Id: number;
  furnizor_Id: number;
  pret_Cumparare: number;
  nume_Piesa: string;
  created_at: string;
}
export interface Furnizor {
  furnizor_Id: number;
  nume_Furnizor: string;
  email_Furnizor: string;
  numar_Telefon: string;
  nume_Admin_Furnizor: string;
  prenume_Admin_Furnizor: string;
}

@Component({
  selector: 'app-seteaza-pret-piesa-furnizor',
  imports: [CommonModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './seteaza-pret-piesa-furnizor.html',
  styleUrl: './seteaza-pret-piesa-furnizor.scss',
})
export class SeteazaPretPiesaFurnizor {

  piese = signal<Piese | null>(null);
  furnizor = signal<Furnizor | null>(null);

  furnizorId!: number;
  pieseId!: number;

  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);

  alertaEroare = signal<string>('');
  alertaSucces = signal<string>('');
  seIncarca = signal<boolean>(false);
  eroriBackend = signal<any>({});

  pretForm!: FormGroup;

  ngOnInit(): void {

    this.pretForm = this.fb.group({
      pret_vanzare: ['', [Validators.required, Validators.min(0.01)]]
    });


    const fId = this.route.snapshot.paramMap.get('furnizorId');
    const pId = this.route.snapshot.paramMap.get('piesaId');

    if (fId && pId) {
      this.furnizorId = +fId;
      this.pieseId = +pId;
      this.incarcaDatePiesa();
    } else {
      console.warn('ID-urile lipsesc din URL! Redirecționăm...');
      this.inapoiLaPiese();
    }
  }

  incarcaDatePiesa(): void {
    this.seIncarca.set(true);

    this.http.get<{ furnizor: Furnizor; piesa: Piese }>(
      `${environment.apiUrl}/admin/seteaza-pret-piesa-furnizor/${this.furnizorId}/${this.pieseId}`
    ).subscribe({
      next: (response) => {
        console.log('Date primite pentru setare preț:', response);
        this.furnizor.set(response.furnizor);
        this.piese.set(response.piesa);

        this.pretForm.patchValue({
          pret_vanzare: response.piesa.pret_Cumparare
        });

        this.seIncarca.set(false);
      },
      error: (err) => {
        this.seIncarca.set(false);
        console.error('Eroare la aducerea datelor:', err);

        if (err.status === 400 && err.error && err.error.message) {
          alert(err.error.message);
          this.inapoiLaPiese();
          return;
        }

        if (err.status === 401) {
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set('Nu s-au putut încărca datele piesei.');
        }
      }
    });
  }

  onSave(): void {
    if (this.pretForm.invalid) {
      this.pretForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.alertaEroare.set('');
    this.alertaSucces.set('');
    this.eroriBackend.set({});

    const formData = this.pretForm.value;

    this.http.put<any>(
      `${environment.apiUrl}/admin/seteaza-pret-piesa-furnizor/${this.furnizorId}/${this.pieseId}`,
      formData
    ).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        console.log("Succes setare preț:", response);

        this.router.navigate(['/admin/vezi-piese-furnizor', this.furnizorId], {
          state: { mesajSucces: response.message || 'Prețul a fost actualizat cu succes!' }
        });
      },
      error: (err) => {
        this.seIncarca.set(false);
        console.error('Eroare la salvarea prețului:', err);


        if (err.status === 400) {
          if (err.error && err.error.eroriCampuri) {
            this.eroriBackend.set(err.error.eroriCampuri);
            return;
          }
          if (err.error && err.error.message) {
            this.alertaEroare.set(err.error.message);
            return;
          }
        }

        if (err.status === 401) {
          this.router.navigate(['/dashboard']);
        } else {
          this.alertaEroare.set('A apărut o problemă la salvarea prețului.');
        }
      }
    });
  }

  inapoiLaPiese(): void {
    if (this.furnizorId) {
      this.router.navigate(['/admin/vezi-piese-furnizor', this.furnizorId]);
    } else {
      this.router.navigate(['/admin/vezi-furnizorii']);
    }
  }
}


