import { Component, Output, EventEmitter, Injector, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { WidgetConfigurationModalBaseComponent } from '../../widget-configuration-modal-base';
import {
    BarChartType,
    BarChartWidgetConfigurationsServiceProxy,
    CreateOrEditBarChartWidgetConfigurationDto,
    CreateOrEditWidgetConfigurationDto,
    BarChartRequest,
    BarChartResponse,
    BarParameter,
    CustomWidgetTableData,
    DimensionSelector,
    DimensionType,
    FeederComponentInfo,
    TableWidgetEvent,
    TenantDashboardServiceProxy,
    EventClass,
    GroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize, Subscription } from 'rxjs';
import safeStringify from 'fast-safe-stringify';
import { NgForm } from '@angular/forms';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ComponentsState } from '@app/shared/models/components-state';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DxDataGridComponent, DxScrollViewComponent, DxTabPanelComponent } from 'devextreme-angular';
import { AdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';
import { LogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import {
    AddCustomParameterEventCallBack,
    CustomParameterSelectionTabComponent,
    EditCustomParameterEventCallBack,
} from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { ChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import {
    AddEventParameterEventCallBack,
    EditEventParameterEventCallBack,
    EventParameterSelectionTabComponent,
} from '@app/shared/common/components/parameter-selection-tabs/event-parameter-selection-tab/event-parameter-selection-tab.component';
import { Guid } from 'guid-ts';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ColumnType } from '@app/shared/enums/column-type';
import { EditExceptionEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';
import { DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { CustomParameterService } from '@app/shared/services/custom-parameter-service.service';
import { BarchartWidgetConfigurationService } from '@app/shared/services/widget-configurations/barchart-widget-configuration.service';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateRangeType } from '@app/shared/enums/date-range-type';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { DateTime } from 'luxon';


@Component({
    selector: 'createOrEditBarChartConfiguration',
    templateUrl: './create-or-edit-bar-chart-configuration.component.html',
    styleUrl: './create-or-edit-bar-chart-configuration.component.css',
})
export class CreateOrEditBarChartConfigurationComponent
    extends WidgetConfigurationModalBaseComponent
    implements OnInit, OnDestroy
{
    @ViewChild('barChartWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab') customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab') logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab') channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab')
    additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab') eventParameterSelectionTab: EventParameterSelectionTabComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave = new EventEmitter<CreateOrEditBarChartWidgetConfigurationDto>();
    @Output() onClose = new EventEmitter<boolean>();

    saving = false;
    expandTags = true;

    barChartWidgetConfiguration: CreateOrEditBarChartWidgetConfigurationDto =
        new CreateOrEditBarChartWidgetConfigurationDto();

    componentsState: ComponentsState;

    parameters: WidgetParametersColumn[] = [];

    previewData: any[] = null;

    tabs = [
        { ID: 1, name: 'Custom', template: 'customTemplate' },
        { ID: 2, name: 'Logical', template: 'logicalTemplate' },
        { ID: 3, name: 'Channel', template: 'channelTemplate' },
        { ID: 4, name: 'Additional', template: 'additionalTemplate' },
        { ID: 5, name: 'Event', template: 'eventTemplate' },
    ];

    xUnitOptions = [];

    chartTypeOptions = [
        //{ label: this.l('Plain'), value: 1 },
        { label: this.l('Stacked'), value: 2 },
        { label: this.l('Clustered'), value: 3 },
    ];

    showComponentSelector = true;
    showParameterTabs = true;

    seriesOptions = [];
    selectedXUnit: string = null;
    selectedSeries: string = null;
    isAutoResolution = false;
    resolutionInSeconds = 0;

    private subs: Subscription[] = [];

    dateRangeSelectionState: DateRangeAndRefreshModelNew;

    constructor(
        injector: Injector,
        private _barChartConfigurationService: BarchartWidgetConfigurationService,
        private _barChartConfigurationServiceProxy: BarChartWidgetConfigurationsServiceProxy,
        private _customParameterService: CustomParameterService,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _dateRangeService: DateRangeService,
        private _groupServiceProxy: GroupsServiceProxy,
    ) {
        super(injector);
    }

    get xUnitLabel(): string {
        return this.selectedXUnit ? this.xUnitOptions.find((x) => x.value === this.selectedXUnit)?.label : '';
    }

    get seriesLabel(): string {
        return this.selectedSeries ? this.seriesOptions.find((x) => x.value === this.selectedSeries)?.label : '';
    }

    ngOnInit(): void {
        this.fillXUnitOptions();
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

    onChartTypeChange(event) {
        if (event === null) {
            this.barChartWidgetConfiguration.type = 1;
        } else {
            this.barChartWidgetConfiguration.type = event;
        }
    }

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        this.resetPreviewData();
        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            const newItem = {
                id: Guid.newGuid().toString(),
                name: parameter.name,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                advancedSettings: event.advancedSettings,
                resolution: 0,
                style: null
            };
            this.parameters = [newItem, ...this.parameters];
        });
        this.subs.push(sub);
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        this.resetPreviewData();
        const newItem = {
            id: Guid.newGuid().toString(),
            name: event.parameter.name,
            quantity: event.quantity,
            type: ColumnType.BaseParameter,
            data: safeStringify(event.parameter),
            advancedSettings: event.advancedSettings,
            resolution: 0,
            style: null
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
        this.resetPreviewData();
        const phaseNames = event.phases.map((phase) => phase).join(', ');
        const formattedName = `${event.event.description} (${phaseNames}) ${event.parameter}`;

        const newItem = {
            id: Guid.newGuid().toString(),
            name: formattedName,
            quantity: event.quantity,
            type: ColumnType.Event,
            resolution: 0,
            data: safeStringify({
                event: event.event,
                phases: event.phases,
                parameter: event.parameter,
                isPolyphase: event.polyphase,
                aggregationInSeconds: event.aggregation.aggregationValue,
            }),
            advancedSettings: event.advancedSettings,
            style: null
        };

        this.parameters = [...this.parameters, newItem];

        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        setTimeout(() => {
            this.scrollDown();
        }, 100);
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        this.resetPreviewData();
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.advancedSettings = event.advancedSettings;
        });
        this.subs.push(sub);
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        this.resetPreviewData();
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        tableParameter.name = event.parameter.name;
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify(event.parameter);
        tableParameter.advancedSettings = event.advancedSettings;
    }

    onEditException(event: EditExceptionEventCallBack) {
        this.resetPreviewData();
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
        }); 
        this.subs.push(sub);
    }

    onEditEvent(event: EditEventParameterEventCallBack) {
        this.resetPreviewData();
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

    hide(isSaved: boolean): void {
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.additionalParameterSelectionTab.finishEdit();
        // this.exceptionParameterSelectionTab.finishEdit();
        this.eventParameterSelectionTab.finishEdit();
        this.modalVisible = false;
        this.onClose.emit(isSaved);
    }

    isFormValid(): boolean {
        return this.pqsForm?.form?.valid ?? false;
    }

    changeExpandState() {
        this.expandTags = !this.expandTags;
    }

    onDateRangeChanged(dateRangeState: DateRangeState) {
        this.barChartWidgetConfiguration.dateRange = dateRangeState.toJSON();
    }

    onComponentsStateChange() {
        this.componentsState = { ...this.componentsState };
        this.resetPreviewData();
    }

    save() {
        this.saving = true;
        this.barChartWidgetConfiguration.dateRange = safeStringify(this.dateRangeSelectionState);
        this.barChartWidgetConfiguration.components = safeStringify(this.componentsState);

        const params = this.parameters.map((p) => ({
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
        if (this.barChartWidgetConfiguration.id) {
            this._barChartConfigurationServiceProxy
                .createOrEdit(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this._barChartConfigurationService.update(this.barChartWidgetConfiguration);
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        } else {
            this._barChartConfigurationServiceProxy
                .createAndGetId(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.barChartWidgetConfiguration.id = result;
                    this._barChartConfigurationService.update(this.barChartWidgetConfiguration);
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto) {
        this.open();
        if (configuration && configuration.configuration) {
            var sub = this._barChartConfigurationService
                .getForEdit(+configuration.configuration)
                .subscribe((result) => {
                    this.barChartWidgetConfiguration = result.barChartWidgetConfiguration;
                    let loadedSeries: string = '';

                    // try {
                        const cfg = JSON.parse(this.barChartWidgetConfiguration.configuration);

                        this.selectedXUnit = cfg.xUnit;
                        this.selectedSeries = cfg.series;
                        loadedSeries = cfg.series;

                        this.isAutoResolution = cfg.isAutoResolution ?? false;
                        this.resolutionInSeconds = cfg.resolutionInSeconds ?? 0;
                        this.parameters = (cfg.parameters ?? []).map((p: any) => ({
                            ...p,
                            resolution: p.resolution ?? 0,
                        }));
                        this.componentsState = cfg.componentsState;
                        this.dateRangeSelectionState = DateRangeAndRefreshModelNew.createItem(
                            this.barChartWidgetConfiguration.dateRange,
                        );
                        
                    // } catch {
                    //     this.dateRangeSelectionState = DateRangeAndRefreshModelNew.createItem(
                    //         this.barChartWidgetConfiguration.dateRange,
                    //     );
                    //     this.componentsState = JSON.parse(this.barChartWidgetConfiguration.components);
                    //     this.parameters = JSON.parse(this.barChartWidgetConfiguration.configuration).map((p: any) => ({
                    //         ...p,
                    //         resolution: p.resolution ?? 0,
                    //     }));
                    //     const comps = JSON.parse(this.barChartWidgetConfiguration.components) as any[];
                    //     const params = this.parameters;

                    //     if (this.barChartWidgetConfiguration.dateRange && params.length === 0) {
                    //         this.selectedXUnit = 'time';
                    //         loadedSeries = comps.length > 1 ? 'components' : 'parameters';
                    //     } else if (comps.length > 1 && params.length === 1) {
                    //         this.selectedXUnit = 'components';
                    //         loadedSeries = 'parameters';
                    //     } else if (params.length > 1 && comps.length === 1) {
                    //         this.selectedXUnit = 'parameters';
                    //         loadedSeries = 'components';
                    //     }
                    // }

                    this.onXUnitChange(this.selectedXUnit);
                    this.selectedSeries = loadedSeries;
                    this.onSeriesChange(this.selectedSeries);
                    this.loadPreviewData();
                });
            this.subs.push(sub);
        } else {
            this.barChartWidgetConfiguration = new CreateOrEditBarChartWidgetConfigurationDto();
            this.dateRangeSelectionState = null;
            this.componentsState = null;
            this.parameters = [];
            this.selectedXUnit = null;
            this.selectedSeries = null;
            this.isAutoResolution = false;
            this.resolutionInSeconds = 0;
            this.previewData = null;
        }
    }

    updateParameter(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.handleParameter(event.data, 'edit');
        this.resetPreviewData();
    }

    duplicateParameterCommand = (e: DxDataGridTypes.ColumnButtonClickEvent) => {
        const parameter = e.row.data as WidgetParametersColumn;
        this.handleParameter(parameter, 'duplicate');
        this.resetPreviewData();
    };

    fillXUnitOptions() {
        var options = [
            { label: 'Components', value: 'components' },
            { label: 'Parameters', value: 'parameters' },
            { label: 'Time intervals', value: 'time' },
            // { label: 'Phases', value: 'phases' },
            // { label: 'Base', value: 'base' },
        ];

        var sub = this._groupServiceProxy
            .getAll(undefined, undefined, undefined, undefined, 0, 2147483647)
            .subscribe((groups) => {
                for (const group of groups.items) {
                    options.push({ label: group.group.name, value: group.group.id });
                }
                this.xUnitOptions = options;
            });
        this.subs.push(sub);
    }

    onXUnitChange(x: string) {
        this.resetPreviewData();
        this.selectedXUnit = x;
        this.selectedSeries = '';
        this.showComponentSelector = x !== 'components';
        this.showParameterTabs = x !== 'parameters';

        this.seriesOptions = this.xUnitOptions.filter((u) => u.value !== x);
    }

    swapXUnitSeries() {
        const prevX = this.selectedXUnit;
        const prevSeries = this.selectedSeries;

        this.selectedXUnit = prevSeries;
        this.onXUnitChange(prevSeries);

        setTimeout(() => {
            this.selectedSeries = prevX;
            this.onSeriesChange(this.selectedSeries);
        }, 0);
    }

    onSeriesChange(series: string) {
        this.resetPreviewData();
        this.selectedSeries = series;
        this.showComponentSelector = this.selectedXUnit !== 'components' || this.selectedSeries.includes('components');

        this.showParameterTabs = this.selectedXUnit !== 'parameters' || this.selectedSeries.includes('parameters');
    }

    onRowRemoved() {
        this.parameters = [...this.parameters];
        this.resetPreviewData();
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
    private resetPreviewData(): void {
        this.previewData = null;
    }

    private loadPreviewData(): void {
        if (!this.barChartWidgetConfiguration?.id) {
            return;
        }

        let [startDate, endDate] = this._dateRangeService.getDateRangeFromNewState(this.dateRangeSelectionState);

        if (!startDate || !endDate || startDate >= endDate) {
            [startDate, endDate] = this._dateRangeService.getDateRangeFromNewUnit(RefreshSelectionCustomUnits.Day, 7);
        }

        const config = JSON.parse(this.barChartWidgetConfiguration.configuration);

        const seriesBy = this.mapSerieses(config.series);
        const category = this.mapSerieses(config.xUnit);

        const componentsState: ComponentsState = this.componentsState;
        const formattedFeeders = componentsState?.feeders?.map((f) => new FeederComponentInfo(f)) ?? [];
        const tableWidgetConfigurationComponents = componentsState?.components ?? [];
        const formattedComponents = tableWidgetConfigurationComponents
            .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
            .map((c) => new FeederComponentInfo({ componentId: c.key, id: null, name: null, compName: c.label }));

        const request = new BarChartRequest({
            seriesBy: new DimensionSelector({ type: seriesBy, id: null, name: null }),
            category: new DimensionSelector({ type: category, id: null, name: null }),
            barPrmList: config.parameters.map((p) => {
                let baseData: string = null;
                let customData: CustomWidgetTableData = null;
                let tableEvent: TableWidgetEvent = null;

                switch (p.type) {
                    case ColumnType.CustomParameter:
                        customData = new CustomWidgetTableData({
                            id: Number.parseInt(p.data.toString()),
                            ignoreAlignment: false,
                            quantity: p.quantity,
                        });
                        break;

                    case ColumnType.Exception:
                    case ColumnType.BaseParameter:
                        baseData = this.prepareParameterForRequest(p.type, p.data);
                        break;

                    case ColumnType.Event:
                        tableEvent = this.createTableWidgetEvent(p.data.toString(), p.quantity);
                        break;

                    default:
                        console.warn('Unknown column type:', p.type);
                        break;
                }

                return new BarParameter({
                    parameterType: p.type,
                    excludeFlagged: this.prepareExcludedFlagged(
                        p.advancedSettings?.excludeFlagged,
                        ArrayUtils.ensureArray(p.advancedSettings?.defaultFlagEvent),
                    ),
                    baseData,
                    customData,
                    tableEvent,
                    parameterName: p.name,
                    isExcludeFlaggedData: p.advancedSettings?.excludeFlagged === ExcludeFlagged.DefaultEvents,
                });
            }),
            feeders: [...formattedFeeders, ...formattedComponents],
            widgetName: '',
            startDate: DateTime.fromJSDate(startDate),
            endDate: DateTime.fromJSDate(endDate),
            userTimeZone: 1,
            refreshRateInSeconds: 0,
            isRealTime: false,
        });

        var sub = this._tenantDashboardService.pQSBarChartWidgetData(request).subscribe({
            next: (response: BarChartResponse) => {
                const groups = response?.groups || [];
                this.previewData =
                    this.barChartWidgetConfiguration.type === BarChartType.Plain
                        ? this.getPlainData(groups)
                        : this.transformData(groups);
            },
            error: (err) => {
                console.error('Bar chart preview data load failed', err);
            },
        });
        this.subs.push(sub);
    }

    private getPlainData(groups: any[]): any[] {
        return groups.map((g) => ({
            category: g.category,
            value: g.bars?.[0]?.value ?? 0,
            seriesName: g.bars?.[0]?.seriesName,
        }));
    }

    private transformData(groups: any[]): any[] {
        const transformed: any[] = [];
        groups.forEach((g) => {
            (g.bars || []).forEach((bar) => {
                transformed.push({
                    category: g.category,
                    seriesName: bar.seriesName,
                    value: bar.value,
                });
            });
            if (!g.bars?.length) {
                transformed.push({
                    category: g.category,
                    seriesName: 'No Data',
                    value: 0,
                });
            }
        });
        return transformed;
    }

    private mapSerieses(seriesBy: string): DimensionType {
        switch (seriesBy) {
            case 'components':
                return DimensionType.Feeders;
            case 'parameters':
                return DimensionType.Parameters;
            case 'groups':
                return DimensionType.CustomGroup;
            case 'time':
                return DimensionType.Dates;
        }
    }

    private prepareParameterForRequest(type: ColumnType, data: string | number): string {
        switch (type) {
            case ColumnType.BaseParameter:
                return data.toString();
            case ColumnType.Event:
                const event = JSON.parse(data.toString());
                return safeStringify({
                    eventId: event.event.eventClass,
                    phases: event.phases,
                    parameter: event.parameter,
                    isPolyphase: event.isPolyphase,
                    aggregationInSeconds: event.aggregationInSeconds,
                });
            default:
                return data.toString();
        }
    }

    private prepareExcludedFlagged(excludeFlagged: ExcludeFlagged, selectedEvents: EventClass[]): EventClass[] {
        switch (excludeFlagged) {
            case ExcludeFlagged.None:
                return [];
            case ExcludeFlagged.DefaultEvents:
                return [];
            case ExcludeFlagged.UserSelected:
                return selectedEvents;
            default:
                return [];
        }
    }

    private createTableWidgetEvent(json: string, quantity: string): TableWidgetEvent {
        const event = JSON.parse(json);
        const tableWidgetEvent = new TableWidgetEvent({
            phases: event.phases,
            eventId: event.event.confID,
            eventClass: event.event.eventClass,
            isShared: event.event.isShared,
            parameter: event.parameter,
            isPolyphase: event.isPolyphase,
            aggregationInSeconds: event.aggregationInSeconds,
            quantity,
        });

        return tableWidgetEvent;
    }

    ngOnDestroy(): void {
        this.subs.forEach(sub => sub.unsubscribe());
    }
}
