import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class InterceptorService implements HttpInterceptor {

  constructor() { }

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
      const clonedRequest = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next.handle(clonedRequest);
    }

    return next.handle(request);
  }
}
