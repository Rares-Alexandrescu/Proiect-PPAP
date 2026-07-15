import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-edit-piese',
  standalone: true,
  templateUrl: './edit-piese.html',
  styleUrls: ['./edit-piese.scss'], 
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class EditPiesaComponent implements OnInit, OnDestroy {
  editPiesaForm!: FormGroup;
  idPiesa!: number;

  eroriBackend = signal<any>({});
  mesajSucces = signal<string>('');
  seIncarca = signal<boolean>(false);

  private subscriptions: Subscription = new Subscription();

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.idPiesa = +idParam;
    } else {
      this.router.navigate(['/admin-furnizor/vezi-piese']);
      return;
    }

    this.editPiesaForm = this.fb.group({
      nume_Piesa: [''],
      pret_Cumparare: [0]
    });

    this.incarcaDatePiesa();
    this.curataErorileLaTastare();
  }

  private incarcaDatePiesa(): void {
    this.http.get<any>(`${environment.apiUrl}/admin-furnizor/edit-piesa/${this.idPiesa}`).subscribe({
      next: (response) => {
        console.log('DATE PRIMITE PENTRU EDITARE PIESĂ:', response);

        const piesa = response.piesa || response.Piesa;

        if (piesa) {
          this.editPiesaForm.patchValue({
            nume_Piesa: piesa.nume_Piesa || piesa.Nume_Piesa,
            pret_Cumparare: piesa.pret_Cumparare || piesa.Pret_Cumparare
          });
        }
      },
      error: (err) => {
        console.error('Eroare la aducerea datelor piesei:', err);
        this.eroriBackend.set({ eroareGenerala: ['Nu s-au putut încărca datele piesei.'] });
      }
    });
  }

  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_Piesa': ['nume_Piesa', 'Nume_Piesa', 'nume_piesa'],
      'pret_Cumparare': ['pret_Cumparare', 'Pret_Cumparare', 'pret_cumparare']
    };

    Object.keys(this.editPiesaForm.controls).forEach(numeInput => {
      const control = this.editPiesaForm.get(numeInput);

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

  onSubmit(): void {
    if (this.editPiesaForm.invalid) {
      this.editPiesaForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.editPiesaForm.value;
    console.log('Datele care pleacă spre C# (Piesă):', formData);

    this.http.put<any>(`${environment.apiUrl}/admin-furnizor/edit-piesa/${this.idPiesa}`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        console.log("Succes editare piesă:", response);

        this.router.navigate(['/admin-furnizor/vezi-piese'], {
          state: { mesajSucces: 'Piesa a fost editată cu succes!' }
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
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la salvarea datelor piesei.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
