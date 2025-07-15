import { Component, ViewChild, Injector, Output, EventEmitter, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { finalize } from 'rxjs/operators';
import { CustomParametersServiceProxy, CreateOrEditCustomParameterDto } from '@shared/service-proxies/service-proxies';
import { cloneDeep as _cloneDeep, sortBy as _sortBy, range as _range, findIndex } from 'lodash-es';
import { Parameter } from './table-parameters/models/parameter';
import { ArithmeticsModalComponent } from './modals/arithmetics-modal/arithmetics-modal.component';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { ResolutionService } from '@app/shared/services/resolution-service';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { NgForm } from '@angular/forms';
import safestringify from '@node_modules/fast-safe-stringify';
import {
    AddCustomParameterToCustomParameterEventCallback,
    CpCustomParameterSelectionTabComponent,
    EditCustomParameterEventCallBack,
} from './cp-custom-parameter-selection-tab/cp-custom-parameter-selection-tab.component';
import { InnerCustomParameter } from './table-parameters/models/InnerCustomParameter';
import { DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { DxTabPanelComponent } from '@node_modules/devextreme-angular';
import { CpBaseParameterSelectionTabComponent } from './cp-base-parameter-selection-tab/cp-base-parameter-selection-tab.component';
import { CpAdditionalParameterSelectionTabComponent } from './cp-additional-parameter-selection-tab/cp-additional-parameter-selection-tab.component';

@Component({
    selector: 'createOrEditCustomParameterModal',
    templateUrl: './create-or-edit-customParameter-modal.component.html',
    styleUrl: './create-or-edit-customParameter-modal.component.css',
    providers: [],
    encapsulation: ViewEncapsulation.None,
})
export class CreateOrEditCustomParameterModalComponent extends AppComponentBase {
    //#region props
    @ViewChild('arithmeticsModal', { static: true }) arithmeticsModal: ArithmeticsModalComponent;
    @ViewChild('customParameterForm', { static: true }) form: NgForm;
    @ViewChild('cpCustomParameterSelectionTab') cpCustomParameterSelectionTab: CpCustomParameterSelectionTabComponent;
    @ViewChild('cpLogicalParameterSelectionTab') cpLogicalParameterSelectionTab: CpBaseParameterSelectionTabComponent;
    @ViewChild('cpChannelParameterSelectionTab') cpChannelParameterSelectionTab: CpBaseParameterSelectionTabComponent;
    @ViewChild('cpAdditionalParameterSelectionTab') cpAdditionalParameterSelectionTab: CpAdditionalParameterSelectionTabComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;

    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();

    active = false;
    saving = false;
    isPopupVisible = false;

    customParameter: CreateOrEditCustomParameterDto = new CreateOrEditCustomParameterDto();

    parameters!: GridDataItem[];
    baseParameters: Parameter[] | undefined;
    innerCustomParameters: InnerCustomParameter[] | undefined;

    quantityOptions = ['AVG', 'MIN', 'MAX' /*'QSAMPLE'*/];
    aggregationFunctionOptions = _sortBy(
        [
            { name: 'AVG' },
            { name: 'MAX' },
            { name: 'MIN' },
            { name: 'PERCENTILE' },
            //{ name: 'ABSOLUTE' },
            { name: 'COUNT' },
            { name: 'ARITHMETICS' },
            { name: 'SUM' },
            { name: 'MULT' },
            { name: 'RMS' },
            // { name: 'DIVIDE' },
            // { name: 'EXP' },
            // { name: 'LN' },
            // { name: 'LOG' },
            // { name: 'POWER' },
            // { name: 'SQRT' },
        ],
        ['name'],
    );

    minAllowedResolution: ResolutionState;

    selectedResolution: ResolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });

    selectedType: any;
    typeOptions = [
        { name: 'Multi Parameters to Single Feeder/Component', value: 'MPSC' },
        { name: 'Single Parameter to Multi Feeders/Components', value: 'SPMC' },
        { name: 'Specific parameters from specific components', value: 'EXCEPTION' },
        // { name: 'Base Parameter', value: 'BPCP' },
    ];
    selectedAggregationFunction!: string;
    selectedAggregationFunctionArgument?: string;
    selectedAggregationArgument_InNumber: number;

    baseParameterTypes = BaseParameterType;

    isEditForSPMC: boolean = false;

    private readonly _defaultMinAllowedResolution: ResolutionState = new ResolutionState({
        resolutionUnit: ResolutionUnits.CUSTOM,
        customResolutionValue: 3,
        customResolutionUnit: CustomResolutionUnits.MIN,
    });

    private _exceptionTabs = [
        {
            ID: 2,
            name: 'Logical',
            template: 'logicalTemplate',
        },
        {
            ID: 3,
            name: 'Channel',
            template: 'channelTemplate',
        },
        {
            ID: 4,
            name: 'Additional',
            template: 'additionalTemplate',
        },
    ];

    private _tabs = [
        {
            ID: 1,
            name: 'Custom',
            template: 'customTemplate',
        },
        ...this._exceptionTabs,
    ];

    private _customParameterType = 'Custom Parameter';
    //#endregion

    //#region Constructor
    constructor(
        injector: Injector,
        private _customParametersServiceProxy: CustomParametersServiceProxy,
        private _resolutionService: ResolutionService,
    ) {
        super(injector);
        this.minAllowedResolution = this._defaultMinAllowedResolution;
    }

    //#region Getters / Setters

    get combinedAggregationFunctionValues(): string | null {
        return `${this.selectedAggregationFunction || ''}(${this.selectedAggregationFunctionArgument || ''})`;
        // if (['PERCENTILE', 'ARITHMETICS'].includes(this.selectedAggregationFunction)) {
        //     return `${this.selectedAggregationFunction || ''}(${this.selectedAggregationFunctionArgument || ''})`;
        // } else {
        //     return this.selectedAggregationFunction;
        // }
    }

    get aggregationFunctions() {
        return this.customParameter.type === 'MPSC'
            ? this.aggregationFunctionOptions
            : this.aggregationFunctionOptions.filter((func) => func.name !== 'ARITHMETICS');
    }

    get isParameterSelectionEnabled(): boolean {
        let result = true;
        if (!this.customParameter.type || (this.customParameter.type === 'SPMC' && this.parameters?.length >= 1)) {
            result = false;
            if (this.isEditForSPMC) {
                result = true;
            }
        }

        return result;
    }

    get isComponentsSelectionEnabled() {
        return this.customParameter.type === 'EXCEPTION';
    }

    get tabs() {
        if (this.customParameter.type === 'EXCEPTION') {
            return this._exceptionTabs;
        } else {
            return this._tabs;
        }
    }

    set combinedAggregationFunctionValues(value: string | null) {
        if (value) {
            const values = this.splitString(value);
            if (values.length === 2) {
                this.selectedAggregationFunction = values[0];
                this.selectedAggregationFunctionArgument = values[1];
                this.selectedAggregationArgument_InNumber = +this.selectedAggregationFunctionArgument;
            } else {
                this.selectedAggregationFunction = values[0] || null;
            }
        }
    }

    getIconClass(tabID: number): string {
        const icons = {
            1: 'fa-cogs',
            2: 'fa-brain',
            3: 'fa-signal',
            4: 'fa-plus',
        };

        return icons[tabID] || 'fa-question-circle';
    }

    splitString(str: string): string[] {
        const match = str.match(/^(.*?)\((.*)\)$/);
        return match ? [match[1], match[2]] : [str];
    }
    //#endregion

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        let parametersWithSameName = this.parameters.filter(
            (parameterInArr) => parameterInArr.name.split('_')[0] === event.parameter.name,
        );

        if (parametersWithSameName.length > 0) {
            event.parameter.name = event.parameter.name + `_${parametersWithSameName.length + 1}`;
        }

        event.parameter.quantity = event.quantity;
        
        this.parameters.push({
            id: event.parameter.name,
            name: event.parameter.name,
            quantity: event.quantity,
            type: event.parameter.type,
            data: safestringify(event.parameter),
            resolution: this._resolutionService.parseStateFromInt(event.parameter.resolution).toString(),
            operator: event.parameter.operator,
            innerAggregation: event.parameter.aggregationFunction,
        });

        //  this._resolutionService
        //     .calculateTotalSeconds(this._resolutionService.parseStateFromInt(event.parameter.resolution));

        // event.parameter.resolution = this._resolutionService
        //     .formatForRequest(this._resolutionService.parseStateFromInt(event.parameter.resolution))
        //     .toString();

        this.baseParameters.push(event.parameter);
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        if (!event) {
            this.isEditForSPMC = false;
        }

        const tableParameter: GridDataItem = this.parameters.find((p) => p.id === event?.id);
        tableParameter.name = event.parameter.name;
        tableParameter.quantity = event.quantity;
        tableParameter.type = event.parameter.type;
        tableParameter.resolution = this._resolutionService.parseStateFromInt(event.parameter.resolution).toString();
        tableParameter.operator = event.parameter.operator;
        tableParameter.innerAggregation = event.parameter.aggregationFunction;
        tableParameter.data = safestringify(event.parameter);

        this.isEditForSPMC = false;
    }

    onAddCustomParameter(event: AddCustomParameterToCustomParameterEventCallback) {
        this._customParametersServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((result) => {
            const resolutionUIRepresentation = this._resolutionService.getUIRepresentationForResolutionState(
                this._resolutionService.parseStateFromInt(result.customParameter.resolutionInSeconds),
            );
            const parameterMapped: GridDataItem = {
                id: event.customParameterId.toString(),
                name: result.customParameter.name + ' (' + resolutionUIRepresentation + ')',
                quantity: event.quantity,
                type: this._customParameterType,
                resolution: event.resolution.toString(),
                operator: event.operator,
                innerAggregation: event.aggregationFunction,
                data: safestringify(result.customParameter),
            };
            this.parameters.unshift(parameterMapped);
            this.updateMinResolution();
            this.innerCustomParameters.push({
                customParameterId: event.customParameterId,
                quantity: event.quantity,
                Resolution: event.resolution.calculateTotalSeconds(),
                // Resolution: this._resolutionService.calculateTotalSeconds(event.resolution),
                // quantityResolution: this._resolutionService.formatForRequest(event.resolution).toString(),
                innerAggregationFunction: event.aggregationFunction,
                operator: event.operator,
            });
        });
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        if (!event) {
            this.isEditForSPMC = false;
        }

        this._customParametersServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((result) => {
            const resolutionUIRepresentation = this._resolutionService.getUIRepresentationForResolutionState(
                this._resolutionService.parseStateFromInt(result.customParameter.resolutionInSeconds),
            );
            const parameterMapped: GridDataItem = {
                id: event.customParameterId.toString(),
                name: result.customParameter.name + ' (' + resolutionUIRepresentation + ')',
                quantity: event.quantity,
                type: this._customParameterType,
                resolution: event.resolution.toString(),
                operator: event.operator,
                innerAggregation: event.aggregationFunction,
                data: safestringify(result.customParameter),
            };
            this.updateMinResolution();

            const tableParameter: GridDataItem = this.parameters.find((p) => p.id === event?.id);
            tableParameter.id = parameterMapped.id;
            tableParameter.name = parameterMapped.name;
            tableParameter.type = parameterMapped.type;
            tableParameter.quantity = parameterMapped.quantity;
            tableParameter.data = parameterMapped.data;
            tableParameter.resolution = parameterMapped.resolution;
            tableParameter.operator = parameterMapped.operator;
            tableParameter.innerAggregation = parameterMapped.innerAggregation;
        });

        this.isEditForSPMC = false;
    }

    onEditDeleteObject(event: string) {
        const index = this.parameters.findIndex((p) => p.id === event);
        if (index !== -1) {
            this.parameters.splice(index, 1);
        }
    }

    updateResolutionModel() {
        // this.customParameter.resolution = this._resolutionService.formatForRequest(this.selectedResolution).toString();
        this.customParameter.resolutionInSeconds = this._resolutionService.resolutionValueInSeconds(this.selectedResolution);
    }

    updateMinResolution() {
        let resolutions: ResolutionState[] = this.parameters?.map((parameter) =>
            this._resolutionService.parseStateFromString(parameter.resolution),
        );
        let calculatedMinResolution = this._resolutionService.findMaxResolution(resolutions ?? []);
        this.minAllowedResolution =
            this._resolutionService.resolutionValueInMs(calculatedMinResolution) <
            this._resolutionService.resolutionValueInMs(this._defaultMinAllowedResolution)
                ? this._defaultMinAllowedResolution
                : calculatedMinResolution;
    }

    updateAggregationFunctionModel() {
        if (!['ARITHMETICS', 'SUM', 'MULT', 'RMS'].includes(this.selectedAggregationFunction)) {
            this.selectedAggregationFunctionArgument = '';
        }
        this.customParameter.aggregationFunction = this.combinedAggregationFunctionValues;
    }

    updateAggregationArgumentValue(newValue: number) {
        this.selectedAggregationFunctionArgument = this.selectedAggregationArgument_InNumber.toString();
        this.customParameter.aggregationFunction = this.combinedAggregationFunctionValues;
    }
    onFocus(){

    }

    deleteAllParameters() {
        if (this.parameters?.length > 0) {
            this.customParameter.type = this.selectedType;
            this.parameters = [];
            this.baseParameters = [];
            this.innerCustomParameters = [];
            this.updateMinResolution();
            // this.message.confirm(
            //     'Changing Custom Parameter Type will delete all existing parameters.',
            //     this.l('AreYouSure'),
            //     (isConfirmed) => {
            //         if (isConfirmed) {
            //             this.customParameter.type = this.selectedType;
            //             this.parameters = [];
            //             this.baseParameters = [];
            //             this.customParameterIds = [];
            //             this.updateMinResolution();
            //         } else {
            //             this.selectedType = this.customParameter.type;
            //         }
            //     },
            // );
        } else {
            this.customParameter.type = this.selectedType;
        }
    }

    onParameterDelete(event) {
        if (event.data.type === this._customParameterType) {
            const index = findIndex(this.innerCustomParameters, (cp) => cp.customParameterId === +event.data.id);
            this.innerCustomParameters.splice(index, 1);
        } else {
            const index = this.baseParameters.indexOf(event.data.id);
            this.baseParameters.splice(index, 1);
        }
    }

    /** modal */
    show(customParameterId?: number): void {
        this.parameters = [];
        this.baseParameters = [];
        this.innerCustomParameters = [];
        if (!customParameterId) {
            this.customParameter = new CreateOrEditCustomParameterDto();
            this.customParameter.id = customParameterId;
            this.selectedType = null;
            this.updateMinResolution();

            this.active = true;
            this.isPopupVisible = true;
        } else {
            this._customParametersServiceProxy.getCustomParameterForEdit(customParameterId).subscribe((result) => {
                this.customParameter = result.customParameter;

                this.baseParameters = JSON.parse(this.customParameter.customBaseDataList);
                this._resolutionService.parseStateFromString("",true)
                this.baseParameters?.forEach((bp) => {
                    this.parameters.push({
                        id: bp.id?.toString() ?? '',
                        name: bp.name,
                        type: bp.type,
                        quantity: bp.quantity,
                        data: safestringify(bp),
                        resolution: this._resolutionService.parseStateFromInt(bp.resolution, true).toString(),
                        operator: bp.operator,
                        innerAggregation: bp.aggregationFunction,
                    });
                });

                this.innerCustomParameters = JSON.parse(this.customParameter.innerCustomParameters);
                this.innerCustomParameters?.forEach((icp) => {
                    this._customParametersServiceProxy
                        .getCustomParameterForView(icp.customParameterId)
                        .subscribe((result) => {
                            this.parameters.unshift({
                                id: result.customParameter.id.toString(),
                                name:
                                    result.customParameter.name +
                                    ' (' +
                                    this._resolutionService.getUIRepresentationForResolutionState(
                                        this._resolutionService.parseStateFromInt(result.customParameter.resolutionInSeconds, true),
                                    ) +
                                    ')',
                                type: this._customParameterType,
                                quantity: icp.quantity,
                                data: safestringify(result.customParameter),
                                resolution: this._resolutionService.parseStateFromInt(icp.Resolution, true).toString(),

                                // resolution: this._resolutionService
                                //     .formatFromRequest(
                                //         this._resolutionService.parseStateFromInt(icp.Resolution),
                                //         // this._resolutionService.parseStateFromString(icp.quantityResolution, true),
                                //     )
                                //     .toString(),
                                innerAggregation: icp.innerAggregationFunction,
                                operator: icp.operator,
                            });
                        });
                });

                this.updateMinResolution();
                this.combinedAggregationFunctionValues = this.customParameter.aggregationFunction;
                this.selectedType = this.customParameter.type;
                this.selectedResolution = this._resolutionService.formatFromRequest(
                    this._resolutionService.parseStateFromInt(this.customParameter.resolutionInSeconds, true),
                );
                this.active = true;
                this.isPopupVisible = true;
            });
        }
    }

    isFormValid(): boolean {
        let isValid = true;

        if(!this.customParameter.name){
            isValid = false;
        }
        if(!this.selectedType){
            isValid = false;
        }
        if(!this.selectedResolution.customResolutionValue){
            isValid = false;
        }
        if(!this.selectedResolution.customResolutionUnit){
            isValid = false;
        }
        if(!this.selectedAggregationFunction){
            isValid = false;
        }
        if(this.selectedAggregationFunction === 'PERCENTILE'){
            if(!this.selectedAggregationArgument_InNumber){
                isValid = false;
            }
        }
        if(['ARITHMETICS'].includes(this.selectedAggregationFunction)){
            if(!this.selectedAggregationFunctionArgument){
                isValid = false;
            }
        }
        if(this.parameters?.length < 1){
            isValid = false;
        }
        return isValid;
    }

    save(): void {
        this.saving = true;

        if (!this.validate()) {
            this.saving = false;
            return;
        }

        this.customParameter.aggregationFunction = this.combinedAggregationFunctionValues;
        this.customParameter.innerCustomParameters = safestringify(this.innerCustomParameters);
        this.customParameter.customBaseDataList = safestringify(this.baseParameters);

        this._customParametersServiceProxy
            .createOrEdit(this.customParameter)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.showMessage(this.l('SavedSuccessfully'), 'success');
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active = false;
        this.isPopupVisible = false;
    }

    showArithmeticsModal() {
        this.arithmeticsModal.show();
    }

    onArithmeticsSelected(value: string) {
        this.selectedAggregationFunctionArgument = value;
    }

    updateParameter(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.handleParameter(event.data, 'edit');
    }

    duplicateParameterCommand = (e: DxDataGridTypes.ColumnButtonClickEvent) => {
        const parameter = e.row.data as GridDataItem;
        this.handleParameter(parameter, 'duplicate');
    };

    onTabSelectionChanging(e: any) {
        if(this.isEditForSPMC){
            e.cancel = true;
        }
    }

    private handleParameter(data: GridDataItem, action: 'edit' | 'duplicate') {
        const populateOrEdit = (tab: any) => {
            if (action === 'edit') {
                if (this.customParameter.type === 'SPMC') {
                    this.isEditForSPMC = true;
                }

                tab.edit(data);
            } else {
                if (tab.isEdit) {
                    tab.isEdit = false;
                }

                tab.populateForm(data);
            }
        };

        switch (data.type) {
            case 'Custom Parameter':
                this.tabPanel.selectedIndex = 0;
                populateOrEdit(this.cpCustomParameterSelectionTab);
                break;

            case 'LOGICAL':
                this.tabPanel.selectedIndex = 1 - (this.selectedType === 'EXCEPTION' ? 1 : 0);
                populateOrEdit(this.cpLogicalParameterSelectionTab);
                break;
            case 'CHANNEL':
                this.tabPanel.selectedIndex = 2 - (this.selectedType === 'EXCEPTION' ? 1 : 0);
                populateOrEdit(this.cpChannelParameterSelectionTab);
                break;
            case 'Additional':
                this.tabPanel.selectedIndex = 3 - (this.selectedType === 'EXCEPTION' ? 1 : 0);
                populateOrEdit(this.cpAdditionalParameterSelectionTab);
                break;
        }
    }

    private validate(): boolean {
        let result = true;

        let incorrectParameters: string[] = [];
        for (let parameter of this.parameters) {
            if (
                !parameter.innerAggregation &&
                this._resolutionService.greaterThan(
                    this.selectedResolution,
                    this._resolutionService.parseStateFromString(parameter.resolution),
                )
            ) {
                incorrectParameters.push(parameter.name);
                result = false;
            }
        }

        if (incorrectParameters.length > 0) {
            this.showMessage(
                `No selected inner aggregation function for ${incorrectParameters.join(', ')}, but required`,
                'error',
                6000,
            );
        }

        return result;
    }
}

export interface GridDataItem {
    id: string;
    name: string;
    type: string;
    quantity: string;
    data: string;
    resolution: string;
    operator?: string;
    innerAggregation?: string;
}
