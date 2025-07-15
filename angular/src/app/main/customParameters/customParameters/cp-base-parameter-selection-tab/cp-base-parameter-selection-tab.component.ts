import { Component, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ComponentsState } from '@app/shared/models/components-state';
import {
    BaseDataInfo,
    GroupDataInfo,
    PhaseDataInfo,
    PQSRestApiServiceProxy,
    QuantityDataInfo,
} from '@shared/service-proxies/service-proxies';
import { Parameter } from '../table-parameters/models/parameter';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { ParameterCombinationsService } from '@app/shared/services/parameter-combinations-service';
import { orderBy, sortBy, uniqBy } from 'lodash-es';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { ListboxChangeEvent } from 'primeng/listbox';
import { BaseParserService } from '@app/shared/services/base-parser.service';
import { BaseState } from '@app/shared/models/base-state';
import safeStringify from 'fast-safe-stringify';
import { BaseParameterCreationTreeBuilder } from '@app/shared/services/base-parameter-creation-tree-builder';
import { GridDataItem } from '../create-or-edit-customParameter-modal.component';
import { EditableTabComponentBaseComponent } from '@app/shared/common/components/parameter-selection-tabs/editable-tab-component-base';

@Component({
    selector: 'cpBaseParameterSelectionTab',
    templateUrl: './cp-base-parameter-selection-tab.component.html',
    styleUrl: './cp-base-parameter-selection-tab.component.css',
})
export class CpBaseParameterSelectionTabComponent extends EditableTabComponentBaseComponent implements OnInit {
    @Input() baseParameterType: BaseParameterType = BaseParameterType.Logical;
    @Input() disableComponentSelection = false;
    @Input() dynamicSelection = false;
    @Input() isFeederSelectionEnabled: boolean = false; // По умолчанию отключено
    @Output() onAdd: EventEmitter<AddBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;

    parameter: Parameter = {
        type: this.baseParameterType.toUpperCase(),
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

    selectedGroupModel: any;
    selectedHarmonics: number | number[] | null;
    selectedPhases: string | string[] = [];
    selectedBases: string | string[] = [];
    selectedQuantities: string | string[] = [];

    selectedAggregationFunction: string;
    selectedAggregationArgument: number;

    resolutionState: ResolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });

    selectedOperatorArgument: number | null = null;
    selectedOperator: string | null;

    parameterOptions: any;
    groupOptions: any[];
    phaseOptions: any[];
    baseOptions: any[];
    harmonicOptions: any[] = [];
    quantityOptionsList: any[] = [];

    baseParameterTypes = BaseParameterType;

    groupError = '';
    phaseError = '';
    harmonicsError = '';
    baseError = '';
    quantityError = '';

    resolutionTooltipText = {
        unit: 'The unit part of the required resolution of the base parameter',
        argument: 'The value part of the required resolution of the base parameter',
    };

    _multiple = false;

    minResolution: ResolutionState;

    selectedGroup: any;
    componentPhaseArrays: any[][];
    componentBaseArrays: any[][];

    private _outerResolutionState: ResolutionState;

    private trees = {};

    private readonly _defaultMinResolution = new ResolutionState({
        resolutionUnit: ResolutionUnits.CUSTOM,
        customResolutionValue: 1,
        customResolutionUnit: CustomResolutionUnits.MS,
    });

    private readonly _operatorOptions = sortBy([{ name: 'ABSOLUTE' }, { name: 'DIVIDE' }, { name: 'MULT' }], ['name']);
    private readonly _aggregationFuncOptions = sortBy(
        [{ name: 'AVG' }, { name: 'MAX' }, { name: 'MIN' }, { name: 'COUNT' }, { name: 'PERCENTILE' }],
        ['name'],
    );
    private readonly _quantityOptions = [QuantityUnits.MAX, QuantityUnits.MIN, QuantityUnits.AVG];

    constructor(
        injector: Injector,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
        private _parameterCombinationsService: ParameterCombinationsService,
        private _resolutionService: ResolutionService,
        private _baseParser: BaseParserService,
        private _baseParameterCreationTreeBuilder: BaseParameterCreationTreeBuilder,
    ) {
        super(injector);
    }

    get quantityOptions() {
        return this.selectedBases?.length > 0 ? this._quantityOptions : [];
    }

    get operatorOptions() {
        return this._operatorOptions;
    }

    get aggregationFuncionOptions() {
        return this._aggregationFuncOptions;
    }

    get isAggregationSelectionDisabled() {
        return this.resolutionState && this._outerResolutionState
            ? this._resolutionService.greaterOrEqualThan(this.resolutionState, this._outerResolutionState)
            : false;
    }

    @Input()
    set multiple(value: boolean) {
        if (value !== this._multiple) {
            this.reset();
            this._multiple = value;
        }
    }

    @Input()
    set outerResolutionState(value: ResolutionState) {
        if (this._resolutionService.greaterOrEqualThan(this.resolutionState, value)) {
            this.clearAggregationFunction();
        }
        this._outerResolutionState = value;
    }

    ngOnInit(): void {
        this.minResolution = this._defaultMinResolution;

        if (!this.dynamicSelection) {
            this._pqsRestApiServiceProxy.getStaticData().subscribe((response) => {
                this.parameterOptions = response.staticTreeNode.children.find(
                    (child) => child.value.toLowerCase() === this.baseParameterType.toLowerCase(),
                );
                this.groupOptions = this.parameterOptions.children || [];
                this.groupOptions.forEach((item) => {
                    if (!item.groupName) {
                        item.groupName = item.value;
                    }
                });
            });
        }
        setTimeout(() => this.setDefaultFormState(), 100);
    }

    finishEditing() {
        this.onEditSave.emit();
        this.finishEdit();
    }

    edit(parameter: GridDataItem) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: GridDataItem): void {
        this.parameter = JSON.parse(parameter.data.toString());

        this.selectedGroup = this.groupOptions.find((option) => option.groupName === this.parameter.group);

        this.updatePhaseOptions();

        if (this.isHarmonicsGroupSelected()) {
            this.harmonicOptions = this.getHarmonicsRange(this.selectedGroup.range);
            this.selectedHarmonics = this._multiple
                ? ArrayUtils.ensureArray(this.parameter.harmonics.value)
                : this.parameter.harmonics.value;
        }

        this.selectedPhases = this._multiple ? ArrayUtils.ensureArray(this.parameter.phase) : this.parameter.phase;

        this.UpdateBaseOptionsForDublicateOrEdit();
        this.selectedBases = this._multiple ? ArrayUtils.ensureArray(this.parameter.baseResolution) : this.parameter.baseResolution;

        this.selectedQuantities = this._multiple
            ? ArrayUtils.ensureArray(this.parameter.quantity)
            : this.parameter.quantity;

        this.resolutionState = this._resolutionService.parseStateFromInt(this.parameter.resolution, true);

        let operatorParsed;
        if (this.parameter.operator) {
            operatorParsed =
                this.parameter.operator === 'ABSOLUTE'
                    ? { name: 'ABSOLUTE', value: null }
                    : this.parseOperator(this.parameter.operator);

            this.selectedOperator = operatorParsed.name;
            this.selectedOperatorArgument = operatorParsed.value;
        }

        if (this.parameter.aggregationFunction) {
            const aggregationParsed = this.parseOperator(this.parameter.aggregationFunction);

            this.selectedAggregationFunction = aggregationParsed.name;
            this.selectedAggregationArgument = aggregationParsed.value;
        }
    }

    getHarmonicsRange(range: string, delimiter: string | undefined = ':'): any[] {
        const [start, end] = range.split(delimiter).map((num) => Number(num));
        const rangeArray = this.createArray(start, end);
        return rangeArray.map((number) => {
            return { description: number };
        });
    }

    onComponentsChange() {
        this.resetDependentSelections();
        this.updateGroupOptions();
        this.updatePhaseOptions();

        if (this.parameter.group) {
            const groupModel = this.groupOptions.find((option) => option.groupName === this.parameter.group);
            if (!groupModel || groupModel.disabled) {
                this.parameter.group = null;
                this.selectedGroup = null;
            }
        }
    }

    onGroupChange(groupEvent: any) {
        this.selectedPhases = [];
        this.selectedBases = [];
        this.selectedHarmonics = [];
        this.selectedQuantities = [];
        this.baseOptions = [];
        this.quantityOptionsList = [];

        if (groupEvent.value) {
            this.parameter.group = groupEvent.value;
            this.selectedGroup = this.groupOptions.find((g) => g.groupName === groupEvent.value);
        } else {
            this.parameter.group = null;
            this.selectedGroup = null;
        }

        if (this.isHarmonicsGroupSelected()) {
            if (this.dynamicSelection) {
                this.updateHarmonicsOptionsForDynamic();
            } else {
                this.harmonicOptions = [];
                this.harmonicOptions = this.getHarmonicsRange(this.selectedGroup.range);
            }
        }

        this.updatePhaseOptions();
    }

    onPhaseChange(phases: any) {
        if (this.dynamicSelection) {
            this.baseOptions = [];
            if (phases) {
                const uniqueBaseOptions = new Map();
                phases.value.forEach((phaseValue: any) => {
                    const matchingPhases = this.componentPhaseArrays.flat().find((p) => p.phaseName === phaseValue);
                    if (matchingPhases?.bases?.length) {
                        matchingPhases.bases.forEach((b) => {
                            if (!uniqueBaseOptions.has(b.base)) {
                                uniqueBaseOptions.set(b.base, b);
                            }
                        });
                    }
                });
                this.baseOptions = Array.from(uniqueBaseOptions.values());
            }
            this.updateBaseOptions();
        } else {
            this.baseOptions = [];
            const selectedPhasesArr = ArrayUtils.ensureArray(phases.value);

            let baseOptions = [];
            selectedPhasesArr.forEach((phaseValue: string) => {
                const phaseObj = this.phaseOptions.find((p) => p.phaseName === phaseValue);
                baseOptions.push(...(phaseObj?.children ?? []));
            });
            this.baseOptions = orderBy(uniqBy(baseOptions, 'value'), 'description', 'asc').map((base) => {
                return {
                    phaseName: base.value,
                    description: base.description,
                    children: base.children || [],
                };
            });
        }
    }

    isHarmonicsGroupSelected(): boolean {
        return this.selectedGroup?.isHarmonic || this.selectedGroup?.range;
    }

    onBaseChange(event: ListboxChangeEvent) {
        this.minResolution = this._defaultMinResolution;

        const bases = ArrayUtils.ensureArray(event.value);
        let resolutionStates: ResolutionState[] = [];
        for (let base of bases) {
            let baseModel = new BaseState();
            if (this._baseParser.tryParse(base, baseModel)) {
                let resolutionState: ResolutionState = this._resolutionService.parseStateFromBaseState(baseModel);
                resolutionStates.push(resolutionState);
            }
        }
        let maxResolution: ResolutionState = this._resolutionService.findMaxResolution(resolutionStates);
        if (
            this.minResolution &&
            maxResolution &&
            this._resolutionService.lessThan(this.minResolution, maxResolution)
        ) {
            this.minResolution = maxResolution;
        }

        if (this.dynamicSelection) {
            this.updateQuantityOptions();
        } else {
            const selectedBaseArr = ArrayUtils.ensureArray(event.value);
            const uniqueQuantities = new Map<string, any>();

            const selectedPhasesArr = Array.isArray(this.selectedPhases) ? this.selectedPhases : [this.selectedPhases];

            selectedPhasesArr.forEach((phaseValue) => {
                const phaseObj = this.phaseOptions.find((ph) => ph.phaseName === phaseValue);
                if (!phaseObj?.children) return;

                phaseObj.children.forEach((baseItem: any) => {
                    if (selectedBaseArr.includes(baseItem.base || baseItem.value)) {
                        baseItem.children?.forEach((q: any) => {
                            if (!uniqueQuantities.has(q.value)) {
                                uniqueQuantities.set(q.value, {
                                    quantity: q.value,
                                    description: q.value,
                                });
                            }
                        });
                    }
                });
            });

            this.quantityOptionsList = Array.from(uniqueQuantities.values());
        }
    }

    onResolutionChange() {
        if (this.isAggregationSelectionDisabled) {
            this.clearAggregationFunction();
        }
    }

    onOperatorChange(event: any) {
        const value = event.value;
        this.selectedOperator = value;
        if (value === 'ABSOLUTE' || value === 'NONE') {
            this.selectedOperatorArgument = null;
        } else if (value === 'DIVIDE' && this.selectedOperatorArgument === 0) {
            this.selectedOperatorArgument = null;
        }
    }

    onAggregationChange() {
        if (this.selectedAggregationFunction !== 'PERCENTILE') {
            this.selectedAggregationArgument = null;
        }
    }

    onHarmonicsRangeParamsChange(range: any) {
        this.selectedHarmonics = range;
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

        if (this.isHarmonicsGroupSelected() && !this.selectedHarmonics) {
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

        if (!this.resolutionState) {
            isValid = false;
        }

        if (!this.selectedAggregationFunction && !this.isAggregationSelectionDisabled) {
            isValid = false;
        }

        return isValid;
    }

    add() {
        if (this.isEdit) {
            this.editSave();
            return;
        }

        this.emitComponentParameters();
    }

    reset() {
        this.pqsForm?.reset();
        this.setDefaultFormState();
    }

    private editSave() {

        if (
            ArrayUtils.ensureArray(this.selectedPhases).length > 1 ||
            ArrayUtils.ensureArray(this.selectedBases).length > 1 ||
            ArrayUtils.ensureArray(this.selectedQuantities).length > 1 ||
            ArrayUtils.ensureArray(this.selectedHarmonics).length > 1 ||
            ArrayUtils.ensureArray(this.resolutionState.toString()).length > 1 ||
            ArrayUtils.ensureArray(this.combineOperatorAndArgument()).length > 1 ||
            ArrayUtils.ensureArray(this.combineAggregationAndArgument()).length > 1
        ) {
            this.onEditDelete.emit(this.editObjectId);
            this.emitComponentParameters();
            this.finishEdit();
            return;
        }

        this.finishEdit();

        let event: EditBaseParameterEventCallBack = {
            id: this.editObjectId,
            parameter: JSON.parse(JSON.stringify(this.parameter)),
            componentsState: this.disableComponentSelection ? null : this.componentsState,
            quantity: null,
        };

        this._parameterCombinationsService
            .combineParameters(
                event.parameter,
                event.parameter.group,
                ArrayUtils.ensureArray(this.selectedPhases),
                ArrayUtils.ensureArray(this.selectedBases),
                ArrayUtils.ensureArray(this.selectedQuantities),
                ArrayUtils.ensureArray(this.selectedHarmonics),
                ArrayUtils.ensureArray(this.resolutionState.calculateTotalSeconds()),
                ArrayUtils.ensureArray(this.combineOperatorAndArgument()),
                ArrayUtils.ensureArray(this.combineAggregationAndArgument()),
            )
            .subscribe((result) => {
                if (result) {
                    event.parameter = result;
                    event.quantity = QuantityUnits[result.quantity];
                    this.onEditSave.emit(event);
                }
            });
    }

    private emitComponentParameters() {
        if (this.pqsForm.form.valid) {
            if (
                this.selectedOperator &&
                ['DIV', 'MULT'].includes(this.selectedOperator) &&
                (!this.selectedOperatorArgument ||
                    (this.selectedOperator === 'DIV' && this.selectedOperatorArgument === 0))
            ) {
                return;
            }

            const processParameter = (event: AddBaseParameterEventCallBack) => {
                this._parameterCombinationsService
                .combineParameters(
                    event.parameter,
                    event.parameter.group,
                    phases,
                    bases,
                    quantities,
                    harmonics,
                    resolutions,
                    operator,
                    aggregationFunctions,
                )
                .subscribe((result) => {
                    if (result) {
                        event.parameter = result;
                        event.quantity = QuantityUnits[result.quantity];
                        event.parameter.resolution =  resolutions[0];
                        this.onAdd.emit(event);
                        this.resetDependentSelections();
                    }
                });
            }

            const phases = ArrayUtils.ensureArray(this.selectedPhases);
            const bases = ArrayUtils.ensureArray(this.selectedBases);
            const quantities = ArrayUtils.ensureArray(this.selectedQuantities);
            const harmonics = ArrayUtils.ensureArray(this.selectedHarmonics);
            const resolutions = ArrayUtils.ensureArray(this.resolutionState.calculateTotalSeconds());
            const operator = ArrayUtils.ensureArray(this.combineOperatorAndArgument());
            const aggregationFunctions = ArrayUtils.ensureArray(this.combineAggregationAndArgument());

            let parameter = JSON.parse(JSON.stringify(this.parameter));
            parameter.resolution = resolutions;
            parameter.type = this.baseParameterType.toUpperCase();
            parameter.operator = this.combineOperatorAndArgument();

            let event = {
                parameter: parameter,
                componentsState: this.componentsState ? new ComponentsState(this.componentsState) : null,
                quantity: null,
            };

            if (this.dynamicSelection) {
                if (this.baseParameterType === BaseParameterType.Logical) {
                    this.componentsState.feeders?.forEach(feeder => {
                        event = JSON.parse(safeStringify(event));
                        event.parameter.fromComponents = { componentId: feeder.componentId, feederId: feeder.id};
                        processParameter(event);
                    })
                } else if (this.baseParameterType === BaseParameterType.Channel) {
                    this.componentsState.components.forEach(component => {
                        event = JSON.parse(safeStringify(event));
                        event.parameter.fromComponents = { componentId: component.key };
                        processParameter(event);
                    });
                }
            } else {
                processParameter(event);
            }

            this.reset();
        }
    }

    private setDefaultFormState() {
        this.resolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });
        this.minResolution = this._defaultMinResolution;
    }

    private combineOperatorAndArgument(): string | null {
        if (this.selectedOperator) {
            const combined =
                this.selectedOperatorArgument !== null
                    ? `${this.selectedOperator}(${this.selectedOperatorArgument})`
                    : this.selectedOperator;
            return combined;
        }
        return null;
    }

    private combineAggregationAndArgument(): string | null {
        if (this.selectedAggregationFunction) {
            const combined =
                this.selectedAggregationArgument !== null
                    ? `${this.selectedAggregationFunction}(${this.selectedAggregationArgument})`
                    : this.selectedAggregationFunction;
            return combined;
        }
        return '';
    }

    private clearAggregationFunction() {
        this.selectedAggregationFunction = null;
        this.selectedAggregationArgument = null;
    }

    private createArray(start: number, end: number): number[] {
        return Array.from({ length: end - start + 1 }, (_, i) => start + i);
    }

    private updateGroupOptions() {
        this.groupOptions = [];
        let componentGroupsArray = [];

        this.componentsState?.components?.forEach((component) => {
            if (!this.trees[component.key]) {
                this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildTree(
                    this.baseParameterType,
                    component.key,
                    component.parameterInfos,
                )[component.key];
            }

            if (this.baseParameterType === BaseParameterType.Channel) {
                const groups = this.trees[component.key]?.groups ?? [];

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
            } else {
                if (component.data?.feeders?.length) {
                    component.data.feeders.forEach((feeder) => {
                        const parentTree = this.trees[component.key]?.feeders || [];
                        const feederData = parentTree.find((f) => f.feederId == feeder.id);

                        const groups = feederData?.groups ?? [];
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
                }
            }
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
        if (this.dynamicSelection) {
            this.componentsState?.components?.forEach((component) => {
                if (!this.trees[component.key]) {
                    this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildTree(
                        this.baseParameterType,
                        component.key,
                        component.parameterInfos,
                    )[component.key];
                }

                let phases = [];

                if (this.baseParameterType === BaseParameterType.Channel) {
                    phases =
                        this.trees[component.key]?.groups.find((group) => group.groupName === this.parameter.group)
                            ?.phases ?? [];
                } else {
                    component.data?.feeders?.forEach((feeder) => {
                        const feederData = this.trees[component.key]?.feeders?.find(
                            (f) => f.feederId === feeder.id.toString(),
                        );
                        const groupData = feederData.groups.find((group) => group.groupName === this.parameter.group);
                        phases.push(...(groupData?.phases ?? []));
                    });
                }

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
        } else {
            if (!this.selectedGroup?.children) {
                this.phaseOptions = [];
                return;
            }
            this.phaseOptions = orderBy(
                this.selectedGroup.children.map((phase) => {
                    return {
                        description: phase.description,
                        phaseName: phase.value,
                        children: phase.children || [],
                    };
                }),
                'description',
                'asc',
            );
        }

        this.phaseOptions = uniqBy(this.phaseOptions, 'phaseName');
        this.phaseOptions = orderBy(this.phaseOptions, 'description', 'asc');

        for (let phase of this.phaseOptions) {
            if (this.componentPhaseArrays.some((arr) => !arr.some((item) => item.phaseName === phase.phaseName))) {
                phase.disabled = true;
            }
        }
    }

    private updateHarmonicsOptionsForDynamic() {
        this.harmonicOptions = [];
        const componentHarmonicsArrays = [];

        this.componentsState?.components?.forEach((component) => {
            if (!this.trees[component.key]) {
                this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildTree(
                    this.baseParameterType,
                    component.key,
                    component.parameterInfos,
                )[component.key];
            }

            let harmonics = [];

            if (this.baseParameterType === BaseParameterType.Channel) {
                harmonics =
                    this.trees[component.key]?.groups.find((group) => group.groupName === this.parameter.group)
                        ?.harmonics ?? [];
            } else {
                component.data?.feeders?.forEach((feeder) => {
                    const feederData = this.trees[component.key]?.feeders?.find(
                        (f) => f.feederId === feeder.id.toString(),
                    );
                    const groupData = feederData.groups.find((group) => group.groupName === this.parameter.group);

                    harmonics.push(...(groupData?.harmonics ?? []));
                });
            }

            this.harmonicOptions.push(...harmonics.map((harmonic) => ({ description: harmonic })));

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

    private UpdateBaseOptionsForDublicateOrEdit() {
        this.baseOptions = [];
        const selectedPhasesArr = ArrayUtils.ensureArray(this.selectedPhases);

        let baseOptions = [];
        selectedPhasesArr.forEach((phaseValue: string) => {
            const phaseObj = this.phaseOptions.find((p) => p.phaseName === phaseValue);
            baseOptions.push(...(phaseObj?.children ?? []));
        });
        this.baseOptions = orderBy(uniqBy(baseOptions, 'value'), 'description', 'asc').map((base) => {
            return {
                phaseName: base.value,
                description: base.description,
                children: base.children || [],
            };
        });
    }

    private updateQuantityOptions() {
        this.quantityOptionsList = [];
        const componentQuantityArrays = [];
        this.componentBaseArrays.forEach((arr) => {
            const bases = Array.isArray(this.selectedBases) ? this.selectedBases : [this.selectedBases];
            bases.forEach((b) => {
                const quantities = arr.find((bArr) => bArr.phaseName === b).quantities;

                this.quantityOptionsList.push(
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

        this.quantityOptionsList = uniqBy(this.quantityOptionsList, 'quantity');
        this.quantityOptionsList = orderBy(this.quantityOptionsList, 'description', 'asc');
        for (let quantity of this.quantityOptionsList) {
            if (componentQuantityArrays.some((arr) => !arr.some((item) => item.quantity === quantity.quantity))) {
                quantity.disabled = true;
            }
        }
    }

    private parseOperator(operator: string): { name: string; value: number | null } | null {
        if (/^[A-Z]+$/i.test(operator)) {
            return { name: operator.toUpperCase(), value: null };
        }

        const match = operator.match(/^([A-Z]+)\(([-+]?[0-9]*\.?[0-9]+)\)$/i);

        if (match) {
            return {
                name: match[1].toUpperCase(),
                value: parseFloat(match[2]),
            };
        }
        return null;
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
