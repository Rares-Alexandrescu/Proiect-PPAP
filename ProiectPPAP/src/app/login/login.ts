import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';

// Magia pentru URL-ul dinamic
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

  isLoading = false;
  mesajEroare: string | null = null;
  afiseazaSuccesConfirmare = false;

  loginForm: FormGroup = this.fb.group({
    identificator: ['', Validators.required],
    parola: ['', Validators.required]
  });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['confirmat'] === 'true') {
        this.afiseazaSuccesConfirmare = true;
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

    // Aici folosim apiUrl din environment
    this.http.post(`${environment.apiUrl}/login`, loginPayload).subscribe({
      next: (user: any) => {
        this.isLoading = false;
        alert(`Autentificare reușită! Salut, ${user.nume}.`);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading = false;
        this.mesajEroare = err.error.message || 'A apărut o eroare la conectare.';
      }
    });
  }
}
