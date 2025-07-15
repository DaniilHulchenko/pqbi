import { Component } from '@angular/core';
import { ModalBase } from '@app/shared/interfaces/modal-base';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({ template: '' })
export class WidgetConfigurationModalBaseComponent extends AppComponentBase implements ModalBase{
    modalVisible: boolean;

    open() {
        this.modalVisible = true;
    }
    close() {
        this.modalVisible = false;
    }
}
