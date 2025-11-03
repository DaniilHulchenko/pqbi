import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { DxButtonModule } from 'devextreme-angular';
import { UtilsModule } from '@shared/utils/utils.module';
import { CustomParameterSelectorComponent } from '../../../custom-parameter-selector/custom-parameter-selector.component';
import { QuantitySelectorComponent } from '../../../quantity-selector/quantity-selector.component';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { EditableTabComponentBaseComponent } from '../../editable-tab-component-base';
import { PopulatableForm } from '../../populatable-form';
import { GaugeWidgetParameterAdvancedSettingsComponent } from '../gauge-widget-parameter-advanced-settings/gauge-widget-parameter-advanced-settings.component';
import { GaugeWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { NormalizeEnum } from '@shared/service-proxies/service-proxies';

@Component({
    selector: 'gaugeWidgetExceptionParameterSelectionTab',
    standalone: true,
    imports: [
        CommonModule,
        CustomParameterSelectorComponent,
        DxButtonModule,
        FormsModule,
        QuantitySelectorComponent,
        UtilsModule,
        GaugeWidgetParameterAdvancedSettingsComponent,
    ],
    templateUrl: './gauge-widget-exception-parameter-selection-tab.component.html',
    styleUrl: './gauge-widget-exception-parameter-selection-tab.component.css',
})
export class GaugeWidgetExceptionParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Output() onAdd: EventEmitter<AddGaugeWidgetExceptionEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditGaugeWidgetExceptionEventCallBack> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: GaugeWidgetParameterAdvancedSettingsComponent;

    customParameterId: number;
    quantity: QuantityUnits;
    advancedSettingsConfig: GaugeWidgetAdvancedSettingsConfig;

    customParameterError = '';
    quantityError = '';
    allowedQuantities: QuantityUnits[] = [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX];

    private readonly defaultAdvancedSettings: GaugeWidgetAdvancedSettingsConfig = {
        normalizeValue: NormalizeEnum.NO,
        normalizeNominalValue: 0,
        excludeFlagged: ExcludeFlagged.None,
        defaultFlagEvent: [],
        setLimits: Limit.None,
        lowerLimit: 0,
        upperLimit: 0,
        limitFromNominal: false,
        limitFromNormalization: false,
        decimalPoints: 2,
        linkPage: null,
        titleFont: { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' },
        valueFont: { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' },
        segments: [],
        unit: { unitType: 'auto', selectedUnit: '' },
        marker1: null,
        marker2: null,
        colorScheme: ColorSchema.None,
        outOfLimitColor: '',
    };

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.customParameterId = +parameter.data;
        this.quantity = parameter.quantity;
        this.advancedSettingsConfig = parameter.gaugeWidgetAdvancedSettings;
    }

    isFormValid(): boolean {
        return !!this.customParameterId && !!this.quantity;
    }

    onCustomParameterChange() {
        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
    }

    onQuantityChange() {
        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
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
            if (this.isEdit) {
                this.editSave();
                return;
            }

            this.onAdd.emit({
                customParameterId: this.customParameterId,
                quantity: this.quantity,
                gaugeWidgetAdvancedSettings: this.advancedSettingsConfig 
                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                    : this.defaultAdvancedSettings,
            });
            this.reset();
        }
    }

    showAdvancedSettingsModal() {
        this.advancedSettingsModal.show();
    }

    onAdvancedSettingsChanged(config: GaugeWidgetAdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;
    }

    reset() {
        this.pqsForm.reset();
        this.customParameterError = '';
        this.quantityError = '';
    }

    protected cancelEdit() {
        this.onEditCancel.emit();
    }

    private editSave(isSilentInvoke: boolean = false) {
        const editSaveEvent: EditGaugeWidgetExceptionEventCallBack = {
            id: this.editObjectId,
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            gaugeWidgetAdvancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettings,
        };

        this.onEditSave.emit(editSaveEvent);
        
        if (!isSilentInvoke){
            this.finishEdit();
        }
    }
}

export interface AddGaugeWidgetExceptionEventCallBack {
    customParameterId: number;
    quantity: QuantityUnits;
    gaugeWidgetAdvancedSettings?: GaugeWidgetAdvancedSettingsConfig;
}

export interface EditGaugeWidgetExceptionEventCallBack extends AddGaugeWidgetExceptionEventCallBack {
    id: string;
}
