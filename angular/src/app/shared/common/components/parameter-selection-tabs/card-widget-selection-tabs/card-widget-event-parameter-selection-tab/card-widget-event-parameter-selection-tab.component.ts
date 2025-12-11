import { Component, Input, Output, ViewChild, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { DxButtonModule, DxCheckBoxModule, DxNumberBoxModule, DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { UtilsModule } from '@shared/utils/utils.module';
import { EventSelectorComponent } from '../../../event-selector/event-selector.component';
import { FormContainerComponent } from '../../../form-container/form-container.component';
import { QuantitySelectorComponent } from '../../../quantity-selector/quantity-selector.component';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { EventClassDescription, NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { EditableTabComponentBaseComponent } from '../../editable-tab-component-base';
import { EventPhaseOptions, EventParameterOptions, AggregationDuration, AddEventParameterEventCallBack, EditEventParameterEventCallBack } from '../../event-parameter-selection-tab/event-parameter-selection-tab.component';
import { PopulatableForm } from '../../populatable-form';
import { CardWidgetEventAdvancedSettingsComponent } from '../card-widget-event-advanced-settings/card-widget-event-advanced-settings.component';
import { LimitedComponentsSelectorComponent } from '../../../limited-components-selector/limited-components-selector.component';
import { ComponentsState } from '@app/shared/models/components-state';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';

@Component({
    selector: 'cardWidgetEventParameterSelectionTab',
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
        CardWidgetEventAdvancedSettingsComponent,
        FormContainerComponent,
        LimitedComponentsSelectorComponent,
    ],
    templateUrl: './card-widget-event-parameter-selection-tab.component.html',
    styleUrl: './card-widget-event-parameter-selection-tab.component.css',
})
export class CardWidgetEventParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Output() onAdd: EventEmitter<AddCardWidgetEventParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditCardWidgetEventParameterEventCallBack> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('eventAdvancedSettingsModal') eventAdvancedSettingsModal: CardWidgetEventAdvancedSettingsComponent;
    eventAdvancedSettingsConfig: CardWidgetAdvancedSettingsConfig;

    componentState: ComponentsState;
    selectedEvent: EventClassDescription;
    selectedPhases: EventPhaseOptions | EventPhaseOptions[];
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
    isEdit: any;
    editObjectId: any;

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
            file: null,
            iconId: null,
            defaultIconId: null,
            defaultValueKey: null,
            setAsDefaultIcon: false,
            appearance: 'always',
            colorMode: 'scheme',
            customColor: '#000000',
        },
        titleFont: { family: '', size: 12, colorMode: 'scheme', customColor: '#000000' },
        valueFont: { family: '', size: 16, colorMode: 'scheme', customColor: '#000000' },
    };

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: WidgetParametersColumn): void {
        const data = JSON.parse(parameter.data.toString());
        this.componentState = parameter.componentsState;
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
        this.eventAdvancedSettingsConfig = parameter.cardWidgetAdvancedSettings;
    }

    showEventAdvancedSettingsModal() {
        const parameterName = this.getCurrentParameterName();
        this.eventAdvancedSettingsConfig = {
            ...(this.eventAdvancedSettingsConfig ?? {}),
            parameterName: this.eventAdvancedSettingsConfig?.parameterName ?? parameterName,
        } as CardWidgetAdvancedSettingsConfig;
        this.eventAdvancedSettingsModal.show(this.eventAdvancedSettingsConfig);
    }

    onEventAdvancedSettingsChanged(cfg: CardWidgetAdvancedSettingsConfig) {
        this.eventAdvancedSettingsConfig = cfg;

        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
        }
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

    onPolyphaseChange() {
        if (this.polyphase) {
            this.selectedPhases = ArrayUtils.ensureArray(this.selectedPhases) as EventPhaseOptions[];
        } else {
            this.selectedPhases = this.selectedPhases.at(0) as EventPhaseOptions;
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

    onQuantityChange() {
        if (this.isEdit && this.isFormValid()) {
            this.editSave(true);
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

            let event: AddCardWidgetEventParameterEventCallBack = {
                componentState: this.componentState,
                event: this.selectedEvent,
                parameter: this.selectedParameter,
                phases: ArrayUtils.ensureArray(this.selectedPhases),
                polyphase: this.polyphase,
                aggregation: this.aggregation,
                quantity: this.quantity,
                advancedSettings: this.eventAdvancedSettingsConfig
                    ? JSON.parse(JSON.stringify(this.eventAdvancedSettingsConfig))
                    : this.defaultAdvancedSettingConfig,
            };

            this.onAdd.emit(event);

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

    protected cancelEdit() {
        this.onEditCancel.emit();
    }

    private editSave(isSilentInvoke: boolean = false) {
        const editSaveEvent: EditCardWidgetEventParameterEventCallBack = {
            componentState: this.componentState,
            id: this.editObjectId,
            event: this.selectedEvent,
            parameter: this.selectedParameter,
            phases: ArrayUtils.ensureArray(this.selectedPhases),
            polyphase: this.polyphase,
            aggregation: this.aggregation,
            quantity: this.quantity,
            advancedSettings: this.eventAdvancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.eventAdvancedSettingsConfig))
                : this.defaultAdvancedSettingConfig,
        };

        this.onEditSave.emit(editSaveEvent);
        
        if (!isSilentInvoke){
            this.finishEdit();
        }
    }
     private getCurrentParameterName(): string {
        if (!this.selectedEvent || !this.selectedParameter) {
            return '';
        }

        const phaseNames = ArrayUtils.ensureArray(this.selectedPhases)
            .map((phase) => this.getPhaseName(phase))
            .filter((phaseName) => !!phaseName)
            .join(', ');

        const eventName = this.selectedEvent.name ?? this.selectedEvent.eventClass ?? '';

        return `${eventName}${phaseNames ? ` (${phaseNames})` : ''} ${this.selectedParameter}`.trim();
    }
    private getPhaseName(
        phase: EventPhaseOptions | { name?: string; phaseName?: string } | string
    ): string {
        if (typeof phase === 'string') {
            return phase;
        }

        if (phase && typeof phase === 'object') {
            return phase.name ?? phase.phaseName ?? '';
        }

        return '';
    }
}

export interface AddCardWidgetEventParameterEventCallBack {
    componentState: ComponentsState;
    event: EventClassDescription;
    phases: EventPhaseOptions[];
    parameter: EventParameterOptions;
    polyphase: boolean;
    aggregation: AggregationDuration;
    quantity: QuantityUnits;
    advancedSettings?: CardWidgetAdvancedSettingsConfig;
}

export interface EditCardWidgetEventParameterEventCallBack extends AddCardWidgetEventParameterEventCallBack {
    id: string;
}
