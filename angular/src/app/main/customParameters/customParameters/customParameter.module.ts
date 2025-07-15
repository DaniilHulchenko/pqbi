import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { CustomParameterRoutingModule } from './customParameter-routing.module';
import { CustomParametersComponent } from './customParameters.component';
import { CreateOrEditCustomParameterModalComponent } from './create-or-edit-customParameter-modal.component';
import { ViewCustomParameterModalComponent } from './view-customParameter-modal.component';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { TreeSelectModule } from 'primeng/treeselect';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { FieldsetModule } from 'primeng/fieldset';
import { PanelModule } from 'primeng/panel';
import { ListboxModule } from 'primeng/listbox';
import { ArithmeticsModalComponent } from './modals/arithmetics-modal/arithmetics-modal.component';
import { ResolutionSelectorComponent } from '../../../shared/common/components/resolution-selector/resolution-selector.component';
import {
    DxTooltipModule,
    DxTabPanelModule,
    DxScrollViewModule,
    DxListModule,
    DxButtonModule,
    DxPopupModule,
    DxDataGridModule
} from 'devextreme-angular';
import { CustomParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { CpBaseParameterSelectionTabComponent } from './cp-base-parameter-selection-tab/cp-base-parameter-selection-tab.component';
import { ComponentsSelectorComponent } from '@app/shared/common/components/components-selector/components-selector.component';
import { QuantitySelectorComponent } from '@app/shared/common/components/quantity-selector/quantity-selector.component';
import { CpCustomParameterSelectionTabComponent } from './cp-custom-parameter-selection-tab/cp-custom-parameter-selection-tab.component';
import { FormContainerComponent } from "../../../shared/common/components/form-container/form-container.component";
import { CpAdditionalParameterSelectionTabComponent } from './cp-additional-parameter-selection-tab/cp-additional-parameter-selection-tab.component';
import { AdvancedSettingsComponent } from '@app/shared/common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';

@NgModule({
    declarations: [
        CustomParametersComponent,
        CreateOrEditCustomParameterModalComponent,
        ViewCustomParameterModalComponent,
        ArithmeticsModalComponent,
        CpBaseParameterSelectionTabComponent,
        CpCustomParameterSelectionTabComponent,
        CpAdditionalParameterSelectionTabComponent,
    ],
    imports: [
    AppSharedModule,
    CustomParameterRoutingModule,
    AdminSharedModule,
    DropdownModule,
    MultiSelectModule,
    TreeSelectModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    InputGroupModule,
    InputGroupAddonModule,
    FieldsetModule,
    PanelModule,
    ListboxModule,
    ResolutionSelectorComponent,
    DxTooltipModule,
    DxTabPanelModule,
    DxScrollViewModule,
    DxListModule,
    DxButtonModule,
    DxPopupModule,
    DxDataGridModule,
    CustomParameterSelectionTabComponent,
    ComponentsSelectorComponent,
    QuantitySelectorComponent,
    FormContainerComponent,
    AdvancedSettingsComponent
],
    exports: [ArithmeticsModalComponent],
})
export class CustomParameterModule {}
