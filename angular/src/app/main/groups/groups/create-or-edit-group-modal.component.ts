import { Component, ViewChild, Injector, Output, EventEmitter, OnInit } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { GroupsServiceProxy, CreateOrEditGroupDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';
import { Subgroup } from '@app/shared/models/subgroup';
import { SubgroupCreateOrEditBlockComponent } from '@app/shared/common/components/subgroup-create-or-edit-block/subgroup-create-or-edit-block.component';
import { DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';

@Component({
    selector: 'createOrEditGroupModal',
    templateUrl: './create-or-edit-group-modal.component.html',
})
export class CreateOrEditGroupModalComponent extends AppComponentBase implements OnInit {
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();
    @ViewChild('subgroupBlock') subgroupBlock: SubgroupCreateOrEditBlockComponent;

    active = false;
    saving = false;
    isPopupVisible = false;

    val;

    group: CreateOrEditGroupDto = new CreateOrEditGroupDto();
    subgroups: Subgroup[] = [];

    constructor(
        injector: Injector,
        private _groupsServiceProxy: GroupsServiceProxy,
    ) {
        super(injector);
    }
    show(groupId?: string): void {
        if (!groupId) {
            this.group = new CreateOrEditGroupDto();
            this.group.id = groupId;
            this.subgroups = [];

            this.active = true;
            this.isPopupVisible = true;
        } else {
            this._groupsServiceProxy.getGroupForEdit(groupId).subscribe((result) => {
                this.group = result.group;
                this.subgroups = JSON.parse(result.group.subgroups) || [];
                this.active = true;
                this.isPopupVisible = true;
            });
        }
    }

    onSubgroupDelete(event: any): void {
        this.subgroups = this.subgroups.filter((s) => s.id !== event.data.id);
    }

    onSubgroupEdit(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.subgroupBlock.edit(event.data as Subgroup);
    }

    addSubGroup(subgroup: Subgroup): void {
        this.subgroups.push(subgroup);
    }

    saveEditSubGroup(subgroup: Subgroup): void {
        const index = this.subgroups.findIndex((s) => s.id === subgroup.id);
        if (index !== -1) {
            this.subgroups[index] = subgroup;
        } else {
            this.subgroups.push(subgroup);
        }
    }

    isFormValid(): boolean {
        return this.group.name && this.group.name.trim() !== '' && this.subgroups.length > 0;
    }

    save(): void {
        this.saving = true;

        this.group.subgroups = JSON.stringify(this.subgroups);

        this._groupsServiceProxy
            .createOrEdit(this.group)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active = false;
        this.isPopupVisible = false;
    }

    ngOnInit(): void {}
}
