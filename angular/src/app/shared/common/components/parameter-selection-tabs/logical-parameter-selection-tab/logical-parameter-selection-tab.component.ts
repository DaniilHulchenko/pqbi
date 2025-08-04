import { Component, EventEmitter, Injector, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ComponentsSelectorComponent } from '../../components-selector/components-selector.component';
import { DxButtonModule, DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { DxListModule } from 'devextreme-angular';
import { ComponentsState } from '@app/shared/models/components-state';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ParameterCombinationsService } from '@app/shared/services/parameter-combinations-service';
import { BaseParameterCreationTreeBuilder } from '@app/shared/services/base-parameter-creation-tree-builder';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { orderBy, uniqBy } from 'lodash-es';
import { BaseDataInfo, GroupDataInfo, NormalizeEnum, PhaseDataInfo, QuantityDataInfo, Tag } from '@shared/service-proxies/service-proxies';
import { EditableTabComponentBaseComponent } from '../editable-tab-component-base';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import safeStringify from 'fast-safe-stringify';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { PopulatableForm } from '../populatable-form';
import { UtilsModule } from '../../../../../../shared/utils/utils.module';
import { AdvancedSettingsComponent, AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';
import { FormContainerComponent } from "../../form-container/form-container.component";
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';

@Component({
    selector: 'logicalParameterSelectionTab',
    standalone: true,
    imports: [
    CommonModule,
    ComponentsSelectorComponent,
    DxButtonModule,
    DxScrollViewModule,
    FormsModule,
    ListboxModule,
    DxListModule,
    UtilsModule,
    AdvancedSettingsComponent,
    FormContainerComponent
],
    templateUrl: './logical-parameter-selection-tab.component.html',
    styleUrl: './logical-parameter-selection-tab.component.css',
})
export class LogicalParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>{
    @Input() disableComponentSelection = false;
    @Input() isInsideTable = false;
    @Output() onAdd: EventEmitter<AddBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild(ComponentsSelectorComponent) componentsSelector: ComponentsSelectorComponent;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: AdvancedSettingsComponent;

    parameter: Parameter = {
        type: BaseParameterType.Logical,
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

    advancedSettingsConfig: AdvancedSettingsConfig;

    groupError = '';
    phaseError = '';
    harmonicsError = '';
    baseError = '';
    quantityError = '';

    private trees = {};

    constructor(
        injector: Injector,
        private _parameterCombinationsService: ParameterCombinationsService,
        private _baseParameterCreationTreeBuilder: BaseParameterCreationTreeBuilder,
    ) {
        super(injector);
    }

    @Input() set outerComponentsState(state: ComponentsState) {
        if (this.disableComponentSelection) {
            this.componentsState = state;
            this.onComponentsChange();
        }
    }

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);

        this.advancedSettingsConfig = parameter.advancedSettings ?? {
            normalizeValue: NormalizeEnum.NO,
            normalizeNominalValue: 0,
            excludeFlagged: ExcludeFlagged.None,
            defaultFlagEvent: null,
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
            aligningIgnored: false,
            replaceAggregation: false,
            customAggregationFunc: ''
        };
    }

    populateForm(parameter: WidgetParametersColumn): void {
        if (!this.disableComponentSelection) {
            this.componentsState = JSON.parse(safeStringify(parameter.componentsState));
            this.onComponentsChange();
        }

        this.parameter = JSON.parse(parameter.data.toString());
        this.selectedGroup = this.groupOptions.find((option) => option.groupName === this.parameter.group);

        this.updatePhaseOptions();
        if (this.selectedGroup?.isHarmonic) {
            this.updateHarmonicsOptions();
        }

        this.selectedPhases = this.isEdit ? this.parameter.phase : ArrayUtils.ensureArray(this.parameter.phase);
        this.updateBaseOptions();

        this.selectedBases = this.isEdit ? this.parameter.baseResolution : ArrayUtils.ensureArray(this.parameter.baseResolution);
        this.updateQuantityOptions();

        const quantity = 'Q' + this.parameter.quantity;
        this.selectedQuantities = this.isEdit ? quantity : ArrayUtils.ensureArray(quantity);
        this.selectedHarmonics = this.isEdit
            ? this.parameter.harmonics?.value
            : ArrayUtils.ensureArray(this.parameter.harmonics?.value);

        this.advancedSettingsConfig = parameter.advancedSettings;
    }

    populateComponentsFromTab(tab: any) {
        const state = JSON.parse(safeStringify(tab.componentsState));
        this.componentsState = state;
        this.componentsState.feeders = state.feeders.filter((feeder) =>
            new Set(state.components.map((c) => c.key)).has(feeder.parent),
        );
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

    onAdvancedSettingsChanged(config: AdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;
    }

    add() {
        if (this.isEdit) {
            this.editSave();
            return;
        }

        this.emitComponentParameters();

        this.reset();
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
            this.phaseError = 'Select value';
            isValid = false;
        } else {
            this.phaseError = '';
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

    reset() {
        this.selectedGroup = null;
        this.selectedPhases = [];
        this.selectedBases = [];
        this.selectedQuantities = [];
        this.selectedHarmonics = [];
        if (!this.disableComponentSelection) {
            this.componentsState = null;
        }
        this.pqsForm.reset();
    }

    showAdvancedSettingsModal(){
        this.advancedSettingsModal.show();
    }

    private editSave() {
        if (
            !this.disableComponentSelection &&
            (this.componentsState.components?.length > 1 || this.componentsState.feeders?.length > 1)
        ) {
            this.onEditDelete.emit(this.editObjectId);
            this.emitComponentParameters();
            return;
        }

        let event: EditBaseParameterEventCallBack = {
            id: this.editObjectId,
            parameter: JSON.parse(JSON.stringify(this.parameter)),
            componentsState: this.disableComponentSelection ? null : this.componentsState,
            quantity: null,
            advancedSettings: this.advancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                    : undefined,
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

        this.finishEdit();
    }

    private emitComponentParameters() {
        if (!this.disableComponentSelection) {
            for (let component of this.componentsState.components) {
                for (let feeder of this.componentsState.feeders?.filter((f) => f.componentId === component.key)) {
                    const eventComponentState = new ComponentsState({
                        components: [component],
                        tags: null,
                        pickListState: this.componentsState.pickListState,
                        feeders: [feeder],
                    });

                    let event: AddBaseParameterEventCallBack = {
                        parameter: JSON.parse(JSON.stringify(this.parameter)),
                        componentsState: eventComponentState,
                        quantity: null,
                        advancedSettings: this.advancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                    : undefined,
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
            }
        } else {
            let event: AddBaseParameterEventCallBack = {
                parameter: JSON.parse(JSON.stringify(this.parameter)),
                componentsState: null,
                quantity: null,
                advancedSettings: this.advancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                    : undefined,
            };

            event.parameter.fromComponents = { feederId: this.componentsState.feeders[0].id, componentId: this.componentsState.feeders[0].componentId};

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
                        event.parameter.fromComponents = null;
                        event.quantity = QuantityUnits[result.quantity];
                        this.onAdd.emit(event);
                    }
                });
        }
    }

    private updateGroupOptions() {
        this.groupOptions = [];
        let componentGroupsArray = [];
        this.componentsState?.components?.forEach((component) => {
            if (!this.trees[component.key]) {
                this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildTree(
                    BaseParameterType.Logical,
                    component.key,
                    component.parameterInfos,
                )[component.key];
            }
        });

        this.componentsState?.feeders?.forEach((feeder) => {
            const groups =
                this.trees[feeder.componentId].feeders.find((f) => f.feederId === feeder.id.toString())?.groups ?? [];
            this.groupOptions.push(
                ...groups.map(
                    (group) =>
                        new GroupDataInfo({
                            groupId: group.groupId,
                            groupName: group.groupName,
                            description: group.description,
                            isHarmonic: group.isHarmonic,
                        }),
                ),
            );
            componentGroupsArray.unshift(groups);
        });
        this.groupOptions = uniqBy(this.groupOptions, 'groupId');
        this.groupOptions = orderBy(this.groupOptions, 'description', 'asc');

        for (let group of this.groupOptions) {
            if (componentGroupsArray.some((arr) => !arr.some((item) => item.groupId === group.groupId))) {
                group.disabled = true;
            }
        }
    }

    private updatePhaseOptions() {
        this.phaseOptions = [];
        this.componentPhaseArrays = [];
        this.componentsState?.feeders?.forEach((feeder) => {
            const phases = this.trees[feeder.componentId].feeders
                .find((f) => f.feederId === feeder.id.toString())
                .groups.find((group) => group.groupName === this.parameter.group).phases;

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
        this.componentsState?.feeders?.forEach((feeder) => {
            const harmonics = this.trees[feeder.componentId].feeders
                .find((f) => f.feederId === feeder.id.toString())
                .groups.find((group) => group.groupName === this.parameter.group).harmonics;

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
