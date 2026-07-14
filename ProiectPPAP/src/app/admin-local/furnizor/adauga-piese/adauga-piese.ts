import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-add-piesa',
  standalone: true,
  templateUrl: './add-piesa.html',
  styleUrls: ['./add-piesa.scss'], 
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class AdaugaPiesaComponent implements OnInit, OnDestroy {
  adaugaPiesaForm!: FormGroup;

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
    this.adaugaPiesaForm = this.fb.group({
      nume_Piesa: [''],
      pret_Cumparare: [0]
    });

    this.curataErorileLaTastare();
  }

  private curataErorileLaTastare(): void {

    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_Piesa': ['nume_Piesa', 'Nume_Piesa', 'nume_piesa'],
      'pret_Cumparare': ['pret_Cumparare', 'Pret_Cumparare', 'pret_cumparare']
    };

    Object.keys(this.adaugaPiesaForm.controls).forEach(numeInput => {
      const control = this.adaugaPiesaForm.get(numeInput);

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
    if (this.adaugaPiesaForm.invalid) {
      this.adaugaPiesaForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.adaugaPiesaForm.value;
    console.log('Datele care pleacă spre C# (Creare Piesă):', formData);

    this.http.post<any>(`${environment.apiUrl}/admin-furnizor/add-piesa`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        this.mesajSucces.set(response.message || 'Piesa a fost adăugată cu succes!');
        console.log("Succes creare piesă:", response);

        this.adaugaPiesaForm.reset({ pret_Cumparare: 0 });

        this.router.navigate(['/admin-furnizor/vezi-piese'], {
          state: { mesajSucces: 'Piesa a fost adăugată cu succes!' }
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
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la adăugarea piesei.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
