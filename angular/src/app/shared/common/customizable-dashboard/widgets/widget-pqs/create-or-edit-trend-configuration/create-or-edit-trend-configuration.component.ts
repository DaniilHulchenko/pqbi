import {
    Component,
    Injector,
    OnInit,
    Output,
    ViewChild,
    EventEmitter,
    ViewEncapsulation,
    ChangeDetectorRef,
} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTrendWidgetConfigurationDto,
    CreateOrEditWidgetConfigurationDto,
    CustomParametersServiceProxy,
    GetTrendWidgetConfigurationForEditOutput,
    TrendWidgetConfigurationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { NgForm } from '@angular/forms';
import {
    AddExceptionEventCallBack,
    EditExceptionEventCallBack,
    ExceptionParameterSelectionTabComponent,
} from '@app/shared/common/components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';
import {
    AddCustomParameterEventCallBack,
    CustomParameterSelectionTabComponent,
    EditCustomParameterEventCallBack,
} from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ResolutionState } from '@app/shared/models/resolution-state';
import safeStringify from 'fast-safe-stringify';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { finalize } from 'rxjs';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ColumnType } from '@app/shared/enums/column-type';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { DxDataGridComponent, DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { Guid } from 'guid-ts';
import { ComponentsState } from '@app/shared/models/components-state';
import { ChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import { LogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { DxScrollViewComponent, DxTabPanelComponent } from '@node_modules/devextreme-angular';
import { AdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';

@Component({
    selector: 'createOrEditTrendConfiguration',
    templateUrl: './create-or-edit-trend-configuration.component.html',
    styleUrl: './create-or-edit-trend-configuration.component.css',
    encapsulation: ViewEncapsulation.None,
})
export class CreateOrEditTrendConfigurationComponent extends AppComponentBase implements OnInit {
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab') customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab') logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab') channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab') additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    @ViewChild('exceptionParameterSelectionTab')
    exceptionParameterSelectionTab: ExceptionParameterSelectionTabComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave: EventEmitter<CreateOrEditTrendWidgetConfigurationDto> =
        new EventEmitter<CreateOrEditTrendWidgetConfigurationDto>();

    saving = false;
    popupVisible = false;
    configuration: CreateOrEditTrendWidgetConfigurationDto = new CreateOrEditTrendWidgetConfigurationDto();

    dateRangeSelectionState: DateRangeState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null });
    resolutionState: ResolutionState;
    parameters: WidgetParametersColumn[] = [];

    parameterResolutions: ResolutionState[] = [];
    minAllowedResolution: ResolutionState;
    minCustomArgument = 1;
    maxCustomArgument = 99999;

    tabs = [
        {
            ID: 1,
            name: 'Custom',
            template: 'customTemplate',
        },
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
        {
            ID: 5,
            name: 'Exception',
            template: 'exceptionTemplate',
        },
    ];

    selectedTabIndex: number | null = 0;

    constructor(
        injector: Injector,
        private _customParameterService: CustomParametersServiceProxy,
        private _trendWidgetConfigurationService: TrendWidgetConfigurationsServiceProxy,
        private _resolutionService: ResolutionService,
    ) {
        super(injector);
    }

    ngOnInit(): void {}

    componentsToString(componentsState: ComponentsState): string {
        return componentsState?.components?.map((component) => component.label).join(', ') ?? '';
    }

    isFormValid(): boolean {
        const isDateRangeValid =
            this.dateRangeSelectionState?.rangeOption &&
            (this.dateRangeSelectionState.rangeOption !== DateRangeUnits.CUSTOM ||
                (this.dateRangeSelectionState.startDate &&
                    this.dateRangeSelectionState.endDate &&
                    this.dateRangeSelectionState.startDate < this.dateRangeSelectionState.endDate));

        const isResolutionValid =
            this.resolutionState?.resolutionUnit &&
            (this.resolutionState.resolutionUnit !== ResolutionUnits.CUSTOM ||
                (this.resolutionState.customResolutionValue &&
                    this.resolutionState.customResolutionValue >= this.minCustomArgument &&
                    this.resolutionState.customResolutionValue <= this.maxCustomArgument &&
                    this.resolutionState.customResolutionUnit));

        const isTableNotEmpty = this.parameters && this.parameters.length > 0;

        return isDateRangeValid && isResolutionValid && isTableNotEmpty;
    }

    getIconClass(tabID: number): string {
        const icons = {
            1: 'fa-cogs',
            2: 'fa-brain',
            3: 'fa-project-diagram',
            4: 'fa-plus',
            5: 'fa-exclamation-circle',
        };

        return icons[tabID] || 'fa-question-circle';
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        this.parameters.push({
            id: Guid.newGuid().toString(),
            componentsState: event.componentsState,
            name: event.parameter.name,
            quantity: event.quantity,
            type: ColumnType.BaseParameter,
            data: safeStringify(event.parameter),
            advancedSettings: event.advancedSettings,
            resolution:0
        });
        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        this.scrollDown();
    }

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        this._customParameterService.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            this.parameters.unshift({
                id: Guid.newGuid().toString(),
                componentsState: event.componentsState,
                name: parameter.customParameter.name,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                advancedSettings: event.advancedSettings,
                resolution:0
            });
            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.customParameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onAddException(event: AddExceptionEventCallBack) {
        this._customParameterService.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            this.parameters.unshift({
                id: Guid.newGuid().toString(),
                componentsState: null,
                name: parameter.customParameter.name,
                quantity: event.quantity,
                type: ColumnType.Exception,
                data: event.customParameterId,
                resolution:0
            });
            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.customParameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterService.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.componentsState = event.componentsState;
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.advancedSettings = event.advancedSettings;

            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.customParameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        tableParameter.componentsState = event.componentsState;
        tableParameter.name = event.parameter.name;
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify(event.parameter);
        tableParameter.advancedSettings = event.advancedSettings;
    }

    onEditException(event: EditExceptionEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterService.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;

            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.customParameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onEditDeleteObject(event: string) {
        const index = this.parameters.findIndex((p) => p.id === event);
        if (index !== -1) {
            this.parameters.splice(index, 1);
        }
    }

    save(): void {
        this.saving = true;

        this.configuration.dateRange = this.dateRangeSelectionState.toJSON();
        this.configuration.resolution = this.resolutionState.toString();
        this.configuration.parameters = safeStringify(this.parameters);

        if (this.configuration.id) {
            this._trendWidgetConfigurationService
                .createOrEdit(this.configuration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe(() => {
                    this.hide();
                    this.onSave.emit(this.configuration);
                });
        } else {
            this._trendWidgetConfigurationService
                .createAndGetId(this.configuration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((id: number) => {
                    this.configuration.id = id;
                    this.hide();
                    this.onSave.emit(this.configuration);
                });
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto): void {
        this.pqsForm.form.reset();
        this.popupVisible = true;

        if (configuration && configuration.configuration) {
            this._trendWidgetConfigurationService
                .getTrendWidgetConfigurationForEdit(+configuration.configuration)
                .subscribe((configuration: GetTrendWidgetConfigurationForEditOutput) => {
                    this.configuration = configuration.trendWidgetConfiguration;

                    this.resolutionState = this._resolutionService.parseStateFromString(this.configuration.resolution);
                    this.dateRangeSelectionState = DateRangeState.fromJSON(this.configuration.dateRange);
                    this.parameters = JSON.parse(this.configuration.parameters);

                    for (let parameter of this.parameters.filter(
                        (parameter) =>
                            parameter.type === ColumnType.CustomParameter || parameter.type === ColumnType.Exception,
                    )) {
                        this._customParameterService.getCustomParameterForView(+parameter.data).subscribe((cp) => {
                            parameter.name = cp.customParameter.name;
                            this.parameterResolutions.push(
                                this._resolutionService.parseStateFromInt(cp.customParameter.resolutionInSeconds, true),
                            );
                            this.minAllowedResolution = this._resolutionService.findMaxResolution(
                                this.parameterResolutions,
                            );
                        });
                    }

                    setTimeout(() => {
                        this.scrollDown()
                    }, 100)
                });
        } else {
            this.parameters = [];
            this.resolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.AUTO });
            this.minAllowedResolution = this._resolutionService.parseStateFromString(ResolutionUnits.IS1MIN, true);
            this.dateRangeSelectionState = new DateRangeState({
                rangeOption: DateRangeUnits.LAST_30_DAYS,
                startDate: null,
                endDate: null,
            });
            this.parameterResolutions = [];
        }
    }

    hide(): void {
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.exceptionParameterSelectionTab.finishEdit();
        this.popupVisible = false;
    }

    // eslint-disable-next-line @typescript-eslint/member-ordering
    previousTabIndex: number | null = null;

    onTabSelectionChanging(e: any) {
        this.previousTabIndex = e.component.option('selectedIndex');
    }

    onTabSelectionChanged(e: any) {
        const tabMapping: Record<number, any> = {
            0: this.customParameterSelectionTab,
            1: this.logicalParameterSelectionTab,
            2: this.channelParameterSelectionTab
        };
        
        const previousTab = tabMapping[this.previousTabIndex];
        const selectedTab = tabMapping[this.tabPanel.selectedIndex];
        
        if (previousTab && selectedTab && previousTab !== selectedTab) {
            selectedTab.populateComponentsFromTab(previousTab);
        }
    }

    updateParameter(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.handleParameter(event.data, 'edit');
    }

    duplicateParameterCommand = (e: DxDataGridTypes.ColumnButtonClickEvent) => {
        const parameter = e.row.data as WidgetParametersColumn;
        this.handleParameter(parameter, 'duplicate');
    };

    private handleParameter(data: WidgetParametersColumn, action: 'edit' | 'duplicate') {
        const populateOrEdit = (tab: any) => {
            if (action === 'edit') {
                tab.edit(data);
            } else {
                if (tab.isEdit) {
                    tab.isEdit = false;
                }
                tab.populateForm(data);
            }
        };

        switch (data.type) {
            case ColumnType.CustomParameter:
                this.tabPanel.selectedIndex = 0;
                populateOrEdit(this.customParameterSelectionTab);
                break;

            case ColumnType.BaseParameter:
                const parameter: Parameter = JSON.parse(data.data.toString());
                if (parameter?.type) {
                    switch (parameter.type) {
                        case BaseParameterType.Logical:
                            this.tabPanel.selectedIndex = 1;
                            populateOrEdit(this.logicalParameterSelectionTab);
                            break;
                        case BaseParameterType.Channel:
                            this.tabPanel.selectedIndex = 2;
                            populateOrEdit(this.channelParameterSelectionTab);
                            break;
                        case BaseParameterType.Additional:
                            this.tabPanel.selectedIndex = 3;
                            populateOrEdit(this.additionalParameterSelectionTab);
                            break;
                    }
                }
                break;

            case ColumnType.Exception:
                this.tabPanel.selectedIndex = 4;
                populateOrEdit(this.exceptionParameterSelectionTab);
                break;
        }
    }

    private scrollDown(){
        this.scrollView.instance.scrollTo(10000);
    }
}
