import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
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
  constructor(private http: HttpClient) { }

  private apiUrl = environment.apiUrl;

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

  getAccountDetails(): Observable<any> {
    return this.http.get(`${this.apiUrl}/edit-account`);
  }

  updateAccount(data: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/edit-account`, data);
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
