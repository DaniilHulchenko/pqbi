import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComponentsSelectorComponent } from '../../../components-selector/components-selector.component';
import { CustomParameterSelectorComponent } from '../../../custom-parameter-selector/custom-parameter-selector.component';
import { DxButtonModule, DxListModule, DxScrollViewModule } from 'devextreme-angular';
import { FormsModule, NgForm } from '@angular/forms';
import { QuantitySelectorComponent } from '../../../quantity-selector/quantity-selector.component';
import { UtilsModule } from '@shared/utils/utils.module';
import { FormContainerComponent } from '../../../form-container/form-container.component';
import { ComponentsState } from '@app/shared/models/components-state';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { EditableTabComponentBaseComponent } from '../../editable-tab-component-base';
import { PopulatableForm } from '../../populatable-form';
import { CustomParameterDto, NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import safeStringify from 'fast-safe-stringify';
import { LimitedComponentsSelectorComponent } from '../../../limited-components-selector/limited-components-selector.component';
import { GaugeWidgetParameterAdvancedSettingsComponent } from '../gauge-widget-parameter-advanced-settings/gauge-widget-parameter-advanced-settings.component';
import { GaugeWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';

@Component({
    selector: 'gaugeWidgetCustomParameterSelectionTab',
    standalone: false,
    imports: [
        CommonModule,
        LimitedComponentsSelectorComponent,
        CustomParameterSelectorComponent,
        DxButtonModule,
        FormsModule,
        QuantitySelectorComponent,
        DxScrollViewModule,
        DxListModule,
        UtilsModule,
        GaugeWidgetParameterAdvancedSettingsComponent,
        FormContainerComponent,
    ],
    templateUrl: './gauge-widget-custom-parameter-selection-tab.component.html',
    styleUrl: './gauge-widget-custom-parameter-selection-tab.component.css',
})
export class GaugeWidgetCustomParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Input() customParameterTypes: string[] | undefined;
    @Output() onAdd: EventEmitter<AddGaugeCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditGaugeCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild(ComponentsSelectorComponent) componentsSelector: ComponentsSelectorComponent;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: GaugeWidgetParameterAdvancedSettingsComponent;

    componentsState: ComponentsState;
    multipleFeedersSelected: boolean;
    customParameterId: number;
    customParameterType: string;
    quantity: QuantityUnits;
    advancedSettingsConfig: GaugeWidgetAdvancedSettingsConfig;

    groupError = '';
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
        decimalPoints: null,
        linkPage: null,
        titleFont: { family: '', size: null, colorMode: 'scheme', customColor: '#000000' },
        valueFont: { family: '', size: null, colorMode: 'scheme', customColor: '#000000' },
        segments: [],
        unit: { unitType: 'auto', selectedUnit: '' },
        marker1: null,
        marker2: null,
        colorScheme: ColorSchema.None,
        outOfLimitColor: '',
    };

    get typesOptions() {
        return this.multipleFeedersSelected ? ['SPMC'] : ['SPMC', 'MPSC'];
    }

    isFormValid(): boolean {
        return !!this.customParameterId && !!this.quantity;
    }

    onComponentsChange() {
        this.multipleFeedersSelected =
            this.componentsState?.feeders?.length > 1 || this.componentsState?.components?.length > 1;

        if (this.customParameterType === 'MPSC' && this.multipleFeedersSelected) {
            this.customParameterId = null;
            this.groupError = 'Select value';
        } else {
            this.groupError = '';
        }
    }

    onCustomParameterChange(customParameter: CustomParameterDto) {
        this.customParameterType = customParameter.type;

        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
    }

    onQuantityChange() {
        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
    }

    onAdvancedSettingsChanged(config: GaugeWidgetAdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;

        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
    }

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.componentsState = JSON.parse(safeStringify(parameter.componentsState));
        this.onComponentsChange();
        this.customParameterId = +parameter.data;
        this.quantity = parameter.quantity;
        this.advancedSettingsConfig = parameter.gaugeWidgetAdvancedSettings
            ? { ...parameter.gaugeWidgetAdvancedSettings }
            : this.defaultAdvancedSettings;
    }

    populateComponentsFromTab(tab: any) {
        if (!tab || !tab.componentsState) {
            return;
        }
        const state = JSON.parse(safeStringify(tab.componentsState));
        // if(this.componentsState.components.length === 0){
        //     this.componentsState.feeders = null
        // }
        this.componentsState = state;

        this.componentsState.feeders = state.feeders.filter((feeder) =>
            new Set(state.components.map((c) => c.key)).has(feeder.parent),
        );
    }

    showAdvancedSettingsModal() {
        this.advancedSettingsModal.show(this.advancedSettingsConfig);
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

            this.emitAddParameter();

            this.reset();
        }
    }

    reset() {
        this.customParameterId = null;
        this.quantity = null;
        this.pqsForm.reset();
    }

    private editSave(isSilentInvoke: boolean = false) {
        const editSaveEvent: EditGaugeCustomParameterEventCallBack = {
            id: this.editObjectId,
            componentsState: JSON.parse(safeStringify(this.componentsState)),
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            advancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettings,
        };

        this.onEditSave.emit(editSaveEvent);
        
        if (!isSilentInvoke){
            this.finishEdit();
        }
    }

    private emitAddParameter() {
        this.onAdd.emit({
            componentsState: JSON.parse(safeStringify(this.componentsState)),
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            advancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettings,
        });
    }

    protected cancelEdit() {
        this.onEditCancel.emit();
    }
}

export interface AddGaugeCustomParameterEventCallBack {
    componentsState: ComponentsState;
    customParameterId: number;
    quantity: QuantityUnits;
    advancedSettings?: GaugeWidgetAdvancedSettingsConfig;
}

export interface EditGaugeCustomParameterEventCallBack extends AddGaugeCustomParameterEventCallBack {
    id: string;
}
