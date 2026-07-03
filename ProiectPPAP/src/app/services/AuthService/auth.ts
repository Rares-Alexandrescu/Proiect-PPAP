import { Injectable, signal } from '@angular/core';

export interface UtilizatorSesiune {
  
  nume: string;
  prenume: string;
  id?: number;
  jwt: string;
  email?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  utilizatorSesiune = signal<UtilizatorSesiune | null>(this.citesteDinStorage());

  login(user: UtilizatorSesiune): void {
    this.utilizatorSesiune.set(user);
    localStorage.setItem('utilizator', JSON.stringify(user));
  }

  logout(): void {
    this.utilizatorSesiune.set(null);
    localStorage.removeItem('utilizator');
  }

  getToken(): string | null {
    const utilizatorCurent = this.utilizatorSesiune();
    return utilizatorCurent ? utilizatorCurent.jwt : null;
  }

  private citesteDinStorage(): UtilizatorSesiune | null {
    const raw = localStorage.getItem('utilizator');
    return raw ? JSON.parse(raw) : null;
  }
  public isLoggedIn(): boolean {
    const utilizator = this.citesteDinStorage();
    return utilizator !== null;
  }
}
