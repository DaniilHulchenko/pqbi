import { Component, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from '@angular/core';
import { EditableTabComponentBaseComponent } from '@app/shared/common/components/parameter-selection-tabs/editable-tab-component-base';
import { ComponentsState } from '@app/shared/models/components-state';
import { AdditionalData, PQSRestApiServiceProxy, QuantityDataInfo, QuantityEnum } from '@shared/service-proxies/service-proxies';
import { orderBy, sortBy, uniqBy } from 'lodash-es';
import { Parameter } from '../table-parameters/models/parameter';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { NgForm } from '@node_modules/@angular/forms';
import { AddBaseParameterEventCallBack, EditBaseParameterEventCallBack } from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ParameterCombinationsService } from '@app/shared/services/parameter-combinations-service';
import { GridDataItem } from '../create-or-edit-customParameter-modal.component';
import safeStringify from 'fast-safe-stringify';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { BaseParserService } from '@app/shared/services/base-parser.service';
import { BaseParameterCreationTreeBuilder } from '@app/shared/services/base-parameter-creation-tree-builder';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { BaseState } from '@app/shared/models/base-state';

@Component({
    selector: 'cpAdditionalParameterSelectionTab',
    templateUrl: './cp-additional-parameter-selection-tab.component.html',
    styleUrl: './cp-additional-parameter-selection-tab.component.css',
})
export class CpAdditionalParameterSelectionTabComponent extends EditableTabComponentBaseComponent implements OnInit {
    @Input() disableComponentSelection = false;
    @Output() onAdd: EventEmitter<AddBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @Input() isInsideTable = false;
    @ViewChild('pqsForm') pqsForm: NgForm;

    _multiple = false;

    parameter: Parameter = {
        type: BaseParameterType.Additional,
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

    selectedAdditional: any;

    selectedBases: string[] | string = [];
    selectedQuantities: string[] | string = [];

    additionalOptions: any[] = [];
    baseOptions: any[] = [];
    quantityOptions: any[] = [];

    componentBaseArrays: any[][];

    additionalError = '';
    baseError = '';
    quantityError = '';

    selectedOperatorArgument: number | null = null;
    selectedOperator: string | null;

    selectedAggregationFunction: string;
    selectedAggregationArgument: number;

    resolutionState: ResolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });
    minResolution: ResolutionState;
    resolutionTooltipText = {
        unit: 'The unit part of the required resolution of the base parameter',
        argument: 'The value part of the required resolution of the base parameter',
    };
    private _outerResolutionState: ResolutionState;

    private readonly _operatorOptions = sortBy([{ name: 'ABSOLUTE' }, { name: 'DIVIDE' }, { name: 'MULT' }], ['name']);
    private readonly _aggregationFuncOptions = sortBy(
        [{ name: 'AVG' }, { name: 'MAX' }, { name: 'MIN' }, { name: 'COUNT' }, { name: 'PERCENTILE' }],
        ['name'],
    );
    private readonly _quantityOptions = [QuantityUnits.MAX, QuantityUnits.MIN, QuantityUnits.AVG];

    private readonly labels: Record<QuantityEnum, string> = {
        [QuantityEnum.QMIN]: 'Minimum',
        [QuantityEnum.QMAX]: 'Maximum',
        [QuantityEnum.QAVG]: 'Average',
    };

    private readonly quantityKeys: Record<QuantityEnum, string> = {
        [QuantityEnum.QMIN]: 'MIN',
        [QuantityEnum.QMAX]: 'MAX',
        [QuantityEnum.QAVG]: 'AVG',
    };

    private readonly _defaultMinResolution = new ResolutionState({
        resolutionUnit: ResolutionUnits.CUSTOM,
        customResolutionValue: 1,
        customResolutionUnit: CustomResolutionUnits.MS,
    });

    private trees = {};

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

    ngOnInit(): void {
        this.minResolution = this._defaultMinResolution;

        if (this.disableComponentSelection) {
            this._pqsRestApiServiceProxy.getStaticData().subscribe((response) => {
                const additionalDatas = response.additionalDatas;
                var tree = this._baseParameterCreationTreeBuilder.buildAdditionalTree(additionalDatas);
                this.additionalOptions = tree.groups;
                this.additionalOptions = uniqBy(this.additionalOptions, 'groupName');
                this.additionalOptions = orderBy(this.additionalOptions, 'description', 'asc');
            });
        }
        setTimeout(() => this.setDefaultFormState(), 100);
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

    edit(parameter: GridDataItem) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    populateForm(parameter: GridDataItem): void {
        this.parameter = JSON.parse(parameter.data.toString());

        this.selectedAdditional = this.additionalOptions.find((option) => option.groupName === this.parameter.group);

        this.updateBaseOptions();

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

        const aggregationParsed = this.parseOperator(this.parameter.aggregationFunction);

        this.selectedAggregationFunction = aggregationParsed.name;
        this.selectedAggregationArgument = aggregationParsed.value;
    }

    populateComponentsFromTab(tab: any) {
        this.componentsState = JSON.parse(safeStringify(tab.componentsState));
        this.onComponentsChange();
    }

    onComponentsChange() {
        this.resetDependentSelections();
        this.updateAdditionalOptions();

        if (this.parameter.group) {
            const found = this.additionalOptions.find((option) => option.propertiesName === this.parameter.group);
            if (!found) {
                this.parameter.group = null;
                this.selectedAdditional = null;
            }
        }
    }

    onResolutionChange() {
        if (this.isAggregationSelectionDisabled) {
            this.clearAggregationFunction();
        }
    }

    onAdditionalChange(name: string) {
        this.resetDependentSelections();
        this.parameter.group = name;
        this.parameter.name = this.additionalOptions.find((opt) => opt.groupName === name).description;
        this.selectedAdditional = this.additionalOptions.find((ad) => ad.groupName === name);
        this.updateBaseOptions();
    }

    onBaseChange() {
        this.selectedQuantities = [];
        this.updateQuantityOptions();

        this.minResolution = this._defaultMinResolution;

        const bases = ArrayUtils.ensureArray(this.selectedBases);
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

    isFormValid(): boolean {
        let isValid = true;

        if (!this.parameter.group) {
            this.additionalError = 'Select value';
            isValid = false;
        } else {
            this.additionalError = '';
        }

        if (!this.selectedBases?.length) {
            this.baseError = 'Select value';
            isValid = false;
        } else {
            this.baseError = '';
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
        this.parameter = {
            type: BaseParameterType.Additional,
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
        this.selectedAdditional = null;
        this.selectedBases = [];
        this.selectedQuantities = [];
        if (!this.disableComponentSelection) {
            this.componentsState = null;
        }
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
            parameter: JSON.parse(safeStringify(this.parameter)),
            componentsState: this.disableComponentSelection ? null : this.componentsState,
            quantity: null,
        };

        var combinations = this._parameterCombinationsService.combineAdditionalParameters(
            event.parameter,
            ArrayUtils.ensureArray(this.selectedBases),
            ArrayUtils.ensureArray(this.selectedQuantities),
        );

        for (let parameter of combinations) {
            event.parameter = parameter;
            event.quantity = QuantityUnits[parameter.quantity];
            this.onEditSave.emit(event);
        }

        this.finishEdit();
    }

    private emitComponentParameters() {
        let parameter = JSON.parse(JSON.stringify(this.parameter));
        parameter.resolution = this.resolutionState.calculateTotalSeconds();
        parameter.operator = this.combineOperatorAndArgument();
        parameter.aggregationFunction = this.combineAggregationAndArgument()

        const processParameter = (event: AddBaseParameterEventCallBack) => {
            var result = this._parameterCombinationsService.combineAdditionalParameters(
                event.parameter,
                ArrayUtils.ensureArray(this.selectedBases),
                ArrayUtils.ensureArray(this.selectedQuantities),
            );

            for (let item of result) {
                event.parameter = item;
                event.quantity = QuantityUnits[item.quantity];
                this.onAdd.emit(event);
                this.resetDependentSelections();
            }
        }

        let event = {
            parameter: parameter,
            componentsState: this.componentsState ? new ComponentsState(this.componentsState) : null,
            quantity: null,
        };

        if (!this.disableComponentSelection) {
            this.componentsState.components.forEach((component) => {
                event = JSON.parse(safeStringify(event));
                event.parameter.fromComponents = { componentId: component.key };
                processParameter(event);
            });
        } else {
            processParameter(event);
        }

        this.reset();
    }

    private updateAdditionalOptions() {
        this.additionalOptions = [];
        let componentGroupsArray = [];

        this.componentsState?.components?.forEach((component) => {
            if (!this.trees[component.key]) {
                this.trees[component.key] = this._baseParameterCreationTreeBuilder.buildAdditionalTree(
                    component.additionalDatas,
                );
            }

            this.additionalOptions.push(...this.trees[component.key].groups);
            componentGroupsArray.unshift(this.trees[component.key]);
        });
        this.additionalOptions = uniqBy(this.additionalOptions, 'groupName');
        this.additionalOptions = orderBy(this.additionalOptions, 'description', 'asc');
        for (let group of this.additionalOptions) {
            if (componentGroupsArray.some((arr) => !arr.groups.some((item) => item.groupName === group.groupName))) {
                group.disabled = true;
            } else {
                group.disabled = false;
            }
        }
    }

    private updateBaseOptions() {
        this.baseOptions = [];

        if (!this.disableComponentSelection) {
            this.componentBaseArrays = [];
            this.componentsState?.components?.forEach((component) => {
                const bases = this.trees[component.key].groups.find(
                    (group) => group.groupName === this.parameter.group,
                ).bases;

                this.baseOptions.push(...bases);
                this.componentBaseArrays.unshift(bases);
            });

            this.baseOptions = uniqBy(this.baseOptions, 'base');
            this.baseOptions = orderBy(this.baseOptions, 'description', 'asc');
            for (let base of this.baseOptions) {
                if (this.componentBaseArrays.some((arr) => !arr.some((item) => item.base === base.base))) {
                    base.disabled = true;
                }
            }
        } else {
            this.baseOptions = this.selectedAdditional.bases;
        }
    }

    private updateQuantityOptions() {
        this.quantityOptions = [];
        if (!this.parameter.group) {
            return;
        }

        const choices: QuantityEnum[] = [QuantityEnum.QMIN, QuantityEnum.QMAX, QuantityEnum.QAVG];

        this.quantityOptions = choices.map(
            (qe) =>
                new QuantityDataInfo({
                    quantity: qe,
                    phaseName: this.quantityKeys[qe],
                    description: this.labels[qe],
                }),
        );
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

    private setDefaultFormState() {
        this.resolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });
        this.minResolution = this._defaultMinResolution;
    }

    private clearAggregationFunction() {
        this.selectedAggregationFunction = null;
        this.selectedAggregationArgument = null;
    }

    private resetDependentSelections(): void {
        this.parameter.group = null;
        this.selectedBases = [];
        this.selectedQuantities = [];
        this.selectedAdditional = null;
    }
}
