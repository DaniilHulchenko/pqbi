import { Component, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ComponentsSelectorComponent } from '../../../components-selector/components-selector.component';
import { CustomParameterSelectorComponent } from '../../../custom-parameter-selector/custom-parameter-selector.component';
import { DxButtonModule, DxListModule, DxScrollViewModule } from 'devextreme-angular';
import { FormsModule, NgForm } from '@angular/forms';
import { QuantitySelectorComponent } from '../../../quantity-selector/quantity-selector.component';
import { UtilsModule } from '@shared/utils/utils.module';
import { AdvancedSettingsComponent } from '../../advanced-settings/advanced-settings.component';
import { FormContainerComponent } from '../../../form-container/form-container.component';
import { ComponentsState } from '@app/shared/models/components-state';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { EditableTabComponentBaseComponent } from '../../editable-tab-component-base';
import { PopulatableForm } from '../../populatable-form';
import { CustomParameterDto, NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import safeStringify from 'fast-safe-stringify';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { LimitedComponentsSelectorComponent } from '../../../limited-components-selector/limited-components-selector.component';
import { CardWidgetParameterAdvancedSettingsComponent } from '../card-widget-parameter-advanced-settings/card-widget-parameter-advanced-settings.component';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';

@Component({
    selector: 'cardWidgetCustomParameterSelectionTab',
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
        CardWidgetParameterAdvancedSettingsComponent,
        FormContainerComponent,
    ],
    templateUrl: './card-widget-custom-parameter-selection-tab.component.html',
    styleUrl: './card-widget-custom-parameter-selection-tab.component.css',
})
export class CardWidgetCustomParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Input() customParameterTypes: string[] | undefined;
    @Output() onAdd: EventEmitter<AddCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild(ComponentsSelectorComponent) componentsSelector: ComponentsSelectorComponent;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: CardWidgetParameterAdvancedSettingsComponent;

    componentsState: ComponentsState;
    multipleFeedersSelected: boolean;
    customParameterId: number;
    customParameterType: string;
    quantity: QuantityUnits;
    advancedSettingsConfig: CardWidgetAdvancedSettingsConfig;

    groupError = '';
    quantityError = '';
    allowedQuantities: QuantityUnits[] = [QuantityUnits.AVG, QuantityUnits.MIN, QuantityUnits.MAX];

    private readonly defaultAdvancedSettingConfig: CardWidgetAdvancedSettingsConfig = {
        normalizeValue: NormalizeEnum.NO,
        normalizeNominalValue: 0,
        excludeFlagged: ExcludeFlagged.None,
        defaultFlagEvent: [],
        setLimits: Limit.None,
        lowerLimit: 0,
        upperLimit: 0,
        limitFromNominal: false,
        limitFromNormalization: false,
        colorScheme: ColorSchema.None,
        outOfLimitColor: '',
        gradientFromColor: '',
        gradientToColor: '',
        okColor: '',
        noDataColor: '',
        showOkColor: false,
        showNoDataColor: false,
        decimalPoints: null,
        linkPage: null,
        icon: {
            id: null,
            file: null,
            name: null,
            size: 32,
            sizeUnit: 'px',
            appearance: 'always',
            colorMode: 'scheme',
            customColor: '#000000',
            setAsDefault: false,
        },
        titleFont: { family: '', size: null, colorMode: 'scheme', customColor: '#000000' },
        valueFont: { family: '', size: null, colorMode: 'scheme', customColor: '#000000' },
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

        if (this.isEdit && this.isFormValid) {
            this.editSave(true);
        }
    }

    onQuantityChange() {
        if (this.isEdit && this.isFormValid) {
            this.editSave(true);
        }
    }

    onAdvancedSettingsChanged(config: CardWidgetAdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;

        if (this.isEdit && this.isFormValid) {
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
        this.advancedSettingsConfig = parameter.cardWidgetAdvancedSettings
            ? { ...parameter.cardWidgetAdvancedSettings }
            : this.defaultAdvancedSettingConfig;
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
        const editSaveEvent: EditCustomParameterEventCallBack = {
            id: this.editObjectId,
            componentsState: this.componentsState,
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            advancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettingConfig,
        };

        this.onEditSave.emit(editSaveEvent);

        if (!isSilentInvoke){
            this.finishEdit();
        }
    }

    private emitAddParameter() {
        this.onAdd.emit({
            componentsState: new ComponentsState(this.componentsState),
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            advancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettingConfig,
        });
    }

    protected cancelEdit() {
        this.onEditCancel.emit();
    }
}

export interface AddCustomParameterEventCallBack {
    componentsState: ComponentsState;
    customParameterId: number;
    quantity: QuantityUnits;
    advancedSettings?: CardWidgetAdvancedSettingsConfig;
}

export interface EditCustomParameterEventCallBack extends AddCustomParameterEventCallBack {
    id: string;
}
