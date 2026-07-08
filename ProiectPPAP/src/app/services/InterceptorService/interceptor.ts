import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

import { AuthService } from '../AuthService/auth';
@Injectable()
export class InterceptorService implements HttpInterceptor {

  constructor(
    private router: Router,
    private injector: Injector
  ) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    let token: string | null = null;

    const utilizatorRaw = localStorage.getItem('utilizator');

    if (utilizatorRaw) {
      try {
        const utilizatorObiect = JSON.parse(utilizatorRaw);

        token = utilizatorObiect.jwt;
      } catch (e) {
        console.error('Eroare la parsarea obiectului utilizator din localStorage', e);
      }
    }

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          console.warn('Sesiunea a expirat. Utilizatorul este delogat automat.');

          const authService = this.injector.get(AuthService);

          authService.logout();

          this.router.navigate(['/login']);
        }

        return throwError(() => error);
      })
    );
  }
}
