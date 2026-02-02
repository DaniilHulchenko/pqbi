import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { LocalizationRoutingModule } from './localization-routing.module';
import { LocalizationComponent } from './localization.component';
import { DxSelectBoxModule, DxRadioGroupModule, DxNumberBoxModule } from 'devextreme-angular';

@NgModule({
    declarations: [
        
    ],
    imports: [
        LocalizationComponent,
        AppSharedModule,
        AdminSharedModule,
        LocalizationRoutingModule,
        DxSelectBoxModule,
        DxRadioGroupModule,
        DxNumberBoxModule,
    ],
})
export class LocalizationModule {}

