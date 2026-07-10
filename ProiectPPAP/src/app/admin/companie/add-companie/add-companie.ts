import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-add-companie',
  standalone: true,
  templateUrl: './add-companie.html',
  styleUrls: ['./add-companie.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class AdaugaCompanieComponent implements OnInit, OnDestroy {
  adaugaCompanieForm!: FormGroup;

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
    this.adaugaCompanieForm = this.fb.group({
      nume_Companie: [''],
      email: [''],
      numar_Telefon: [''],
      cnpAdminLocal: ['']
    });

    this.curataErorileLaTastare();
  }

  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_Companie': ['nume_Companie', 'Nume_Companie'],
      'email': ['email', 'Email'],
      'numar_Telefon': ['numar_Telefon', 'Numar_Telefon'],
      'cnpAdminLocal': ['cnpAdminLocal', 'CnpAdminLocal', 'identificator']
    };

    Object.keys(this.adaugaCompanieForm.controls).forEach(numeInput => {
      const control = this.adaugaCompanieForm.get(numeInput);

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
    if (this.adaugaCompanieForm.invalid) {
      this.adaugaCompanieForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.adaugaCompanieForm.value;
    console.log('Datele care pleacă spre C# (Creare):', formData);

    this.http.post<any>(`${environment.apiUrl}/admin/add-companie`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        this.mesajSucces.set(response.message || 'Compania a fost adăugată cu succes!');
        console.log("Succes:", response);

        this.adaugaCompanieForm.reset();

        this.router.navigate(['/admin/vezi-companii'], {
          state: { mesajSucces: 'Compania a fost adăugată cu succes!' }
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
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la adăugarea companiei.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
