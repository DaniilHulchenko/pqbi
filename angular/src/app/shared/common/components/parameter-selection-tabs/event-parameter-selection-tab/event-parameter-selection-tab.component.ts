import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { QuantitySelectorComponent } from '../../quantity-selector/quantity-selector.component';
import { DxButtonModule, DxCheckBoxModule, DxNumberBoxModule, DxScrollViewModule } from 'devextreme-angular';
import { EventSelectorComponent } from '../../event-selector/event-selector.component';
import { EventClassDescription } from '@shared/service-proxies/service-proxies';
import { ListboxModule } from 'primeng/listbox';
import { EditableTabComponentBaseComponent } from '../editable-tab-component-base';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { PopulatableForm } from '../populatable-form';
import { UtilsModule } from '../../../../../../shared/utils/utils.module';
import { AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';
import { EventAdvancedSettingsComponent } from '../event-advanced-settings/event-advanced-settings.component';
import { FormContainerComponent } from "../../form-container/form-container.component";

@Component({
    selector: 'eventParameterSelectionTab',
    standalone: true,
    imports: [
    CommonModule,
    DxButtonModule,
    DxCheckBoxModule,
    DxNumberBoxModule,
    DxScrollViewModule,
    FormsModule,
    ListboxModule,
    QuantitySelectorComponent,
    EventSelectorComponent,
    UtilsModule,
    EventAdvancedSettingsComponent,
    FormContainerComponent
],
    templateUrl: './event-parameter-selection-tab.component.html',
    styleUrl: './event-parameter-selection-tab.component.css',
})
export class EventParameterSelectionTabComponent extends EditableTabComponentBaseComponent implements PopulatableForm<WidgetParametersColumn> {
    @Input() isInsideTable = false;
    @Output() onAdd: EventEmitter<AddEventParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditEventParameterEventCallBack> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('eventAdvancedSettingsModal') eventAdvancedSettingsModal: EventAdvancedSettingsComponent;
    eventAdvancedSettingsConfig: AdvancedSettingsConfig;

    selectedEvent: EventClassDescription;
    selectedPhases: EventPhaseOptions[];
    selectedParameter: EventParameterOptions;
    polyphase = false;
    aggregation: AggregationDuration = {
        isEnabled: false,
        aggregationValue: null,
    };
    quantity: QuantityUnits;

    phaseOptions = EventPhaseOptions;
    parameterOptions = EventParameterOptions;
    allowedQuantities: QuantityUnits[] = [];

    errors = {
        event: '',
        phase: '',
        parameter: '',
        aggregation: '',
        quantity: '',
    };

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        const data = JSON.parse(parameter.data.toString());
        this.selectedEvent = new EventClassDescription(data.event);
        this.onEventChange();
        this.selectedPhases = data.phases;
        this.selectedParameter = data.parameter;
        this.polyphase = data.isPolyphase;
        this.aggregation = {
            isEnabled: data.aggregationInSeconds !== null,
            aggregationValue: data.aggregationInSeconds,
        };
        this.quantity = parameter.quantity;
        this.eventAdvancedSettingsConfig = parameter.advancedSettings;
    }

    showEventAdvancedSettingsModal(){
        this.eventAdvancedSettingsModal.show();
    }

    onEventAdvancedSettingsChanged(cfg: AdvancedSettingsConfig) {
        this.eventAdvancedSettingsConfig = cfg;
    }

    onEventChange() {
        if (this.selectedEvent && this.selectedEvent.eventClass) {
            const isEnergy = this.isEnergyGroup(this.selectedEvent.eventClass.toString());
            this.allowedQuantities = isEnergy
                ? [QuantityUnits.SUM, QuantityUnits.MIN, QuantityUnits.MAX, QuantityUnits.COUNT]
                : [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX, QuantityUnits.COUNT];
        } else {
            this.allowedQuantities = [];
        }
    }

    isEnergyGroup(eventClass: string): boolean {
        const energyPrefixes = ['ENERGY'];
        return energyPrefixes.some((prefix) => eventClass.startsWith(prefix));
    }

    getAllowedQuantities(): QuantityUnits[] {
        const isEnergy =
            this.selectedEvent && this.selectedEvent.eventClass
                ? this.isEnergyGroup(this.selectedEvent.eventClass.toString())
                : false;

        return isEnergy
            ? [QuantityUnits.SUM, QuantityUnits.MIN, QuantityUnits.MAX]
            : [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX];
    }

    onAggregationChange() {
        if (this.aggregation.isEnabled) {
            if (this.aggregation.aggregationValue === null || this.aggregation.aggregationValue < 0) {
                this.aggregation.aggregationValue = 0;
            }
        } else {
            this.aggregation.aggregationValue = null;
        }
    }

    isFormValid(): boolean {
        return (
            !!this.selectedEvent &&
            !!this.selectedPhases &&
            !!this.selectedParameter &&
            (!this.aggregation.isEnabled ||
                (this.aggregation.aggregationValue !== null && this.aggregation.aggregationValue > 0)) &&
            !!this.quantity
        );
    }

    validateInputs() {
        this.errors.event = this.selectedEvent ? '' : 'Select value';
        this.errors.phase = this.selectedPhases ? '' : 'Select value';
        this.errors.parameter = this.selectedParameter ? '' : 'Select value';

        if (this.aggregation.isEnabled) {
            this.errors.aggregation =
                this.aggregation.aggregationValue === null || this.aggregation.aggregationValue <= 0
                    ? 'Select value'
                    : '';
        } else {
            this.errors.aggregation = '';
        }

        this.errors.quantity = this.quantity ? '' : 'Select value';
    }

    add() {
        this.validateInputs();

        if (this.isFormValid()) {
            if (this.isEdit) {
                this.editSave();
                return;
            }

            let event: AddEventParameterEventCallBack = {
                event: this.selectedEvent,
                parameter: this.selectedParameter,
                phases: [],
                polyphase: this.polyphase,
                aggregation: this.aggregation,
                quantity: this.quantity,
                advancedSettings: this.eventAdvancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.eventAdvancedSettingsConfig)) 
                                    : undefined,
            };

            if (this.polyphase) {
                event.phases = this.selectedPhases;
                this.onAdd.emit(event);
            } else {
                ArrayUtils.ensureArray(this.selectedPhases).forEach((phase) => {
                    event.phases = [phase];
                    this.onAdd.emit(event);
                });
            }
            this.reset();
        }
    }

    reset() {
        this.pqsForm.reset();
        this.selectedEvent = null;
        this.selectedPhases = null;
        this.selectedParameter = null;
        this.polyphase = false;
        this.aggregation = { isEnabled: false, aggregationValue: null };
        this.quantity = null;

        this.errors = {
            event: '',
            phase: '',
            parameter: '',
            aggregation: '',
            quantity: '',
        };
    }

    private editSave() {
        const editSaveEvent: EditEventParameterEventCallBack = {
            id: this.editObjectId,
            event: this.selectedEvent,
            parameter: this.selectedParameter,
            phases: this.selectedPhases,
            polyphase: this.polyphase,
            aggregation: this.aggregation,
            quantity: this.quantity,
        };

        this.onEditSave.emit(editSaveEvent);
        this.finishEdit();
    }
}

export interface AddEventParameterEventCallBack {
    event: EventClassDescription;
    phases: EventPhaseOptions[];
    parameter: EventParameterOptions;
    polyphase: boolean;
    aggregation: AggregationDuration;
    quantity: QuantityUnits;
    advancedSettings?: AdvancedSettingsConfig;
}

export interface EditEventParameterEventCallBack extends AddEventParameterEventCallBack {
    id: string;
}

export enum EventPhaseOptions {
    L1 = 'L1',
    L2 = 'L2',
    L3 = 'L3'
}

export interface AggregationDuration {
    isEnabled: boolean;
    aggregationValue?: number;
}

export enum EventParameterOptions{
    Duration = 'Duration',
    Value = 'Value',
    Deviation = 'Deviation'
}
