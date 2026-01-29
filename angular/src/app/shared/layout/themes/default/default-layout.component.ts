import { Injector, Component, OnInit, Inject } from '@angular/core';
import { DOCUMENT, CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ThemesLayoutBaseComponent } from '@app/shared/layout/themes/themes-layout-base.component';
import { UrlHelper } from '@shared/helpers/UrlHelper';
import { AppConsts } from '@shared/AppConsts';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

import { UtilsModule } from '@shared/utils/utils.module';
import { AppBsModalModule } from '@shared/common/appBsModal/app-bs-modal.module';
// import { LayoutModule } from '@shared/layout/layout.module';

@Component({
    selector: 'default-layout',
    templateUrl: './default-layout.component.html',
    animations: [appModuleAnimation()],
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        FormsModule,

        UtilsModule,
        AppBsModalModule,
        // LayoutModule, 
    ],
})
export class DefaultLayoutComponent
    extends ThemesLayoutBaseComponent
    implements OnInit {

    remoteServiceBaseUrl: string = AppConsts.remoteServiceBaseUrl;
    releaseDate: string;
    webAppGuiVersion: string;

    constructor(
        injector: Injector,
        _dateTimeService: DateTimeService,
        @Inject(DOCUMENT) private document: Document,
    ) {
        super(injector, _dateTimeService);
    }

    ngOnInit() {
        this.installationMode = UrlHelper.isInstallUrl(location.href);

        if (this.currentTheme.baseSettings.menu.defaultMinimizedAside) {
            this.document.body.setAttribute('data-kt-aside-minimize', 'on');
        }

        this.releaseDate = this.appSession.application.releaseDate.toFormat('yyyyLLdd');
        this.webAppGuiVersion = AppConsts.WebAppGuiVersion;
    }

    getMobileMenuSkin(): string {
        return this.appSession.theme.baseSettings.layout.darkMode ? 'dark' : 'light';
    }
}
