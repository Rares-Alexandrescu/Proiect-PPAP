import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-edit-furnizor',
  standalone: true,
  templateUrl: './edit-furnizor.html',
  styleUrls: ['./edit-furnizor.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class EditFurnizorComponent implements OnInit, OnDestroy {
  editFurnizorForm!: FormGroup;
  furnizorId!: number;

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
      this.furnizorId = +idParam;
    } else {
      this.router.navigate(['/admin/vezi-furnizori']);
      return;
    }

    this.editFurnizorForm = this.fb.group({
      nume_furnizor: [''],
      email_furnizor: [''],
      numar_telefon: [''],
      identificatorAngajat: ['']
    });

    this.incarcaDateFurnizor();
    this.curataErorileLaTastare();
  }

  private incarcaDateFurnizor(): void {
    this.http.get<any>(`${environment.apiUrl}/admin/edit-furnizor/${this.furnizorId}`).subscribe({
      next: (response) => {
        console.log('DATE PRIMITE PENTRU EDITARE FURNIZOR:', response);

        const dateFurnizor = response.furnizor;

        if (dateFurnizor) {

          this.editFurnizorForm.patchValue({
            nume_furnizor: dateFurnizor.nume_furnizor,
            email_furnizor: dateFurnizor.email_furnizor,
            numar_telefon: dateFurnizor.numar_telefon,
            identificatorAngajat: dateFurnizor.identificatorAngajat || '***'
          });
        } else {
          console.warn('Atenție: Proprietatea "furnizor" lipsește din răspunsul JSON!');
        }
      },
      error: (err) => {
        console.error('Eroare la aducerea datelor furnizorului:', err);
        if (err.status === 400 && err.error && err.error.message) {
          const mesajPrimit = err.error.message;
          if (mesajPrimit.includes("ID inexistent")) {
            alert(mesajPrimit);
            this.router.navigate(['/admin/vezi-furnizorii']);
            return;
          }
        }
        this.eroriBackend.set({ eroareGenerala: ['Nu s-au putut încărca datele furnizorului.'] });
      }
    });
  }

  private curataErorileLaTastare(): void {

    const mapareCampuri: { [numeInput: string]: string[] } = {
      'nume_furnizor': ['nume_furnizor'],
      'email': ['email'],
      'numar_telefon': ['numar_telefon'],
      'identificatorAngajat': ['identificatorAngajat']
    };

    Object.keys(this.editFurnizorForm.controls).forEach(numeInput => {
      const control = this.editFurnizorForm.get(numeInput);

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
    if (this.editFurnizorForm.invalid) {
      this.editFurnizorForm.markAllAsTouched();
      return;
    }

    this.seIncarca.set(true);
    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.editFurnizorForm.value;
    console.log('Datele care pleacă spre C# (Furnizor):', formData);

    this.http.put<any>(`${environment.apiUrl}/admin/edit-furnizor/${this.furnizorId}`, formData).subscribe({
      next: (response) => {
        this.seIncarca.set(false);
        console.log("Succes editare furnizor:", response);

        this.router.navigate(['/admin/vezi-furnizorii'], {
          state: { mesajSucces: 'Furnizorul a fost editat cu succes!' }
        });
      },
      error: (err) => {
        this.seIncarca.set(false);
        console.log('Eroare brută de la C#:', err);

        if (err.status === 400 && err.error && err.error.message) {
          const mesajPrimit = err.error.message;
          if (mesajPrimit.includes("ID inexistent")) {
            alert(mesajPrimit);
            this.router.navigate(['/admin/vezi-furnizorii']);
            return;
          }
        }
        if (err.error && err.error.eroriCampuri) {
          this.eroriBackend.set(err.error.eroriCampuri);
          console.log('Erori salvate pentru HTML:', this.eroriBackend());
        } else if (err.error && err.error.message) {
          this.eroriBackend.set({ eroareGenerala: [err.error.message] });
        } else {
          this.eroriBackend.set({ eroareGenerala: ['A apărut o problemă la salvarea datelor furnizorului.'] });
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
