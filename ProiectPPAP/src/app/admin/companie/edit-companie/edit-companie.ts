import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-edit-companie',
  standalone: true,
  templateUrl: './edit-companie.html',
  styleUrls: ['./edit-companie.scss'],
  imports: [CommonModule, ReactiveFormsModule]
})
export class EditCompanieComponent implements OnInit, OnDestroy {
  editCompanieForm!: FormGroup;
  companieId!: number;

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
      this.companieId = +idParam;
    } else {
      this.router.navigate(['/admin/companii']);
      return;
    }

    this.editCompanieForm = this.fb.group({
      nume_Companie: [''],
      email: [''],
      numar_Telefon: [''],
      cnpAdminLocal: ['']
    });

    this.incarcaDateCompanie();
    this.curataErorileLaTastare();
  }

  private incarcaDateCompanie(): void {
    this.http.get<any>(`${environment.apiUrl}/admin/edit-companie/${this.companieId}`).subscribe({
      next: (response) => {
        console.log('DATE PRIMITE PENTRU EDITARE COMPANIE:', response);

        const companie = response.companie;

        this.editCompanieForm.patchValue({
          nume_Companie: companie.nume_Companie,
          email: companie.email,
          numar_Telefon: companie.numar_Telefon,
          cnpAdminLocal: companie.cnpAdminLocal || companie.CnpAdminLocal
        });
      },
      error: (err) => {
        console.error('Eroare la aducerea datelor companiei:', err);
        this.eroriBackend.set({ eroareGenerala: ['Nu s-au putut încărca datele companiei.'] });
      }
    });
  }

  private curataErorileLaTastare(): void {
    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_Companie': ['nume_Companie', 'Nume_Companie'],
      'email': ['email', 'Email'],
      'numar_Telefon': ['numar_Telefon', 'Numar_Telefon'],
      'cnpAdminLocal': ['cnpAdminLocal', 'CnpAdminLocal', 'identificator']
    };

    Object.keys(this.editCompanieForm.controls).forEach(numeInput => {
      const control = this.editCompanieForm.get(numeInput);

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
    if (this.editCompanieForm.invalid) {
      this.editCompanieForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.editCompanieForm.value;
    console.log('Datele care pleacă spre C#:', formData);

    this.http.put<any>(`${environment.apiUrl}/admin/edit-companie/${this.companieId}`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        this.mesajSucces.set(response.message || 'Compania a fost actualizată cu succes!');
        console.log("Succes:", response);

        setTimeout(() => {
          this.router.navigate(['/admin/companii'], {
            state: { mesajSucces: 'Compania a fost editată cu succes!' }
          });
        }, 1500);
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
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la salvarea datelor.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
