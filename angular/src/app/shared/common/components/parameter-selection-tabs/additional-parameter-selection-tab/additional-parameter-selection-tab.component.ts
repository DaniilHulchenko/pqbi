import { Component, EventEmitter, Injector, Input, Output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ListboxModule } from 'primeng/listbox';
import { DxButtonModule, DxScrollViewModule } from 'devextreme-angular';
import { ComponentsSelectorComponent } from '../../components-selector/components-selector.component';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { ComponentsState } from '@app/shared/models/components-state';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ParameterCombinationsService } from '@app/shared/services/parameter-combinations-service';
import { BaseParameterCreationTreeBuilder } from '@app/shared/services/base-parameter-creation-tree-builder';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import {
    FeederComponentInfo,
    QuantityDataInfo,
    QuantityEnum,
} from '@shared/service-proxies/service-proxies';
import { orderBy, uniqBy } from 'lodash-es';
import { EditableTabComponentBaseComponent } from '../editable-tab-component-base';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import safeStringify from 'fast-safe-stringify';
import { PopulatableForm } from '../populatable-form';
import { UtilsModule } from '../../../../../../shared/utils/utils.module';
import { AdvancedSettingsComponent, AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';
import { FormContainerComponent } from '../../form-container/form-container.component';
import { NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';

@Component({
    selector: 'additional-parameter-selection-tab',
    standalone: true,
    imports: [
        AdvancedSettingsComponent,
        CommonModule,
        DxButtonModule,
        DxScrollViewModule,
        ComponentsSelectorComponent,
        ListboxModule,
        FormsModule,
        UtilsModule,
        FormContainerComponent,
    ],
    templateUrl: './additional-parameter-selection-tab.component.html',
    styleUrl: './additional-parameter-selection-tab.component.css',
})
export class AdditionalParameterSelectionTabComponent
    extends EditableTabComponentBaseComponent
    implements PopulatableForm<WidgetParametersColumn>
{
    @Input() disableComponentSelection = false;
    @Output() onAdd: EventEmitter<AddBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditSave: EventEmitter<EditBaseParameterEventCallBack> = new EventEmitter();
    @Output() onEditDelete: EventEmitter<string> = new EventEmitter();
    @Input() isInsideTable = false;
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('advancedSettingsModal') advancedSettingsModal: AdvancedSettingsComponent;

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

    advancedSettingsConfig: AdvancedSettingsConfig;

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

    private trees = {};

    constructor(
        injector: Injector,
        private _parameterCombinationsService: ParameterCombinationsService,
        private _baseParameterCreationTreeBuilder: BaseParameterCreationTreeBuilder,
    ) {
        super(injector);
    }

    @Input() set outerComponentsState(state: ComponentsState) {
        if (this.disableComponentSelection && state) {
            this.componentsState = state;
            this.onComponentsChange();
        }
    }

    edit(parameter: WidgetParametersColumn) {
        super.startEdit(parameter.id);
        this.populateForm(parameter);

        this.advancedSettingsConfig = parameter.advancedSettings ?? {
            normalizeValue: NormalizeEnum.NO,
            normalizeNominalValue: 100,
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
            customAggregationFunc: '',
        };
    }

    populateForm(parameter: WidgetParametersColumn): void {
        if (!this.disableComponentSelection) {
            this.componentsState = JSON.parse(safeStringify(parameter.componentsState));
            this.onComponentsChange();
        }

        this.parameter = JSON.parse(parameter.data.toString());
        this.selectedAdditional = this.additionalOptions.find((opt) => opt.propertiesName === this.parameter.group);

        this.updateBaseOptions();
        this.selectedBases = this.isEdit ? this.parameter.baseResolution : [this.parameter.baseResolution];
        this.updateQuantityOptions();
        this.selectedQuantities = this.isEdit ? this.parameter.quantity : [this.parameter.quantity];
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

    onAdvancedSettingsChanged(config: AdvancedSettingsConfig): void {
        this.advancedSettingsConfig = config;
    }

    onAdditionalChange(name: string) {
        this.resetDependentSelections();
        this.parameter.group = name;
        this.parameter.name = this.additionalOptions.find((opt) => opt.groupName === name).description;
        this.selectedAdditional = this.componentsState.components
            .flatMap((c) => c.additionalDatas || [])
            .find((ad) => ad.propertiesName === name);
        this.updateBaseOptions();
    }

    onBaseChange() {
        this.selectedQuantities = [];
        this.updateQuantityOptions();
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

    showAdvancedSettingsModal() {
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
            parameter: JSON.parse(safeStringify(this.parameter)),
            componentsState: this.disableComponentSelection ? null : this.componentsState,
            quantity: null,
            advancedSettings: this.advancedSettingsConfig,
        };

        var combinations = this._parameterCombinationsService.combineAdditionalParameters(
            event.parameter,
            ArrayUtils.ensureArray(this.selectedBases),
            ArrayUtils.ensureArray(this.selectedQuantities),
        );

        for (let parameter of combinations) {
            event.parameter = parameter;
            event.quantity = QuantityUnits[parameter.quantity];
            event.advancedSettings = this.advancedSettingsConfig 
                                        ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                        : undefined,
            this.onEditSave.emit(event);
        }

        this.finishEdit();
    }

    private emitComponentParameters() {
        if (!this.disableComponentSelection) {
            for (let component of this.componentsState.components) {
                const eventComponentState = new ComponentsState({
                    components: [component],
                    tags: null,
                    pickListState: this.componentsState.pickListState,
                    feeders: [new FeederComponentInfo({
                                            id: undefined,
                                            name: undefined,
                                            componentId: component.key.toString(),
                                            compName: component.label,
                                          })]
                });

                let event: AddBaseParameterEventCallBack = {
                    parameter: JSON.parse(JSON.stringify(this.parameter)),
                    componentsState: eventComponentState,
                    quantity: null,
                    advancedSettings: this.advancedSettingsConfig 
                                    ? JSON.parse(JSON.stringify(this.advancedSettingsConfig)) 
                                    : undefined,
                };

                var combinations = this._parameterCombinationsService.combineAdditionalParameters(
                    event.parameter,
                    ArrayUtils.ensureArray(this.selectedBases),
                    ArrayUtils.ensureArray(this.selectedQuantities),
                );

                for (let parameter of combinations) {
                    event.parameter = parameter;
                    event.quantity = QuantityUnits[parameter.quantity];
                    this.onAdd.emit(event);
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

            var combinations = this._parameterCombinationsService.combineAdditionalParameters(
                event.parameter,
                ArrayUtils.ensureArray(this.selectedBases),
                ArrayUtils.ensureArray(this.selectedQuantities),
            );

            for (let parameter of combinations) {
                event.parameter = parameter;
                event.quantity = QuantityUnits[parameter.quantity];
                this.onAdd.emit(event);
            }
        }
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

    private resetDependentSelections(): void {
        this.parameter.group = null;
        this.selectedBases = [];
        this.selectedQuantities = [];
        this.selectedAdditional = null;
    }
}
