import { Component, EventEmitter, Output } from '@angular/core';
import { Subgroup } from '@app/shared/models/subgroup';
import { Guid } from '@node_modules/guid-ts/lib';

@Component({
    selector: 'subgroupCreateOrEditBlock',
    templateUrl: './subgroup-create-or-edit-block.component.html',
    styleUrl: './subgroup-create-or-edit-block.component.css',
})
export class SubgroupCreateOrEditBlockComponent {
    @Output() onAdd: EventEmitter<Subgroup> = new EventEmitter<Subgroup>();
    @Output() onEditSave: EventEmitter<Subgroup> = new EventEmitter<Subgroup>();

    subgroup: Subgroup = new Subgroup();
    isEditMode: boolean = false;


    isFormValid(): boolean {
        return this.subgroup?.name?.trim() !== '' 
            && ((this.subgroup?.fromVal !== null && this.subgroup?.fromVal !== undefined)
            || (this.subgroup?.toVal !== null && this.subgroup?.toVal !== undefined));
    }

    edit(subgroup: Subgroup) {
        this.subgroup = { ...subgroup };
        this.isEditMode = true;
    }

    save() {
        console.log(this.subgroup)
        console.log(this.subgroup?.fromVal, this.subgroup?.toVal)
        if (this.isEditMode){
            this.onEditSave.emit(this.subgroup);
            this.finishEdit();
        } else
        {
            this.subgroup.id = Guid.newGuid().toString();
            this.onAdd.emit(this.subgroup);
            this.subgroup = new Subgroup();
        }
    }

    finishEdit() {
        this.isEditMode = false;
        this.subgroup = new Subgroup();
    }
}
