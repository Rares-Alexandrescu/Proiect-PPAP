import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterModule } from '@angular/router';


import { environment } from '../../environments/environment';


@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './register.html',
  styleUrls: ['./register.scss']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);

  isLoading = false;
  mesajEroareGenerala: string | null = null;

  registerForm: FormGroup = this.fb.group({
    nume: ['', Validators.required],
    prenume: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    cnp: ['', Validators.required],
    parola: ['', Validators.required],
    parolaConfirmare: ['', Validators.required]
  });

  ngOnInit() {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);

      control?.valueChanges.subscribe(() => {
        if (control.hasError('serverErrors')) {
          const eroriCurente = control.errors;
          if (eroriCurente) {
            delete eroriCurente['serverErrors'];
            control.setErrors(Object.keys(eroriCurente).length > 0 ? eroriCurente : null);
          }
        }
      });
    });
  }

  onRegister() {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.mesajEroareGenerala = null;

    this.http.post(`${environment.apiUrl}/register`, this.registerForm.value)
      .subscribe({
        next: (response: any) => {
          this.isLoading = false;
          alert(response.message);
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.isLoading = false;

          if (err.error && err.error.eroriCampuri) {
            const eroriServer = err.error.eroriCampuri;

            Object.keys(eroriServer).forEach(camp => {
              const control = this.registerForm.get(camp);
              if (control) {
                control.setErrors({ serverErrors: eroriServer[camp] });
              }
            });
          } else if (err.error && err.error.message) {
            this.mesajEroareGenerala = err.error.message;
          }
        }
      });
  }
}
