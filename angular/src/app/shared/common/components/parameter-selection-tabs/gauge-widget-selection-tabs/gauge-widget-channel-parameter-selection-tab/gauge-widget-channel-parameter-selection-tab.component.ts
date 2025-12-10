import { Component, EventEmitter, Injector, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { DxButtonModule, DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { UtilsModule } from '@shared/utils/utils.module';
import { FormContainerComponent } from '../../../form-container/form-container.component';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ComponentsState } from '@app/shared/models/components-state';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { BaseParameterCreationTreeBuilder } from '@app/shared/services/base-parameter-creation-tree-builder';
import { ParameterCombinationsService } from '@app/shared/services/parameter-combinations-service';
import { uniqBy, orderBy } from 'lodash-es';
import {
    NormalizeEnum,
    FeederComponentInfo,
    GroupDataInfo,
    PhaseDataInfo,
    BaseDataInfo,
    QuantityDataInfo,
} from '@shared/service-proxies/service-proxies';
import { EditableTabComponentBaseComponent } from '../../editable-tab-component-base';
import { PopulatableForm } from '../../populatable-form';
import safeStringify from 'fast-safe-stringify';
import { LimitedComponentsSelectorComponent } from '../../../limited-components-selector/limited-components-selector.component';
import { GaugeWidgetParameterAdvancedSettingsComponent } from '../gauge-widget-parameter-advanced-settings/gauge-widget-parameter-advanced-settings.component';
import { GaugeWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';

@Component({
    selector: 'gaugeWidgetChannelParameterSelectionTab',
    standalone: true,
    imports: [
        CommonModule,
        LimitedComponentsSelectorComponent,
        DxButtonModule,
        DxScrollViewModule,
        FormsModule,
        ListboxModule,
        UtilsModule,
        GaugeWidgetParameterAdvancedSettingsComponent,
        FormContainerComponent,
    ],
    templateUrl: './gauge-widget-channel-parameter-selection-tab.component.html',
    styleUrl: './gauge-widget-channel-parameter-selection-tab.component.css',
})
export class GaugeWidgetChannelParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Output() onAdd: EventEmitter<AddBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @Output() onEditCancel: EventEmitter<void> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: GaugeWidgetParameterAdvancedSettingsComponent;

    parameter: Parameter = {
        type: BaseParameterType.Channel,
        fromComponents: null,
        group: null,
        phase: null,
        harmonics: { range: null, rangeOn: null },
        baseResolution: null,
        quantity: null,
        resolution: null,
        operator: null,
        aggregationFunction: null,
    };
    componentsState: ComponentsState;

    selectedGroup: any;

    selectedPhases: string[] | string = [];
    selectedHarmonics: number[] | number = [];
    selectedBases: string[] | string = [];
    selectedQuantities: string[] | string = [];

    groupOptions: any[] = [];
    phaseOptions: any[] = [];
    harmonicOptions: any[] = [];
    baseOptions: any[] = [];
    quantityOptions: any[] = [];

    componentPhaseArrays: any[][];
    componentBaseArrays: any[][];

    groupError = '';
    channelError = '';
    harmonicsError = '';
    baseError = '';
    quantityError = '';

    advancedSettingsConfig: GaugeWidgetAdvancedSettingsConfig;

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

    private trees = {};

    constructor(
        injector: Injector,
        private _parameterCombinationsService: ParameterCombinationsService,
        private _baseParameterCreationTreeBuilder: BaseParameterCreationTreeBuilder,
    ) {
        super(injector);
    }

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);

        this.advancedSettingsConfig = parameter.gaugeWidgetAdvancedSettings ?? this.defaultAdvancedSettings;
    }

    populateForm(parameter: WidgetParametersColumn): void {
        this.componentsState = JSON.parse(safeStringify(parameter.componentsState));
        this.onComponentsChange();

        this.parameter = JSON.parse(parameter.data.toString());
        this.selectedGroup = this.groupOptions.find((option) => option.groupName === this.parameter.group);
        this.updatePhaseOptions();
        if (this.selectedGroup.isHarmonic) {
            this.updateHarmonicsOptions();
        }

        this.selectedPhases = this.isEdit ? this.parameter.phase : ArrayUtils.ensureArray(this.parameter.phase);
        this.updateBaseOptions();

        this.selectedBases = this.isEdit
            ? this.parameter.baseResolution
            : ArrayUtils.ensureArray(this.parameter.baseResolution);
        this.updateQuantityOptions();

        const quantity = 'Q' + this.parameter.quantity;
        this.selectedQuantities = this.isEdit ? quantity : ArrayUtils.ensureArray(quantity);
        this.selectedHarmonics = this.isEdit
            ? this.parameter.harmonics?.value
            : ArrayUtils.ensureArray(this.parameter.harmonics?.value);
    }

    populateComponentsFromTab(tab: any) {
        if (!tab || !tab.componentsState) {
            return;
        }
        this.componentsState = JSON.parse(safeStringify(tab.componentsState));
        this.onComponentsChange();
    }

    isHarmonicsGroupSelected(): boolean {
        return this.selectedGroup?.isHarmonic;
    }

    onComponentsChange() {
        this.resetDependentSelections();
        this.updateGroupOptions();

        if (this.parameter.group) {
            const groupModel = this.groupOptions.find((option) => option.groupName === this.parameter.group);
            if (!groupModel || groupModel.disabled) {
                this.parameter.group = null;
                this.selectedGroup = null;
            }
        }
    }

    onAdvancedSettingsChanged(config: GaugeWidgetAdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;

        if (this.isEdit) {
            this.editSave(true);
        }
    }

    onGroupChange(groupEvent: any) {
        if (groupEvent.value) {
            this.parameter.group = groupEvent.value;
            this.selectedGroup = this.groupOptions.find((option) => option.groupName === groupEvent.value);
        } else {
            this.parameter.group = null;
            this.selectedGroup = null;
        }
        this.selectedPhases = [];
        this.selectedBases = [];
        this.selectedHarmonics = [];
        this.selectedQuantities = [];

        this.baseOptions = [];
        this.selectedHarmonics = [];
        this.quantityOptions = [];

        this.updatePhaseOptions();
        if (this.selectedGroup.isHarmonic) {
            this.updateHarmonicsOptions();
        }
    }

    onPhaseChange() {
        this.selectedBases = [];
        this.selectedQuantities = [];

        this.quantityOptions = [];

        this.updateBaseOptions();
    }

    onBaseChange() {
        this.selectedQuantities = [];

        this.updateQuantityOptions();
    }

    onQuantityChange() {
        if (this.isEdit) {
            this.editSave(true);
        }
    }

    isFormValid(): boolean {
        let isValid = true;

        if (!this.parameter.group) {
            this.groupError = 'Select value';
            isValid = false;
        } else {
            this.groupError = '';
        }

        if (!this.selectedPhases?.length) {
            this.channelError = 'Select value';
            isValid = false;
        } else {
            this.channelError = '';
        }

        if (!this.selectedBases?.length) {
            this.baseError = 'Select value';
            isValid = false;
        } else {
            this.baseError = '';
        }

        if (this.selectedGroup?.isHarmonic && !ArrayUtils.ensureArray(this.selectedHarmonics)?.length) {
            this.harmonicsError = 'Select value';
            isValid = false;
        } else {
            this.harmonicsError = '';
        }

        if (!this.selectedQuantities?.length) {
            this.quantityError = 'Select value';
            isValid = false;
        } else {
            this.quantityError = '';
        }

        return isValid;
    }

    add() {
        if (this.isEdit) {
            this.editSave();
            return;
        }
        this.emitComponentParameters();
        this.reset();
    }

    reset() {
        this.selectedGroup = null;
        this.selectedPhases = [];
        this.selectedBases = [];
        this.selectedQuantities = [];
        this.selectedHarmonics = [];
        this.componentsState = null;

        this.pqsForm.reset();
    }

    showAdvancedSettingsModal() {
        const parameterName = this.parameter?.name;
        this.advancedSettingsConfig = {
            ...(this.advancedSettingsConfig ?? {}),
            parameterName: this.advancedSettingsConfig?.parameterName ?? parameterName,
        } as GaugeWidgetAdvancedSettingsConfig;

        this.advancedSettingsModal.show(this.advancedSettingsConfig);
    }

    protected cancelEdit() {
        this.onEditCancel.emit();
    }

    private editSave(isSilentInvoke: boolean = false) {
        let event: EditBaseParameterEventCallBack = {
            id: this.editObjectId,
            parameter: JSON.parse(safeStringify(this.parameter)),
            componentsState: JSON.parse(safeStringify(this.componentsState)),
            quantity: null,
            gaugeWidgetAdvancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettings,
        };

        this._parameterCombinationsService
            .combineParameters(
                event.parameter,
                this.parameter.group,
                ArrayUtils.ensureArray(this.selectedPhases),
                ArrayUtils.ensureArray(this.selectedBases),
                ArrayUtils.ensureArray(this.selectedQuantities).map((quantity) => quantity.slice(1)),
                ArrayUtils.ensureArray(this.selectedHarmonics),
            )
            .subscribe((result) => {
                if (result) {
                    event.parameter = result;
                    event.quantity = QuantityUnits[result.quantity];
                    this.onEditSave.emit(event);
                }
            });

        if (!isSilentInvoke){
            this.finishEdit();
        }
    }

    private emitComponentParameters() {
        let event: AddBaseParameterEventCallBack = {
            parameter: JSON.parse(JSON.stringify(this.parameter)),
            componentsState: JSON.parse(safeStringify(this.componentsState)),
            quantity: null,
            gaugeWidgetAdvancedSettings: this.advancedSettingsConfig
                ? JSON.parse(JSON.stringify(this.advancedSettingsConfig))
                : this.defaultAdvancedSettings,
        };

        this._parameterCombinationsService
            .combineParameters(
                event.parameter,
                this.parameter.group,
                ArrayUtils.ensureArray(this.selectedPhases),
                ArrayUtils.ensureArray(this.selectedBases),
                ArrayUtils.ensureArray(this.selectedQuantities).map((quantity) => quantity.slice(1)),
                ArrayUtils.ensureArray(this.selectedHarmonics),
            )
            .subscribe((result) => {
                if (result) {
                    event.parameter = result;
                    event.quantity = QuantityUnits[result.quantity];
                    this.onAdd.emit(event);
                }
            });
    }

    private updateGroupOptions() {
        this.groupOptions = [];
        let componentGroupsArray = [];
        this.componentsState?.components?.forEach((component) => {
            if (!this.trees[component.key]) {
                this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildTree(
                    BaseParameterType.Channel,
                    component.key,
                    component.parameterInfos,
                    component.customBaseInfo,
                    component.channels,
                )[component.key];
            }

            this.groupOptions.push(
                ...this.trees[component.key].groups.map(
                    (group) =>
                        new GroupDataInfo({
                            groupId: group.groupId,
                            groupName: group.groupName,
                            description: group.description,
                            isHarmonic: group.isHarmonic,
                        }),
                ),
            );
            componentGroupsArray.unshift(this.trees[component.key]);
        });
        this.groupOptions = uniqBy(this.groupOptions, 'groupId');
        this.groupOptions = orderBy(this.groupOptions, 'description', 'asc');
        for (let group of this.groupOptions) {
            if (componentGroupsArray.some((arr) => !arr.groups.some((item) => item.groupId === group.groupId))) {
                group.disabled = true;
            }
        }
    }

    private updatePhaseOptions() {
        this.phaseOptions = [];
        this.componentPhaseArrays = [];
        this.componentsState?.components?.forEach((component) => {
            const phases = this.trees[component.key].groups.find(
                (group) => group.groupName === this.parameter.group,
            ).phases;

            this.phaseOptions.push(
                ...phases.map(
                    (phase) =>
                        new PhaseDataInfo({
                            phase: phase.phase,
                            phaseName: phase.phaseName,
                            description: phase.description,
                        }),
                ),
            );
            this.componentPhaseArrays.unshift(phases);
        });

        this.phaseOptions = uniqBy(this.phaseOptions, 'phaseName');
        this.phaseOptions = orderBy(this.phaseOptions, 'description', 'asc');
        for (let phase of this.phaseOptions) {
            if (this.componentPhaseArrays.some((arr) => !arr.some((item) => item.phaseName === phase.phaseName))) {
                phase.disabled = true;
            }
        }
    }

    private updateHarmonicsOptions() {
        this.harmonicOptions = [];
        const componentHarmonicsArrays = [];
        this.componentsState?.components?.forEach((component) => {
            const harmonics = this.trees[component.key].groups.find(
                (group) => group.groupName === this.parameter.group,
            ).harmonics;

            this.harmonicOptions.push(
                ...harmonics.map((harmonic) => {
                    return { description: harmonic };
                }),
            );
            componentHarmonicsArrays.unshift(harmonics);
        });

        this.harmonicOptions = uniqBy(this.harmonicOptions, 'description');
        this.harmonicOptions = orderBy(this.harmonicOptions, 'description', 'asc');
        for (let harmonic of this.harmonicOptions) {
            if (componentHarmonicsArrays.some((arr) => !arr.some((item) => item === harmonic.description))) {
                harmonic.disabled = true;
            }
        }
    }

    private updateBaseOptions() {
        this.baseOptions = [];
        this.componentBaseArrays = [];
        this.componentPhaseArrays.forEach((arr) => {
            const phases = Array.isArray(this.selectedPhases) ? this.selectedPhases : [this.selectedPhases];
            phases.forEach((p) => {
                const bases = arr.find((pArr) => pArr.phaseName === p).bases;

                this.baseOptions.push(
                    ...bases.map(
                        (base) =>
                            new BaseDataInfo({
                                base: base.base,
                                phaseName: base.phaseName,
                                description: base.description,
                            }),
                    ),
                );

                this.componentBaseArrays.unshift(bases);
            });
        });

        this.baseOptions = uniqBy(this.baseOptions, 'base');
        this.baseOptions = orderBy(this.baseOptions, 'description', 'asc');
        for (let base of this.baseOptions) {
            if (this.componentBaseArrays.some((arr) => !arr.some((item) => item.base === base.base))) {
                base.disabled = true;
            }
        }
    }

    private updateQuantityOptions() {
        this.quantityOptions = [];
        const componentQuantityArrays = [];
        this.componentBaseArrays.forEach((arr) => {
            const bases = Array.isArray(this.selectedBases) ? this.selectedBases : [this.selectedBases];
            bases.forEach((b) => {
                const quantities = arr.find((bArr) => bArr.phaseName === b).quantities;

                this.quantityOptions.push(
                    ...quantities.map(
                        (quantity) =>
                            new QuantityDataInfo({
                                quantity: quantity.quantity,
                                phaseName: quantity.phaseName,
                                description: quantity.description,
                            }),
                    ),
                );

                componentQuantityArrays.unshift(quantities);
            });
        });

        this.quantityOptions = uniqBy(this.quantityOptions, 'quantity');
        this.quantityOptions = orderBy(this.quantityOptions, 'description', 'asc');
        for (let quantity of this.quantityOptions) {
            if (componentQuantityArrays.some((arr) => !arr.some((item) => item.quantity === quantity.quantity))) {
                quantity.disabled = true;
            }
        }
    }

    private resetDependentSelections(): void {
        this.parameter.group = null;
        this.selectedGroup = null;
        this.selectedPhases = [];
        this.selectedBases = [];
        this.selectedQuantities = [];
        this.selectedHarmonics = [];
    }
}
