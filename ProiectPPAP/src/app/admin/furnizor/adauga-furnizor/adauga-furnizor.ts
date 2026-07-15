import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-adauga-furnizor',
  imports: [ReactiveFormsModule],
  templateUrl: './adauga-furnizor.html',
  styleUrl: './adauga-furnizor.scss',
})
export class AdaugaFurnizorComponent implements OnInit, OnDestroy {
  adaugaFurnizorForm!: FormGroup;

  eroriBackend = signal<any>({});
  mesajSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.adaugaFurnizorForm = this.fb.group({
      nume_furnizor: [''],
      email_furnizor: [''],
      numar_telefon: [''],
      identificatorAngajat: ['']
    });

    this.curataErorileLaTastare();
  }

  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_furnizor': ['nume_furnizor'],
      'email_furnizor': ['email_furnizor'],
      'numar_telefon': [ 'numar_telefon'],
      'identificatorAngajat': ['identificatorAngajat']
    };

    Object.keys(this.adaugaFurnizorForm.controls).forEach(numeInput => {
      const control = this.adaugaFurnizorForm.get(numeInput);

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

  onSave(): void {
    if (this.adaugaFurnizorForm.invalid) {
      this.adaugaFurnizorForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.adaugaFurnizorForm.value;
    console.log('Datele care pleacă spre C# (Creare Furnizor):', formData);

    this.http.post<any>(`${environment.apiUrl}/admin/adauga-furnizor`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        this.mesajSucces.set(response.message || 'Furnizorul a fost adăugat cu succes!');
        console.log("Succes creare furnizor:", response);

        this.adaugaFurnizorForm.reset();


        this.router.navigate(['/admin/vezi-furnizorii'], {
          state: { mesajSucces: 'Furnizorul a fost adăugat cu succes!' }
        });
      },
      error: (err) => {
        this.seIncarca.set(false);
        console.log('Eroare brută de la C#:', err);

        if (err.error && err.error.eroriCampuri) {
          this.eroriBackend.set(err.error.eroriCampuri);
          console.log('Erori salvate pentru HTML:', this.eroriBackend());
        } else if (err.error && err.error.message) {
          this.eroriBackend.set({ eroareGenerala: [err.error.message] });
        } else {
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la adăugarea furnizorului.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
