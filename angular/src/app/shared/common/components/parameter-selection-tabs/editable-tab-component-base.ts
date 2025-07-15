import { AppComponentBase } from '@shared/common/app-component-base';
import { PopulatableForm } from './populatable-form';

export class EditableTabComponentBaseComponent extends AppComponentBase {
    isEdit = false;
    editObjectId: string | null;

    startEdit(id: string) {
        this.isEdit = true;
        this.editObjectId = id;
    }

    finishEdit() {
        this.isEdit = false;
        this.editObjectId = null;
        this.reset();
    }

    reset() {}
}
