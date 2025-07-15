import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ComponentsSelectorComponent } from '../../components-selector/components-selector.component';
import { CustomParameterSelectorComponent } from '../../custom-parameter-selector/custom-parameter-selector.component';
import { QuantitySelectorComponent } from '../../quantity-selector/quantity-selector.component';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { DxButtonModule, DxScrollViewModule, DxListModule } from 'devextreme-angular';
import { ComponentsState } from '@app/shared/models/components-state';
import { EditableTabComponentBaseComponent } from '../editable-tab-component-base';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import safeStringify from 'fast-safe-stringify';
import { PopulatableForm } from '../populatable-form';
import { CustomParameterDto } from '@shared/service-proxies/service-proxies';
import { UtilsModule } from '../../../../../../shared/utils/utils.module';
import { AdvancedSettingsComponent, AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';
import { FormContainerComponent } from "../../form-container/form-container.component";

@Component({
    selector: 'customParameterSelectionTab',
    standalone: true,
    imports: [
    CommonModule,
    ComponentsSelectorComponent,
    CustomParameterSelectorComponent,
    DxButtonModule,
    FormsModule,
    QuantitySelectorComponent,
    DxScrollViewModule,
    DxListModule,
    UtilsModule,
    AdvancedSettingsComponent,
    FormContainerComponent
],
    templateUrl: './custom-parameter-selection-tab.component.html',
    styleUrl: './custom-parameter-selection-tab.component.css',
})
export class CustomParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>{
    @Input() disableComponentSelection = false;
    @Input() disableQuantitySelection = false;
    @Input() isInsideTable = false;
    @Input() customParameterTypes: string[] | undefined;
    @Output() onAdd: EventEmitter<AddCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild(ComponentsSelectorComponent) componentsSelector: ComponentsSelectorComponent;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: AdvancedSettingsComponent;

    componentsState: ComponentsState;
    customParameterId: number;
    customParameterType: string;
    quantity: QuantityUnits;
    advancedSettingsConfig: AdvancedSettingsConfig;

    groupError = '';
    quantityError = '';
    allowedQuantities: QuantityUnits[] = [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX];

    isFormValid(): boolean {
        return !!this.customParameterId && (!!this.quantity || this.disableQuantitySelection);
    }

    onCustomParameterChange(customParameter: CustomParameterDto) {
        this.customParameterType = customParameter.type;
    }

    onAdvancedSettingsChanged(config: AdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;
    }

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.componentsState = !this.disableComponentSelection
            ? JSON.parse(safeStringify(parameter.componentsState))
            : null;
        this.customParameterId = +parameter.data;
        this.quantity = parameter.quantity;
        this.advancedSettingsConfig = parameter.advancedSettings ? { ...parameter.advancedSettings } : null;
    }

    populateComponentsFromTab(tab: any) {
        const state = JSON.parse(safeStringify(tab.componentsState));
        // if(this.componentsState.components.length === 0){
        //     this.componentsState.feeders = null
        // }
        this.componentsState = state;

        this.componentsState.feeders = state.feeders.filter((feeder) =>
            new Set(state.components.map((c) => c.key)).has(feeder.parent),
        );
    }

    showAdvancedSettingsModal(){
        this.advancedSettingsModal.show();
    }

    add() {
        if (!this.customParameterId) {
            this.groupError = 'Select value';
        } else {
            this.groupError = '';
        }

        if (!this.quantity) {
            this.quantityError = 'Select value';
        } else {
            this.quantityError = '';
        }

        if (this.isFormValid()) {
            if (this.isEdit) {
                this.editSave();
                return;
            }

            this.emitComponentParameters();

            this.reset();
        }
    }

    reset() {
        this.customParameterId = null;
        this.quantity = null;
        this.pqsForm.reset();
    }

    private editSave() {
        if (
            !this.disableComponentSelection &&
            this.customParameterType !== 'SPMC' &&
            (this.componentsState.components?.length > 1 || this.componentsState.feeders?.length > 1)
        ) {
            this.onEditDelete.emit(this.editObjectId);
            this.emitComponentParameters();

            return;
        }

        const editSaveEvent: EditCustomParameterEventCallBack = {
            id: this.editObjectId,
            componentsState: this.componentsState,
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            advancedSettings: this.advancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                    : undefined,
        };

        this.onEditSave.emit(editSaveEvent);
        this.finishEdit();
    }

    private emitComponentParameters() {
        if (this.disableComponentSelection) {
            this.emitAddParameter(null, this.customParameterId, this.quantity, this.advancedSettingsConfig);
        } else {
            if (this.customParameterType === 'MPSC') {
                for (let component of this.componentsState.components) {
                    for (let feeder of this.componentsState.feeders?.filter((f) => f.componentId === component.key)) {
                        const eventComponentsState: ComponentsState = new ComponentsState({
                            components: [component],
                            tags: null,
                            pickListState: this.componentsState.pickListState,
                            feeders: [feeder],
                        });
                        this.emitAddParameter(eventComponentsState, this.customParameterId, this.quantity, this.advancedSettingsConfig);
                    }
                }
            } else if (this.customParameterType === 'SPMC') {
                this.emitAddParameter(new ComponentsState(this.componentsState), this.customParameterId, this.quantity, this.advancedSettingsConfig);
            }
        }
    }

    private emitAddParameter(componentsState: ComponentsState, customParameterId: number, quantity: QuantityUnits, advancedSettings: AdvancedSettingsConfig) {
        this.onAdd.emit({
            componentsState: componentsState,
            customParameterId: customParameterId,
            quantity: quantity,
            advancedSettings: advancedSettings
                                    ? JSON.parse(JSON.stringify(advancedSettings)) 
                                    : undefined,
        });
    }
}

export interface AddCustomParameterEventCallBack {
    componentsState: ComponentsState;
    customParameterId: number;
    quantity: QuantityUnits;
    advancedSettings?: AdvancedSettingsConfig;
}

export interface EditCustomParameterEventCallBack extends AddCustomParameterEventCallBack {
    id: string;
}
