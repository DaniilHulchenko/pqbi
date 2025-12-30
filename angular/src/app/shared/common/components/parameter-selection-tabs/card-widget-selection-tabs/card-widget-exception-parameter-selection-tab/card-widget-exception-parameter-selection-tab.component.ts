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
import { CardWidgetParameterAdvancedSettingsComponent } from '../card-widget-parameter-advanced-settings/card-widget-parameter-advanced-settings.component';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { CustomParameterDto, NormalizeEnum } from '@shared/service-proxies/service-proxies';

@Component({
    selector: 'cardWidgetExceptionParameterSelectionTab',
    standalone: true,
    imports: [
        CommonModule,
        CustomParameterSelectorComponent,
        DxButtonModule,
        FormsModule,
        QuantitySelectorComponent,
        UtilsModule,
        CardWidgetParameterAdvancedSettingsComponent,
    ],
    templateUrl: './card-widget-exception-parameter-selection-tab.component.html',
    styleUrl: './card-widget-exception-parameter-selection-tab.component.css',
})
export class CardWidgetExceptionParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Output() onAdd: EventEmitter<AddCardWidgetExceptionEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditCardWidgetExceptionEventCallBack> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: CardWidgetParameterAdvancedSettingsComponent;

    customParameterId: number;
    quantity: QuantityUnits;
    advancedSettingsConfig: CardWidgetAdvancedSettingsConfig;

    customParameterError = '';
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
        decimalPoints: 2,
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

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.customParameterId = +parameter.data;
        this.quantity = parameter.quantity;
        this.advancedSettingsConfig = parameter.cardWidgetAdvancedSettings;
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
            if (this.isEdit) {
                this.editSave();
                return;
            }

            this.onAdd.emit({
                customParameterId: this.customParameterId,
                quantity: this.quantity,
                cardWidgetAdvancedSettings: this.advancedSettingsConfig
                    ? this.advancedSettingsConfig
                    : this.defaultAdvancedSettingConfig,
            });
            this.reset();
        }
    }

    showAdvancedSettingsModal() {
        this.advancedSettingsModal.show(this.advancedSettingsConfig);
    }

    onAdvancedSettingsChanged(config: CardWidgetAdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;

        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
    }

    onCustomParameterChange(customParameter: CustomParameterDto) {
        if (this.isEdit && this.isFormValid) {
            this.editSave(true);
        }
    }

    onQuantityChange() {
        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
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
        const editSaveEvent: EditCardWidgetExceptionEventCallBack = {
            id: this.editObjectId,
            customParameterId: this.customParameterId,
            quantity: this.quantity,
            cardWidgetAdvancedSettings: this.advancedSettingsConfig
                ? this.advancedSettingsConfig
                : this.defaultAdvancedSettingConfig,
        };

        this.onEditSave.emit(editSaveEvent);

        if (!isSilentInvoke){
            this.finishEdit();
        }
    }
}

export interface AddCardWidgetExceptionEventCallBack {
    customParameterId: number;
    quantity: QuantityUnits;
    cardWidgetAdvancedSettings?: CardWidgetAdvancedSettingsConfig;
}

export interface EditCardWidgetExceptionEventCallBack extends AddCardWidgetExceptionEventCallBack {
    id: string;
}
