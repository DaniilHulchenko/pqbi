import { Injectable } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { XmlHttpRequestHelper } from '@shared/helpers/XmlHttpRequestHelper';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import { IAjaxResponse } from 'abp-ng2-module';

@Injectable()
export class AppAuthService {

    constructor(private _appSessionService: AppSessionService) {}

    logout(reload?: boolean, returnUrl?: string): void {
        let customHeaders = {
            [abp.multiTenancy.tenantIdCookieName]: abp.multiTenancy.getTenantIdCookie(),
            Authorization: 'Bearer ' + abp.auth.getToken(),
        };

        XmlHttpRequestHelper.ajax(
            'GET',
            AppConsts.remoteServiceBaseUrl + '/api/TokenAuth/LogOut',
            customHeaders,
            null,
            () => {
                this.logoutInternal(reload, returnUrl);
            },
            (errorResult: IAjaxResponse) => {
                if (errorResult.unAuthorizedRequest) {
                    abp.log.error(errorResult.error);
                    this.logoutInternal(reload, returnUrl);
                } else {
                    abp.message.error(errorResult.error.message);
                }
            }
        );
    }

    logoutInternal(reload?: boolean, returnUrl?: string): void {
        abp.auth.clearToken();
        abp.auth.clearRefreshToken();
        this._appSessionService.clearSession();
        new LocalStorageService().removeItem(AppConsts.authorization.encrptedAuthTokenName, () => {
            if (reload !== false) {
                if (returnUrl) {
                    location.href = returnUrl;
                } else {
                    location.href = '';
                }
            }
        });
    }
}
