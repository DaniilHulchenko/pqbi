import { Component, EventEmitter, Injector, Input, OnInit, Output, ViewChild } from '@angular/core';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { NgForm } from '@node_modules/@angular/forms';
import { ListboxChangeEvent } from '@node_modules/primeng/listbox';
import {
    CustomParameterDto,
    CustomParametersServiceProxy,
    PagedResultDtoOfGetCustomParameterForViewDto,
} from '@shared/service-proxies/service-proxies';
import { sortBy } from 'lodash-es';
import { GridDataItem } from '../create-or-edit-customParameter-modal.component';
import { EditableTabComponentBaseComponent } from '@app/shared/common/components/parameter-selection-tabs/editable-tab-component-base';

@Component({
    selector: 'cpCustomParameterSelectionTab',
    templateUrl: './cp-custom-parameter-selection-tab.component.html',
    styleUrl: './cp-custom-parameter-selection-tab.component.css',
})
export class CpCustomParameterSelectionTabComponent extends EditableTabComponentBaseComponent implements OnInit {
    @Output() onAdd: EventEmitter<AddCustomParameterToCustomParameterEventCallback> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditCustomParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @ViewChild('pqsForm') pqsForm: NgForm;

    selectedCustomParameter: number;
    customParameters!: CustomParameterDto[];

    selectedQuantities: string | string[] = [];

    selectedAggregationFunction: string;
    selectedAggregationArgument: number;

    resolutionState: ResolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });

    selectedOperatorArgument: number | null = null;
    selectedOperator: string | null;
    advancedSettingsConfig: any = {};

    _multiple = false;

    resolutionTooltip = {
        unit: 'The unit part of the required resolution of the custom parameter',
        argument: 'The value part of the required resolution of the custom parameter',
    };

    minResolution: ResolutionState;

    customParameterError = '';
    quantityError = '';

    readonly operatorOptions = sortBy([{ name: 'ABSOLUTE' }, { name: 'DIVIDE' }, { name: 'MULT' }], ['name']);
    readonly aggregationFuncOptions = sortBy(
        [{ name: 'AVG' }, { name: 'MAX' }, { name: 'MIN' }, { name: 'COUNT' }, { name: 'PERCENTILE' }],
        ['name'],
    );
    readonly quantityOptions = [QuantityUnits.MAX, QuantityUnits.MIN, QuantityUnits.AVG];

    private _outerResolutionState: ResolutionState;

    private readonly _customParameterTypes: string[] = ['SPMC', 'MPSC'];

    private readonly _defaultMinResolution = new ResolutionState({
        resolutionUnit: ResolutionUnits.CUSTOM,
        customResolutionValue: 1,
        customResolutionUnit: CustomResolutionUnits.MS,
    });

    constructor(
        injector: Injector,
        private _customParameterServiceProxy: CustomParametersServiceProxy,
        private _resolutionService: ResolutionService,
    ) {
        super(injector);
    }

    get isAggregationSelectionDisabled() {
        return this._resolutionService.greaterOrEqualThan(this.resolutionState, this._outerResolutionState);
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
        this.customParameters = [];
        for (let type of this._customParameterTypes) {
            this._customParameterServiceProxy
                .getAll(undefined, undefined, undefined, type, undefined, undefined, undefined, undefined, 0, 100)
                .subscribe((result: PagedResultDtoOfGetCustomParameterForViewDto) => {
                    this.customParameters.push(...result.items.map((item) => item.customParameter));
                    this.customParameters = [...this.customParameters];
                });
        }
    }

    populateForm(parameter: GridDataItem): void {
        this.selectedCustomParameter = Number(parameter.id);
        this.selectedQuantities = this._multiple ? [parameter.quantity] : parameter.quantity;
        this.resolutionState = this._resolutionService.parseStateFromString(parameter.resolution);
        // this.resolutionState = this._resolutionService.parseStateFromInt(parameter.resolution);
        let operatorParsed;
        if (parameter.operator) {
            operatorParsed =
                parameter.operator === 'ABSOLUTE'
                    ? { name: 'ABSOLUTE', value: null }
                    : this.parseOperator(parameter.operator);

            this.selectedOperator = operatorParsed.name;
            this.selectedOperatorArgument = operatorParsed.value;
        }
        const aggregationParsed = this.parseOperator(parameter.innerAggregation);

        this.selectedAggregationFunction = aggregationParsed.name;
        this.selectedAggregationArgument = aggregationParsed.value;
    }

    getCustomParameterUI(customParameter: CustomParameterDto) {
        const resolution = this._resolutionService.getUIRepresentationForResolutionState(
            this._resolutionService.formatFromRequest(
                this._resolutionService.parseStateFromInt(customParameter.resolutionInSeconds, true),
            ),
        );
        return customParameter.name + ' (' + resolution + ')';
    }

    onCustomParameterChange(event: ListboxChangeEvent) {
        const customParameterModel = this.customParameters.find((cp) => cp.id === event.value);
        const customParameterResolution: ResolutionState = this._resolutionService.parseStateFromInt(
            customParameterModel.resolutionInSeconds, true);
        this.minResolution = this._resolutionService.formatFromRequest(customParameterResolution);
    }

    onResolutionChange() {
        if (this.isAggregationSelectionDisabled) {
            this.clearAggregationFunction();
        }
    }

    onOperatorChange(event: any) {
        const value = event.value;
        this.selectedOperator = value;
        if (!value || value === 'ABSOLUTE') {
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

    finishEditing() {
        this.onEditSave.emit();
        this.finishEdit();
    }

    edit(parameter: GridDataItem) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);
    }

    isFormValid(): boolean {
        let isValid = true;

        if (!this.selectedCustomParameter) {
            this.customParameterError = 'Select value';
            isValid = false;
        } else {
            this.customParameterError = '';
        }

        // if(!this.resolutionState){
        //     isValid = false;
        // }

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
        this.minResolution = this._defaultMinResolution;
        this.resolutionState = this.minResolution;
    }

    private editSave() {
        this.finishEdit();

        if (ArrayUtils.ensureArray(this.selectedQuantities).length > 1) {
            this.onEditDelete.emit(this.editObjectId);
            this.emitComponentParameters();
            return;
        }

        let event: EditCustomParameterEventCallBack = {
            id: this.editObjectId,
            customParameterId: this.selectedCustomParameter,
            quantity: ArrayUtils.ensureArray(this.selectedQuantities)[0],
            resolution: this.resolutionState,
            operator: this.combineOperatorAndArgument(),
            aggregationFunction: this.combineAggregationAndArgument(),
            advancedSettings: this.advancedSettingsConfig,
        };

        this.onEditSave.emit(event);
    }

    private emitComponentParameters() {
        if (this.pqsForm.form.valid) {
            let event: AddCustomParameterToCustomParameterEventCallback = {
                customParameterId: this.selectedCustomParameter,
                quantity: '',
                resolution: this.resolutionState,
                operator: this.combineOperatorAndArgument(),
                aggregationFunction: this.combineAggregationAndArgument(),
                advancedSettings: this.advancedSettingsConfig
            };

            ArrayUtils.ensureArray(this.selectedQuantities).forEach((quantity) => {
                event.quantity = quantity;
                this.onAdd.emit(event);
            });

            this.reset();
        }
    }

    private clearAggregationFunction() {
        this.selectedAggregationFunction = null;
        this.selectedAggregationArgument = null;
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
}

export interface AddCustomParameterToCustomParameterEventCallback {
    customParameterId: number;
    quantity: string;
    resolution: ResolutionState;
    operator: string;
    aggregationFunction: string;
    advancedSettings: any; 
}

export interface EditCustomParameterEventCallBack extends AddCustomParameterToCustomParameterEventCallback {
    id: string;
}
