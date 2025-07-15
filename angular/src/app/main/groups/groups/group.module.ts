import { NgModule } from '@angular/core';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { GroupRoutingModule } from './group-routing.module';
import { GroupsComponent } from './groups.component';
import { CreateOrEditGroupModalComponent } from './create-or-edit-group-modal.component';
import { ViewGroupModalComponent } from './view-group-modal.component';
import { DropdownModule } from 'primeng/dropdown';
import { MultiSelectModule } from 'primeng/multiselect';
import { TreeSelectModule } from 'primeng/treeselect';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { InputTextModule } from 'primeng/inputtext';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';
import { FieldsetModule } from 'primeng/fieldset';
import { PanelModule } from 'primeng/panel';
import { InputNumberModule } from 'primeng/inputnumber';
import { ResolutionSelectorComponent } from '@app/shared/common/components/resolution-selector/resolution-selector.component';
import { DxTooltipModule, DxTabPanelModule, DxScrollViewModule, DxListModule, DxButtonModule, DxPopupModule, DxDataGridModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { SubgroupCreateOrEditBlockComponent } from '@app/shared/common/components/subgroup-create-or-edit-block/subgroup-create-or-edit-block.component';

@NgModule({
    declarations: [GroupsComponent, CreateOrEditGroupModalComponent, ViewGroupModalComponent, SubgroupCreateOrEditBlockComponent],
    imports: [
        AppSharedModule,
        GroupRoutingModule,
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
        InputNumberModule
    ],
})
export class GroupModule {}
