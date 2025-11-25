import {
    Component,
    Injector,
    OnInit,
    Output,
    ViewChild,
    EventEmitter,
    ViewEncapsulation,
    OnDestroy,
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
import { finalize, Subscription } from 'rxjs';
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
import { CustomParameterService } from '@app/shared/services/custom-parameter-service.service';
import { TrendWidgetConfigurationService } from '@app/shared/services/widget-configurations/trend-widget-configuration.service';
import { DateRangeAndResolutionModel } from '@app/shared/models/date-range-and-resolution-model';
import { DateRangeAndResolutionSelectorComponent } from '@app/shared/common/components/date-range-and-resolution-selector/date-range-and-resolution-selector.component';

@Component({
    selector: 'createOrEditTrendConfiguration',
    templateUrl: './create-or-edit-trend-configuration.component.html',
    styleUrl: './create-or-edit-trend-configuration.component.css',
    encapsulation: ViewEncapsulation.None,
})
export class CreateOrEditTrendConfigurationComponent extends AppComponentBase implements OnInit, OnDestroy {
    @ViewChild('pqsForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab') customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab') logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab') channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab') additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    @ViewChild('exceptionParameterSelectionTab')
    exceptionParameterSelectionTab: ExceptionParameterSelectionTabComponent;
    @ViewChild('dateRangeResolutionSelector') dateRangeResolutionSelector: DateRangeAndResolutionSelectorComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave: EventEmitter<CreateOrEditTrendWidgetConfigurationDto> =
        new EventEmitter<CreateOrEditTrendWidgetConfigurationDto>();
    @Output() onClose = new EventEmitter<boolean>();

    saving = false;
    popupVisible = false;
    configuration: CreateOrEditTrendWidgetConfigurationDto = new CreateOrEditTrendWidgetConfigurationDto();

    // dateRangeSelectionState: DateRangeState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null });
    // resolutionState: ResolutionState;
    dateRangeAndResolutionSelectionState: DateRangeAndResolutionModel;
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

    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        private _customParameterService: CustomParameterService,
        private _trendWidgetConfigurationService: TrendWidgetConfigurationService,
        private _trendWidgetConfigurationServiceProxy: TrendWidgetConfigurationsServiceProxy,
        private _resolutionService: ResolutionService,
    ) {
        super(injector);
    }

    ngOnInit(): void {}

    componentsToString(componentsState: ComponentsState): string {
        return componentsState?.components?.map((component) => component.label).join(', ') ?? '';
    }

    isFormValid(): boolean {
        const isTableNotEmpty = this.parameters && this.parameters.length > 0;

        return (this.dateRangeResolutionSelector?.isValid() ?? false) && isTableNotEmpty;
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
            resolution:0,
            style: null
        });
        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        this.scrollDown();
    }

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            this.parameters.unshift({
                id: Guid.newGuid().toString(),
                componentsState: event.componentsState,
                name: parameter.name,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                advancedSettings: event.advancedSettings,
                resolution:0,
                style: null
            });
            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onAddException(event: AddExceptionEventCallBack) {
        this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            this.parameters.unshift({
                id: Guid.newGuid().toString(),
                componentsState: null,
                name: parameter.name,
                quantity: event.quantity,
                type: ColumnType.Exception,
                data: event.customParameterId,
                resolution:0,
                style: null
            });
            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.resolutionInSeconds, true),
            );
            this.minAllowedResolution = this._resolutionService.findMaxResolution(this.parameterResolutions);
        });
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.componentsState = event.componentsState;
            tableParameter.name = parameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.advancedSettings = event.advancedSettings;

            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.resolutionInSeconds, true),
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

        this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;

            this.parameterResolutions.push(
                this._resolutionService.parseStateFromInt(parameter.resolutionInSeconds, true),
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

        this.configuration.dateRange = safeStringify(this.dateRangeAndResolutionSelectionState);
        this.configuration.resolution = this.dateRangeAndResolutionSelectionState.resolution.toString();
        this.configuration.parameters = safeStringify(this.parameters);

        if (this.configuration.id) {
            var sub = this._trendWidgetConfigurationServiceProxy
                .createOrEdit(this.configuration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe(() => {
                    this._trendWidgetConfigurationService.update(this.configuration);
                    this.hide(true);
                    this.onSave.emit(this.configuration);
                });
            this.subs.push(sub);
        } else {
            var sub = this._trendWidgetConfigurationServiceProxy
                .createAndGetId(this.configuration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((id: number) => {
                    this.configuration.id = id;
                    this._trendWidgetConfigurationService.update(this.configuration);
                    this.hide(true);
                    this.onSave.emit(this.configuration);
                });
            this.subs.push(sub);
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto): void {
        this.pqsForm.form.reset();
        this.popupVisible = true;

        if (configuration && configuration.configuration) {
            var sub = this._trendWidgetConfigurationService
                .getForEdit(+configuration.configuration)
                .subscribe((configuration: GetTrendWidgetConfigurationForEditOutput) => {
                    this.configuration = configuration.trendWidgetConfiguration;

                    this.dateRangeAndResolutionSelectionState = DateRangeAndResolutionModel.createItem(
                        this.configuration.dateRange,
                        this._resolutionService.parseStateFromString(this.configuration.resolution, true)
                    );

                    this.parameters = JSON.parse(this.configuration.parameters);

                    for (let parameter of this.parameters.filter(
                        (parameter) =>
                            parameter.type === ColumnType.CustomParameter || parameter.type === ColumnType.Exception,
                    )) {
                        var subCP = this._customParameterService.getById(+parameter.data).subscribe((cp) => {
                            parameter.name = cp.name;
                            this.parameterResolutions.push(
                                this._resolutionService.parseStateFromInt(cp.resolutionInSeconds, true),
                            );
                            this.minAllowedResolution = this._resolutionService.findMaxResolution(
                                this.parameterResolutions,
                            );
                        });
                        this.subs.push(subCP);
                    }
                });
            this.subs.push(sub);
        } else {
            this.parameters = [];
            this.dateRangeAndResolutionSelectionState = null;
            this.minAllowedResolution = this._resolutionService.parseStateFromString(ResolutionUnits.IS1MIN, true);
            this.parameterResolutions = [];
        }
    }

    hide(isSaved: boolean): void {
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.exceptionParameterSelectionTab.finishEdit();
        this.popupVisible = false;
        this.onClose.emit(isSaved);
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

    ngOnDestroy(): void {
        this.subs.forEach(sub => sub.unsubscribe());
    }
}
