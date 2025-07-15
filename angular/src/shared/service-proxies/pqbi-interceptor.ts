import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AbpHttpConfigurationService, AbpHttpInterceptor, TokenService } from 'abp-ng2-module';
import { blobToText } from './service-proxies';
import { Router } from '@angular/router';
import notify from 'devextreme/ui/notify';

@Injectable()
export class PQBIInterceptor extends AbpHttpInterceptor {
    private _criticalIssueCode = 1;

    constructor(configuration: AbpHttpConfigurationService, injector: Injector, private tokenService: TokenService, private _router: Router) {
        super(configuration, injector);
    }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return super.intercept(request, next).pipe(
            tap({
                next: (event) => event,
                error: (error) => {
                    blobToText(error.error).subscribe(err => {
                        let errorModel = JSON.parse(err);
                        if (errorModel.error.code === this._criticalIssueCode) {
                            abp.message.confirm(errorModel.error.message, 'Error', (isConfirmed) => {
                                this.tokenService.clearToken();
                                this._router.navigateByUrl('/account/login');
                            }, {
                                showCancelButton: false,
                                icon: 'error'
                            });
                        }
                    });
                },
            }),
        );
    }
}
