import { Injectable, Injector, NgZone } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AbpHttpConfigurationService, AbpHttpInterceptor, TokenService } from 'abp-ng2-module';
import { blobToText } from './service-proxies';
import { Router } from '@angular/router';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';

@Injectable()
export class PQBIInterceptor extends AbpHttpInterceptor {
    private readonly _criticalIssueCode = 1;

  constructor(
    private abpHttpConfiguration: AbpHttpConfigurationService,
    injector: Injector,
    private authService: AppAuthService
  ) {
    super(abpHttpConfiguration, injector);
  }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return super.intercept(request, next).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.error instanceof Blob && error.error.type === 'application/json') {
          return blobToText(error.error).pipe(
            switchMap(text => {
              let errorModel;
              try {
                errorModel = JSON.parse(text);
              } catch {
                return throwError(() => error);
              }

              const code = errorModel?.error?.code;

              if (code === this._criticalIssueCode) {
                this.authService.logout(true);
              }

              return throwError(() => error);
            })
          );
        }

        return throwError(() => error);
      })
    );
  }
}
