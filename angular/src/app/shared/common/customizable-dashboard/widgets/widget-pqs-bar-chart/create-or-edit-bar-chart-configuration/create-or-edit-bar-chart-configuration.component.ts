import { Component, Output, EventEmitter, Injector, OnInit, ViewChild } from '@angular/core';
import { WidgetConfigurationModalBaseComponent } from '../../widget-configuration-modal-base';
import {
    BarChartType,
    BarChartWidgetConfigurationsServiceProxy,
    ComponentDto,
    CreateOrEditBarChartWidgetConfigurationDto,
    CreateOrEditWidgetConfigurationDto,
    CustomParametersServiceProxy,
    EventClassDescription,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import safeStringify from 'fast-safe-stringify';
import { NgForm } from '@angular/forms';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ComponentsState } from '@app/shared/models/components-state';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DxDataGridComponent, DxScrollViewComponent, DxTabPanelComponent } from '@node_modules/devextreme-angular';
import { AdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';
import { LogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import { AddCustomParameterEventCallBack, CustomParameterSelectionTabComponent, EditCustomParameterEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { ChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import { AddEventParameterEventCallBack, EditEventParameterEventCallBack, EventParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/event-parameter-selection-tab/event-parameter-selection-tab.component';
import { Guid } from 'guid-ts';
import { AddBaseParameterEventCallBack, EditBaseParameterEventCallBack } from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ColumnType } from '@app/shared/enums/column-type';
import { EditExceptionEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';
import { DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';

@Component({
    selector: 'createOrEditBarChartConfiguration',
    templateUrl: './create-or-edit-bar-chart-configuration.component.html',
    styleUrl: './create-or-edit-bar-chart-configuration.component.css',
})
export class CreateOrEditBarChartConfigurationComponent
    extends WidgetConfigurationModalBaseComponent
    implements OnInit {
    @ViewChild('barChartWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab')    customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab')   logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab')   channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab') additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab')     eventParameterSelectionTab: EventParameterSelectionTabComponent;
    @ViewChild('tabPanel')                       tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave = new EventEmitter<CreateOrEditBarChartWidgetConfigurationDto>();

    saving = false;
    expandTags = true;

    barChartWidgetConfiguration: CreateOrEditBarChartWidgetConfigurationDto =
        new CreateOrEditBarChartWidgetConfigurationDto();

    componentsState: ComponentsState

    parameters: WidgetParametersColumn[] = [];

    tabs = [
        { ID: 1, name: 'Custom', template: 'customTemplate'  },
        { ID: 2, name: 'Logical', template: 'logicalTemplate' },
        { ID: 3, name: 'Channel', template: 'channelTemplate' },
        { ID: 4, name: 'Additional',template: 'additionalTemplate'} ,
        { ID: 5, name: 'Event', template: 'eventTemplate' },
    ];

    xUnitOptions = [
        { label: 'Components', value: 'components' },
        { label: 'Parameters', value: 'parameters' },
        { label: 'Time intervals', value: 'time' },
        { label: 'MyVals (Groups)', value: 'groups' },
        { label: 'Phases', value: 'phases' },
        { label: 'Base', value: 'base' },
    ];

    get chartTypeOptions() {
        return [
            { label: this.l('Plain'), value: 1, disabled: this.totalSeriesCount !== 1 },
            { label: this.l('Stacked'), value: 2, disabled: this.selectedSeries.length < 2 },
            { label: this.l('Clustered'), value: 3, disabled: this.selectedSeries.length < 2 },
        ];
    }

    showComponentSelector = true;
    showParameterTabs = true;

    seriesOptions = [];
    selectedXUnit: string = null;
    selectedSeries: string[] = [];
    isAutoResolution = false;
    resolutionInSeconds = 0;

    dateRangeSelectionState: DateRangeState = new DateRangeState({
        rangeOption: DateRangeUnits.LAST_7_DAYS,
        startDate: null,
        endDate: null,
    });
    constructor(
        injector: Injector,
        private _barChartConfigurationService: BarChartWidgetConfigurationsServiceProxy,
        private _customParameterServiceProxy: CustomParametersServiceProxy,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
    ) {
        super(injector);
    }

    get totalSeriesCount(): number {
        return this.parameters.length + this.selectedSeries.length;
    }

    ngOnInit(): void {
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

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe(parameter => {
            const newItem = {
                id: Guid.newGuid().toString(),
                name: parameter.customParameter.name,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                advancedSettings: event.advancedSettings,
                resolution: 0
            };
            this.parameters = [newItem, ...this.parameters];
        });
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        const newItem = {
            id: Guid.newGuid().toString(),
            name: event.parameter.name,
            quantity: event.quantity,
            type: ColumnType.BaseParameter,
            data: safeStringify(event.parameter),
            advancedSettings: event.advancedSettings,
            resolution: 0
        };

        this.parameters = [...this.parameters, newItem];
        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        setTimeout(() => {
            this.scrollDown();
        }, 100);
    }

    // onAddException(event: AddExceptionEventCallBack) {
    //     this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
    //         const newItem = {
    //             id: Guid.newGuid().toString(),
    //             name: parameter.customParameter.name,
    //             quantity: event.quantity,
    //             type: ColumnType.Exception,
    //             data: event.customParameterId,
    //         };

    //         this.parameters = [newItem, ...this.parameters];
    //     });
    // }

    onAddEvent(event: AddEventParameterEventCallBack) {
        const phaseNames = event.phases.map((phase) => phase).join(', ');
        const formattedName = `${event.event.description} (${phaseNames}) ${event.parameter}`;

        const newItem = {
            id: Guid.newGuid().toString(),
            name: formattedName,
            quantity: event.quantity,
            type: ColumnType.Event,
            resolution:0,
            data: safeStringify({
                event: event.event,
                phases: event.phases,
                parameter: event.parameter,
                isPolyphase: event.polyphase,
                aggregationInSeconds: event.aggregation.aggregationValue,
            }),
            advancedSettings: event.advancedSettings
        };

        this.parameters = [...this.parameters, newItem];

        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        setTimeout(() => {
            this.scrollDown();
        }, 100);
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.advancedSettings = event.advancedSettings;
        });
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

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

        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
        });
    }

    onEditEvent(event: EditEventParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);
        if (!tableParameter) return;

        const phaseNames = event.phases.map((phase) => phase).join(', ');
        tableParameter.name = `${event.event.description} (${phaseNames}) ${event.parameter}`;
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify({
            event: event.event,
            phases: event.phases,
            parameter: event.parameter,
            isPolyphase: event.polyphase,
            aggregationInSeconds: event.aggregation.aggregationValue,

        });
        tableParameter.advancedSettings = event.advancedSettings;
    }

    isFormValid(): boolean {
        const formValid = this.pqsForm?.form?.valid ?? false;
        const barCount  = this.totalSeriesCount;

        // you must have at least one bar
        if (!formValid || barCount < 1) {
            return false;
        }

        switch (this.barChartWidgetConfiguration.type) {
            case BarChartType.Plain:
            return barCount === 1;

            case BarChartType.Stacked:
            case BarChartType.Clustered:
            return barCount >= 2;

            default:
            return false;
        }
    }

    changeExpandState() {
        this.expandTags = !this.expandTags;
    }

    onDateRangeChanged(dateRangeState: DateRangeState) {
        this.barChartWidgetConfiguration.dateRange = dateRangeState.toJSON();
    }

    onComponentsStateChange() {
        this.componentsState = { ...this.componentsState };
    }

    save() {
        this.saving = true;
        this.barChartWidgetConfiguration.dateRange    = this.dateRangeSelectionState.toJSON();
        this.barChartWidgetConfiguration.components   = safeStringify(this.componentsState);

        const params = this.parameters.map(p => ({
            ...p,
            resolution: p.resolution ?? 0,
        }));
        const config = {
            xUnit: this.selectedXUnit,
            series: this.selectedSeries,
            isAutoResolution: this.isAutoResolution,
            resolutionInSeconds: this.resolutionInSeconds,
            parameters: params,
            componentsState: this.componentsState,
            dateRangeSelectionState: this.dateRangeSelectionState,
        };
        this.barChartWidgetConfiguration.configuration = safeStringify(config);
        console.debug('Saving BarChartWidgetConfiguration', this.barChartWidgetConfiguration);
        if (this.barChartWidgetConfiguration.id) {
            this._barChartConfigurationService
                .createOrEdit(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        } else {
            this._barChartConfigurationService
                .createAndGetId(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    console.debug('createAndGetId result', result);
                    this.barChartWidgetConfiguration.id = result;
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto) {
        this.open();
        if (configuration && configuration.configuration) {
            this._barChartConfigurationService
                .getBarChartWidgetConfigurationForEdit(+configuration.configuration)
                .subscribe(result => {
                    this.barChartWidgetConfiguration = result.barChartWidgetConfiguration;
                    let loadedSeries: string[] = [];

try {
                        const cfg = JSON.parse(this.barChartWidgetConfiguration.configuration);
                        console.debug('Loaded BarChartWidgetConfiguration', cfg);

                        this.selectedXUnit = cfg.xUnit;
                        this.selectedSeries = cfg.series ?? [];
                        loadedSeries = cfg.series ?? [];

                        this.isAutoResolution = cfg.isAutoResolution ?? false;
                        this.resolutionInSeconds = cfg.resolutionInSeconds ?? 0;
                        this.parameters = (cfg.parameters ?? []).map((p: any) => ({
                            ...p,
                            resolution: p.resolution ?? 0,
                        }));
                        this.componentsState = cfg.componentsState;
                        this.dateRangeSelectionState = DateRangeState.fromJSON(cfg.dateRangeSelectionState);
                    } catch {
                        this.dateRangeSelectionState = DateRangeState.fromJSON(this.barChartWidgetConfiguration.dateRange);
                        this.componentsState = JSON.parse(this.barChartWidgetConfiguration.components);
                        this.parameters = JSON.parse(this.barChartWidgetConfiguration.configuration).map((p: any) => ({
                            ...p,
                            resolution: p.resolution ?? 0,
                        }));
                        const comps = JSON.parse(this.barChartWidgetConfiguration.components) as any[];
                        const params = this.parameters;

                        if (this.barChartWidgetConfiguration.dateRange && params.length === 0) {
                            this.selectedXUnit = 'time';
                            loadedSeries = comps.length > 1 ? ['components'] : ['parameters'];                        } else if (comps.length > 1 && params.length === 1) {
                            this.selectedXUnit = 'components';
                            loadedSeries = ['parameters'];
                        } else if (params.length > 1 && comps.length === 1) {
                            this.selectedXUnit = 'parameters';
                            loadedSeries = ['components'];
                        }                    }

                    this.onXUnitChange(this.selectedXUnit);
                    this.selectedSeries = loadedSeries;
                    this.onSeriesChange(this.selectedSeries);
                });
        } else {
            this.barChartWidgetConfiguration = new CreateOrEditBarChartWidgetConfigurationDto();
            this.dateRangeSelectionState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null });
            this.componentsState = null;
            this.parameters = [];
            this.selectedXUnit = null;
            this.selectedSeries = [];
            this.isAutoResolution = false;
            this.resolutionInSeconds = 0;
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

    onXUnitChange(x: string) {
        this.selectedXUnit       = x;
        this.selectedSeries      = [];
        this.showComponentSelector = x !== 'components';
        this.showParameterTabs    = x !== 'parameters';

        if (x === 'groups') {
            this._pqsRestApiServiceProxy.measurementsGroups().subscribe(gs => {
            this.seriesOptions = gs
                .filter(g => g.groupId != null)
                .map(g => ({ label: g.description || `${g.groupId}`, value: `${g.groupId}` }));
            });
        } else if (x === 'phases') {
            this._pqsRestApiServiceProxy.measurementsPhases().subscribe(ps => {
            this.seriesOptions = ps
                .filter(p => p.phaseName != null)
                .map(p => ({ label: p.description || p.phaseName, value: p.phaseName }));
            });
        } else if (x === 'base') {
            this._pqsRestApiServiceProxy.measurementsBases().subscribe(bs => {
            this.seriesOptions = bs
                .filter(b => b.base != null)
                .map(b => ({ label: b.description || `${b.base}`, value: `${b.base}` }));
            });
        } else {
            this.seriesOptions = this.xUnitOptions.filter(u => u.value !== x);
        }
    }

    swapXUnitSeries() {
        if (this.selectedSeries.length !== 1) {
            return;
        }

        const prevX       = this.selectedXUnit;
        const prevSeries  = this.selectedSeries[0];

        this.selectedXUnit = prevSeries;
        this.onXUnitChange(prevSeries);

        setTimeout(() => {
            this.selectedSeries = [prevX];
            this.onSeriesChange(this.selectedSeries);
        }, 0);
    }

    onSeriesChange(series: string[]) {
        this.selectedSeries = (series || []).filter(v => v != null);
        this.showComponentSelector = this.selectedXUnit !== 'components'
        || this.selectedSeries.includes('components');

        // show the parameter/event tabs if either:
        //  our X isn't parameters
        //  OR we've explicitly picked "parameters" in series
        this.showParameterTabs =
            this.selectedXUnit !== 'parameters'
        || this.selectedSeries.includes('parameters');
    }

    onRowRemoved() {
        this.parameters = [...this.parameters];
    }

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

            // case ColumnType.Exception:
            //     this.tabPanel.selectedIndex = 4;
            //     populateOrEdit(this.exceptionParameterSelectionTab);
            //     break;

            case ColumnType.Event:
                this.tabPanel.selectedIndex = 5;
                populateOrEdit(this.eventParameterSelectionTab);
                break;
        }
    }

    private scrollDown() {
        this.scrollView.instance.scrollTo(10000);
    }
}
