import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { CustomParameterSelectorComponent } from '../../custom-parameter-selector/custom-parameter-selector.component';
import { DxButtonModule } from 'devextreme-angular';
import { FormsModule, NgForm } from '@angular/forms';
import { QuantitySelectorComponent } from '../../quantity-selector/quantity-selector.component';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { CommonModule } from '@angular/common';
import { EditableTabComponentBaseComponent } from '../editable-tab-component-base';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { PopulatableForm } from '../populatable-form';
import { UtilsModule } from '../../../../../../shared/utils/utils.module';

@Component({
    selector: 'exceptionParameterSelectionTab',
    standalone: true,
    imports: [CommonModule, CustomParameterSelectorComponent, DxButtonModule, FormsModule, QuantitySelectorComponent, UtilsModule],
    templateUrl: './exception-parameter-selection-tab.component.html',
    styleUrl: './exception-parameter-selection-tab.component.css',
})
export class ExceptionParameterSelectionTabComponent extends EditableTabComponentBaseComponent implements PopulatableForm<WidgetParametersColumn> {
    @Output() onAdd: EventEmitter<AddExceptionEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditExceptionEventCallBack> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;

    customParameterId: number;
    quantity: QuantityUnits;

    customParameterError = '';
    quantityError = '';
    allowedQuantities: QuantityUnits[] = [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX];

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.customParameterId = +parameter.data;
        this.quantity = parameter.quantity;
    }

    isFormValid(): boolean {
        return !!this.customParameterId && !!this.quantity;
    }

    add() {
        if (!this.customParameterId) {
            this.customParameterError = 'Select value';
        } else {
            this.customParameterError = '';
        }

        if (!this.quantity) {
            this.quantityError = 'Select value';
        } else {
            this.quantityError = '';
        }

        if (this.isFormValid()) {
            if(this.isEdit) {
                this.editSave();
                return;
            }

            this.onAdd.emit({
                customParameterId: this.customParameterId,
                quantity: this.quantity,
            });
            this.reset();
        }
    }

    reset() {
        this.pqsForm.reset();
        this.customParameterError = '';
        this.quantityError = '';
    }

    private editSave() {
        const editSaveEvent: EditExceptionEventCallBack = {
            id: this.editObjectId,
            customParameterId: this.customParameterId,
            quantity: this.quantity,
        };

        this.onEditSave.emit(editSaveEvent);
        this.finishEdit();
    }
}

export interface AddExceptionEventCallBack {
    customParameterId: number;
    quantity: QuantityUnits;
}

export interface EditExceptionEventCallBack extends AddExceptionEventCallBack {
    id: string;
}
