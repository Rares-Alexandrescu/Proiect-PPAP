import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../services/AuthService/auth';

import { environment } from '../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  isLoading = false;
  mesajEroare: string | null = null;
  afiseazaSuccesConfirmare = false;

  loginForm: FormGroup = this.fb.group({
    identificator: ['', Validators.required],
    parola: ['', Validators.required]
  });

  ngOnInit() {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/'], { queryParams: this.route.snapshot.queryParams });
    }
    this.route.queryParams.subscribe(params => {
      if (params['confirmat'] === 'true') {
        this.afiseazaSuccesConfirmare = true;
      }
     const eroare = params['eroare'];
      if (eroare) {
        if (eroare === 'token_lipsa' || eroare === 'token_invalid') {
          this.mesajEroare = 'Link-ul de confirmare este invalid sau a expirat. Te rugăm să încerci din nou.';
        }
        else {
          this.mesajEroare = 'A apărut o problemă la confirmarea contului.';
        }
      }
    });
  }

  onLogin() {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    this.mesajEroare = null;

    const loginPayload = {
      email: this.loginForm.value.identificator,
      cnp: this.loginForm.value.identificator,
      parola: this.loginForm.value.parola
    };

    this.http.post(`${environment.apiUrl}/login`, loginPayload).subscribe({
      next: (user: any) => {
        this.isLoading = false;

        this.auth.login(
          {
            id: user.id,
            nume: user.nume,
            prenume: user.prenume,
            jwt: user.jwt
          },
        );


        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading = false;

        console.error('Eroare detaliată de la server:', err);
        if (typeof err.error === 'string') {
          this.mesajEroare = err.error;
        }

        else if (err.error && err.error.message) {
          this.mesajEroare = err.error.message;
        }

        else if (err.error && err.error.title) {
          this.mesajEroare = err.error.title;
        }

        else {
          this.mesajEroare = 'A apărut o eroare la conectare. Te rugăm să încerci din nou.';
        }
      }
    });
  }
}
