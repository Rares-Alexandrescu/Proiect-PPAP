import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common'; 
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/AuthService/auth';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';


@Component({
  selector: 'app-resetare-parola',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule], 
  templateUrl: './resetare-parola.html',
  styleUrl: './resetare-parola.scss',
})
export class ResetareParolaComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private http = inject(HttpClient);

  tokenDinUrl: string | null = null;
  mesajSucces: string | null = null;
  mesajEroareGenerala: string | null = null;
  isLoading = false;

  cerereForm!: FormGroup;
  resetareForm!: FormGroup;

  cereLinkResetare(emailSauCNP: string) {
    return this.http.post(`${environment.apiUrl}/resetare-parola`, { emailSauCNP: emailSauCNP });
  }

  salveazaParolaNoua(token: string, parolaNoua: string, parolaConfirmare: string) {
    return this.http.put(`${environment.apiUrl}/resetare-parola`, {
      token: token,
      parolaNoua: parolaNoua,
      parolaConfirmare: parolaConfirmare
    });
  }

  ngOnInit() {
 
    this.tokenDinUrl = this.route.snapshot.queryParamMap.get('token');

    if (this.tokenDinUrl) {
      this.resetareForm = this.fb.group({
        parola: ['', Validators.required],
        parolaConfirmare: ['', Validators.required]
      });

      this.curataEroriServerLaTastare(this.resetareForm);
    } else {
      this.cerereForm = this.fb.group({
        identificator: ['', Validators.required]
      });
      this.curataEroriServerLaTastare(this.cerereForm);
    }
  }

  onCereResetare() {
    if (this.cerereForm.invalid) return;

    this.isLoading = true;
    this.mesajEroareGenerala = null;

    const valoare = this.cerereForm.get('identificator')?.value;

    this.cereLinkResetare(valoare).subscribe({
      next: (raspuns: any) => {
        this.isLoading = false;
        this.mesajSucces = raspuns.message; 
        this.cerereForm.reset();
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 400 && err.error?.message) {
          this.cerereForm.get('identificator')?.setErrors({ serverErrors: err.error.message });
        } else {
          this.mesajEroareGenerala = 'A apărut o eroare neașteptată de la server.';
        }
      }
    });
  }
  onSalveazaParola() {
    if (this.resetareForm.invalid || !this.tokenDinUrl) return;

    this.isLoading = true;
    this.mesajEroareGenerala = null;

    const parola = this.resetareForm.get('parola')?.value;
    const confirmare = this.resetareForm.get('parolaConfirmare')?.value;

    this.salveazaParolaNoua(this.tokenDinUrl, parola, confirmare).subscribe({
      next: () => {
        this.isLoading = false;

        this.router.navigate(['/login'], { queryParams: { resetat: 'true' } });
      },
      error: (err) => {
        this.isLoading = false;


        if (err.status === 400 && err.error?.eroriCampuri) {
          const eroriDinCsharp = err.error.eroriCampuri;

          for (const camp in eroriDinCsharp) {
            const control = this.resetareForm.get(camp);
            if (control) {
              control.setErrors({ serverErrors: eroriDinCsharp[camp] });
            }
          }
        } else {

          this.mesajEroareGenerala = err.error?.message || 'Link invalid sau expirat.';
        }
      }
    });
  }

  private curataEroriServerLaTastare(form: FormGroup) {
    Object.keys(form.controls).forEach(key => {
      const control = form.get(key);
      control?.valueChanges.subscribe(() => {
        if (control.hasError('serverErrors')) {
          const erori = control.errors;
          if (erori) {
            delete erori['serverErrors'];
            control.setErrors(Object.keys(erori).length > 0 ? erori : null);
          }
        }
      });
    });
  }
}
