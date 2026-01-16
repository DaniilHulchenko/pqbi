import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { DashboardSettingsRoutingModule } from './dashboard-settings-routing.module';
import { DashboardSettingsComponent } from './dashboard-settings.component';
import { DxNumberBoxModule, DxColorBoxModule, DxTabPanelModule, DxDataGridModule, DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { AdvancedParameterSelectorComponent } from './shared/advanced-parameter-selector/advanced-parameter-selector.component';
import { FormContainerComponent } from '@app/shared/common/components/form-container/form-container.component';

@NgModule({
    declarations: [
        DashboardSettingsComponent,
        AdvancedParameterSelectorComponent,
    ],
    imports: [
        AppSharedModule,
        AdminSharedModule,
        DashboardSettingsRoutingModule,
        DxNumberBoxModule,
        DxColorBoxModule,
        DxTabPanelModule,
        DxDataGridModule,
        DxScrollViewModule,
        ListboxModule,
        FormContainerComponent,
    ],
})
export class DashboardSettingsModule {}
