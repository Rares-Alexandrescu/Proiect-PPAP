import { Injectable, signal } from '@angular/core';

export interface UtilizatorSesiune {
  nume: string;
  prenume: string;
  email?: string;
  [key: string]: any; // pentru orice alt câmp venit din backend
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  utilizatorSesiune = signal<UtilizatorSesiune | null>(this.citesteDinStorage());

  login(user: UtilizatorSesiune, token: string): void {
    this.utilizatorSesiune.set(user);
    localStorage.setItem('token', token);
    localStorage.setItem('utilizator', JSON.stringify(user));
  }

  logout(): void {
    this.utilizatorSesiune.set(null);
    localStorage.removeItem('token');
    localStorage.removeItem('utilizator');
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  private citesteDinStorage(): UtilizatorSesiune | null {
    const raw = localStorage.getItem('utilizator');
    return raw ? JSON.parse(raw) : null;
  }
}
