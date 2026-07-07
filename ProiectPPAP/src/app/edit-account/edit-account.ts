import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/AuthService/auth';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-account',
  standalone: true,
  templateUrl: './edit-account.html',
  styleUrls: ['./edit-account.scss'],
  imports: [CommonModule, ReactiveFormsModule]
})
export class EditAccountComponent implements OnInit, OnDestroy {
  editForm!: FormGroup;

  eroriBackend = signal<any>({});
  mesajSucces = signal<string>('');

  private subscriptions: Subscription = new Subscription();

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit(): void {
    if (this.authService.isLoggedIn() == false) {
      this.router.navigate(['/login']);
      return;
    }

    this.editForm = this.fb.group({
      nume: [''],
      prenume: [''],
      emailNou: [''],
      cnpNou: [''],
      parolaVeche: [''],
      parolaNoua: [''],
      parolaNouaConfirmare: ['']
    });

    this.authService.getAccountDetails().subscribe({
      next: (utilizator) => {
        console.log('DATE PRIMITE PENTRU EDITARE:', utilizator);

        this.editForm.patchValue({
          nume: utilizator.nume,
          prenume: utilizator.prenume,
          emailNou: utilizator.email
        });
      },
      error: (err) => console.error('Eroare la aducerea datelor:', err)
    });

    this.curataErorileLaTastare();
  }

  private curataErorileLaTastare(): void {
    const mapareCâmpuri: { [numeInput: string]: string[] } = {
      'nume': ['nume'],
      'prenume': ['prenume'],
      'emailNou': ['email', 'emailNou'],
      'cnpNou': ['cnp', 'cnpNou'],
      'parolaVeche': ['parolaVeche'],
      'parolaNoua': ['parola', 'parolaNoua'],
      'parolaNouaConfirmare': ['parolaConfirmare']
    };

    Object.keys(this.editForm.controls).forEach(numeInput => {
      const control = this.editForm.get(numeInput);

      if (control) {
        const sub = control.valueChanges.subscribe(() => {
          this.mesajSucces.set('');

          const cheiBackend = mapareCâmpuri[numeInput];

          if (cheiBackend) {

            const eroriCurente = { ...this.eroriBackend() };
            let ModificatCeva = false;

            cheiBackend.forEach(cheie => {
              if (eroriCurente[cheie]) {
                delete eroriCurente[cheie]; 
                 ModificatCeva = true;
              }
            });


            if (ModificatCeva) {
              this.eroriBackend.set(eroriCurente);
            }
          }
        });

        this.subscriptions.add(sub);
      }
    });
  }

  onSubmit(): void {

    console.log('Datele care pleacă spre C#:', this.editForm.value);

    this.mesajSucces.set('');
    this.eroriBackend.set({});

    const formData = this.editForm.value;

    this.authService.updateAccount(formData).subscribe({
      next: (response) => {

        this.mesajSucces.set(response.message);
        console.log("Succes:", response);

        this.editForm.patchValue({
          parolaVeche: '',
          parolaNoua: '',
          parolaNouaConfirmare: '',
          cnpNou: ''
        });
        this.router.navigate(['/']);
      },
      error: (err) => {
        console.log('Eroare brută de la C#:', err);

        if (err.error && err.error.eroriCampuri) {

          this.eroriBackend.set(err.error.eroriCampuri);
          console.log('Erori salvate pentru HTML:', this.eroriBackend());
        } else {

          this.eroriBackend.set({ eroare: ['A apărut o problemă la comunicarea cu serverul.'] });
        }
      }
    }); 
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
