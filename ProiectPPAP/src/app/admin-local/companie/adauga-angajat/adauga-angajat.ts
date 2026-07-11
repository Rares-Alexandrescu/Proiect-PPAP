import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-adauga-angajat',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './adauga-angajat.html',
  styleUrls: ['./adauga-angajat.scss']
})
export class AdaugaAngajatComponent implements OnInit, OnDestroy {
  adaugaAngajatForm!: FormGroup;

  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  eroriBackend = signal<any>({});
  mesajEroareGeneral = signal<string>('');
  seIncarca = signal<boolean>(false);

  private subscriptions: Subscription = new Subscription();

  ngOnInit(): void {

    this.adaugaAngajatForm = this.fb.group({
      identificatorAngajat: ['', [Validators.required]]
    });

    this.curataErorileLaTastare();
  }

  private curataErorileLaTastare(): void {
    const control = this.adaugaAngajatForm.get('identificatorAngajat');
    if (control) {
      const sub = control.valueChanges.subscribe(() => {
        this.mesajEroareGeneral.set('');

        const eroriCurente = { ...this.eroriBackend() };
        if (eroriCurente['identificator'] || eroriCurente['identificatorAngajat']) {
          delete eroriCurente['identificator'];
          delete eroriCurente['identificatorAngajat'];
          this.eroriBackend.set(eroriCurente);
        }
      });
      this.subscriptions.add(sub);
    }
  }

  onSave(): void {
    if (this.adaugaAngajatForm.invalid) {
      this.adaugaAngajatForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.eroriBackend.set({});
    this.mesajEroareGeneral.set('');

    const formData = this.adaugaAngajatForm.value;

    this.http.post<any>(`${environment.apiUrl}/admin-companie/adauga-angajat`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);


        this.router.navigate(['/admin-companie/vezi-companie'], {
          state: { mesajSucces: response.message || 'Angajatul a fost adăugat cu succes în companie!' }
        });
      },
      error: (err) => {
        this.seIncarca.set(false);
        console.error('Eroare primită de la backend:', err);

        if (err.error && err.error.eroriIdentificator) {
          this.eroriBackend.set(err.error.eroriIdentificator);
        } else if (err.error && err.error.message) {
          this.mesajEroareGeneral.set(err.error.message);
        } else {
          this.mesajEroareGeneral.set('A apărut o problemă neașteptată la adăugarea angajatului.');
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
