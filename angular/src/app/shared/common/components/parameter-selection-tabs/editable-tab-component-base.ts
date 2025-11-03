import { AppComponentBase } from '@shared/common/app-component-base';

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
        this.cancelEdit();
    }

    reset() {}
    isFormValid(): boolean {
        return false;
    }
    protected cancelEdit() {}
}
