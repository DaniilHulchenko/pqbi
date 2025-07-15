import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { UICustomizationRoutingModule } from './ui-customization-routing.module';
import { UiCustomizationComponent } from './ui-customization.component';
import { DefaultThemeUiSettingsComponent } from './default-theme-ui-settings.component';

@NgModule({
    declarations: [
        UiCustomizationComponent,
        DefaultThemeUiSettingsComponent
    ],
    imports: [AppSharedModule, AdminSharedModule, UICustomizationRoutingModule],
})
export class UICustomizationModule {}
