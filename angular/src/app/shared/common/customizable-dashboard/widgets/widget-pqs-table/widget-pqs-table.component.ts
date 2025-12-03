import {
    AfterViewInit,
    Component,
    ElementRef,
    EventEmitter,
    HostBinding,
    Injector,
    OnDestroy,
    OnInit,
    Output,
    ViewChild,
} from '@angular/core';
import {
    CreateOrEditTableWidgetConfigurationDto,
    TableWidgetResponse,
    TableWidgetConfigurationsServiceProxy,
    TenantDashboardServiceProxy,
    FeederComponentInfo,
    GetComponentByTagsRequest,
    GetComponentSlimInfosRequest,
    PQZStatus,
    TableWidgetRequest,
    ColumnWidgetTable,
    CustomWidgetTableData,
    RowWidgetTable,
    TagTableWidget,
    ITableWidgetResponseItem,
    DataUnitType,
    TableWidgetEvent,
    EventClass,
    TableWidgetConfigurationDto,
} from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { CreateOrEditTableConfigurationComponent } from './create-or-edit-table-configuration/create-or-edit-table-configuration.component';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { DateTime } from 'luxon';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { catchError, debounceTime, ignoreElements, map, Subject, Subscription, switchMap, takeUntil, throwError, timer } from 'rxjs';
import { ComponentsState } from '@app/shared/models/components-state';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ColumnType } from '@app/shared/enums/column-type';
import safeStringify from 'fast-safe-stringify';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { TableWidgetDataSourceBuilderService } from '@app/shared/services/table-widget-data-source-builder.service';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { ResolutionComparerService } from '@app/shared/services/resolution-comparer.service';
import { BaseParserService } from '@app/shared/services/base-parser.service';
import { BaseState } from '@app/shared/models/base-state';
import { BaseUnits } from '@app/shared/enums/base-units';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { TableDesignOptions } from './create-or-edit-table-configuration/table-design-options/table-design-options.component';
import { AdvancedSettingsConfig } from '@app/shared/common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { ComponentsService } from '@app/shared/services/components-service.service';
import { TableWidgetConfigurationService } from '@app/shared/services/widget-configurations/table-widget-configuration.service';
import { dxTreeListColumn } from '@node_modules/devextreme/ui/tree_list';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';



interface TreeWidgetParametersColumn extends WidgetParametersColumn {
    columnDataUnitTypeName: string;
}


@Component({
    selector: 'app-widget-pqs-table',
    templateUrl: './widget-pqs-table.component.html',
    styleUrls: ['./widget-pqs-table.component.css'],
})
export class WidgetPQSTableComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy, AfterViewInit {
    @HostBinding('style.--header-bg-color') headerBgColor: string;
    @HostBinding('style.--header-text-color') headerTextColor: string;
    @HostBinding('style.--border-color') borderColor: string;
    @HostBinding('style.--cell-font-family') cellFontFamily: string;
    @HostBinding('style.--header-font-family') headerFontFamily: string;

    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditTableConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;
    @ViewChild('treeContainer', { static: false }) treeContainer!: ElementRef;

    @Output() widgetRefresh: EventEmitter<any> = new EventEmitter();

    editState: boolean = false;

    isLoading = false;

    pqsTableConfigRequest: TableWidgetRequest;
    pqsTableDataResponse: TableWidgetResponse;
    selectedDateRange: any;
    files: any;
    tableWidgetConfiguration: TableWidgetConfigurationModel;
    tableWidgetConfDb: TableWidgetConfigurationDto;

    eventDisplayMode: 'value' | 'duration' = 'value';
    sortMode: 'value' | 'color' = 'value';

    dataSource: any;
    columns: dxTreeListColumn[];

    stopStream$ = new Subject();

    private widthChanges$ = new Subject<number[]>();
    private widthChangeStream$ = new Subject();

    private observer!: MutationObserver;
    private configColumnWidthes: string[];
    private columnWidthes: string[];

    private subs: Subscription[] = [];

    private isResizeIgnored = false;

    constructor(
        injector: Injector,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _resolutionService: ResolutionService,
        private _baseParserService: BaseParserService,
        private _resolutionComparerService: ResolutionComparerService,
        private _tableWidgetConfigurationService: TableWidgetConfigurationService,
        private _tableWidgetConfigurationsServiceProxy: TableWidgetConfigurationsServiceProxy,
        private _tableWidgetDataSourseBuilder: TableWidgetDataSourceBuilderService,
        private _componentsService: ComponentsService,
        elementReference: ElementRef,
        dateRangeService: DateRangeService,
        private _configurationVersionService: ConfigurationVersionService,
        private _dateTimeService: DateTimeService,

    ) {
        super(injector, elementReference, dateRangeService);

        this._defaultWidgetName = this.l('WidgetPQSTable');
        const saved = localStorage.getItem('tableSortMode');
        if (saved === 'value' || saved === 'color') {
            this.sortMode = saved as any;
        }
    }

    ngAfterViewInit(): void {
        this.setupScrollBehavior();
    }

    setupScrollBehavior() {
        const innerScroll = this.elementRef.nativeElement.querySelector('#innerScroll');
        const outerScroll = this.elementRef.nativeElement.querySelector('#outerScroll');

        innerScroll.addEventListener('wheel', (event: WheelEvent) => {
            event.preventDefault();
            const scrollAmount = event.deltaY;
            innerScroll.scrollTop += scrollAmount;

            const isAtTop = innerScroll.scrollTop === 0;
            const isAtBottom = innerScroll.scrollHeight - innerScroll.clientHeight === innerScroll.scrollTop;

            if ((isAtTop && scrollAmount < 0) || (isAtBottom && scrollAmount > 0)) {
                outerScroll.scrollTop += scrollAmount;
            }
        });
    }

    ngOnInit(): void {
        super.ngOnInit();

        this.subscribeToEvent('app.dashboardEdit.onSave', () => this.checkAndSaveColumns());

        this.widthChanges$.pipe(debounceTime(500), takeUntil(this.widthChangeStream$)).subscribe((widths) => {
            if (this.isResizeIgnored) {
                return;
            }
            this.calculateWidthes(widths);
            this.onColumnWidthsStabilized();
        });

        if (this.isNew) {
            this.runDelayed(() => this.onEditRequested(null));
        }
    }

    onSortModeChange(mode: 'value' | 'color') {
        this.sortMode = mode;
        localStorage.setItem('tableSortMode', mode);
        this.refreshWidget();
        //this.applySort();
    }

    onNameEdit() {
        this.renameModal.show(this.widgetConfigurationInDB?.name);
    }

    protected override onEditRequested(_payload: any): void {
        this.edit();
    }

    protected override onRenameRequested(_payload: any): void {
        this.onNameEdit();
    }

    createTableWidgetEvent(json: string, quantity: string): TableWidgetEvent {
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

    fetch() {
        let range: [DateTime, DateTime] = this.prepareDataRange();

        let formattedFeeders =
            this.tableWidgetConfiguration?.components?.feeders?.map((f) => new FeederComponentInfo(f)) ?? [];
        let tableWidgetConfigurationComponents = this.tableWidgetConfiguration?.components?.components ?? [];
        let formattedComponents = tableWidgetConfigurationComponents
            .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
            .map((c) => new FeederComponentInfo({ componentId: c.key, id: null, name: null, compName: c.label }));

        let tagsList = this.tableWidgetConfiguration.components?.tags ?? [];

        this.pqsTableConfigRequest = new TableWidgetRequest({
            widgetName: this.widgetConfigurationInDB?.name,
            rows: new RowWidgetTable({
                feeders: [...formattedFeeders, ...formattedComponents],
                tags: tagsList.map((t) => {
                    const [key, value] = t.label.split(':');
                    return new TagTableWidget({
                        id: key,
                        name: value,
                        feeders: this.getFeedersByTagModel(t, formattedFeeders),
                    });
                }),
            }),
            columnWidgetTables: this.tableWidgetConfiguration.parameters.map((column) => {
                let baseData: string = null;
                let customData: CustomWidgetTableData = null;
                let tableEvent: TableWidgetEvent = null;

                switch (column.type) {
                    case ColumnType.CustomParameter:
                        customData = new CustomWidgetTableData({
                            id: Number.parseInt(column.data.toString()),
                            ignoreAlignment: false,
                            quantity: column.quantity,
                        });
                        break;

                    case ColumnType.Exception:
                    case ColumnType.BaseParameter:
                        baseData = this.prepareParameterForRequest(column.type, column.data);
                        break;

                    case ColumnType.Event:
                        tableEvent = this.createTableWidgetEvent(column.data.toString(), column.quantity);
                        break;

                    default:
                        // optionally handle unknown types
                        console.warn('Unknown column type:', column.type);
                        break;
                }

                return new ColumnWidgetTable({
                    parameterType: column.type,
                    normalize: column.advancedSettings?.normalizeValue,
                    normalValue: column.advancedSettings?.normalizeNominalValue,
                    excludeFlagged: this.prepareExcludedFlagged(
                        column.advancedSettings?.excludeFlagged,
                        ArrayUtils.ensureArray(column.advancedSettings?.defaultFlagEvent),
                    ),
                    ignoreAligningFunction: column.advancedSettings?.aligningIgnored,
                    replaceAggregationWith: column.advancedSettings?.customAggregationFunc,
                    baseData,
                    customData,
                    tableEvent,
                    parameterName: column.name,
                    isExcludeFlaggedData: column.advancedSettings?.excludeFlagged === ExcludeFlagged.DefaultEvents,
                    markers: null,
                });
            }),
            startDate: range[0],
            endDate: range[1],
            userTimeZone: this._dateTimeService.getUserTimeZoneName(),
            refreshRateInSeconds: 0,
            isRealTime: false,
        });

        // find minimum resolution from all columns
        let columnResolutions = [];

        let baseParameterResolutions = this.tableWidgetConfiguration.parameters
            .filter((column) => {
                return column.type === 'BaseParameter';
            })
            .map((column) => {
                let data = JSON.parse(String(column.data));
                let baseModel: BaseState = { value: 0, unit: BaseUnits.Cycle };
                this._baseParserService.tryParse(data.baseResolution, baseModel);
                return this._resolutionService.parseStateFromBaseState(baseModel);
            });
        let customParameterColumnIds = this.pqsTableConfigRequest.columnWidgetTables
            .filter((column) => column.parameterType === 'CustomParameter')
            .map((column) => column.customData.id);
        let customParameterChildrenResolutions = new Set();
        customParameterColumnIds.forEach((cpId) => {
            this._resolutionComparerService.getResolutionsFromCustomParameter(cpId).subscribe((response: number[]) => {
                response.forEach((item) => customParameterChildrenResolutions.add(item));
            });
        });
        columnResolutions.push(...baseParameterResolutions);
        columnResolutions.push(...Array.from(customParameterChildrenResolutions));

        if (this.tableWidgetConfiguration.dateRange?.refreshIntervalInSeconds
            && this.tableWidgetConfiguration.dateRange?.refreshIntervalInSeconds !== -1) {
            timer(0, this.tableWidgetConfiguration.dateRange.refreshIntervalInSeconds * 1000) // use minimum resulotion value(ms) to refresh table widget
                .pipe(takeUntil(this.stopStream$))
                .subscribe((result) => {
                    let range: [DateTime, DateTime] = this.prepareDataRange();
                    this.pqsTableConfigRequest.startDate = range[0];
                    this.pqsTableConfigRequest.endDate = range[1];
                    this.pqsTableConfigRequest.refreshRateInSeconds = this.tableWidgetConfiguration.dateRange.refreshIntervalInSeconds;
                    this.pqsTableConfigRequest.isRealTime = true;
                    this.getPQSTableData(this.pqsTableConfigRequest);
                });
        } else {
            this.pqsTableConfigRequest.refreshRateInSeconds = 0;
            this.pqsTableConfigRequest.isRealTime = false;
            this.getPQSTableData(this.pqsTableConfigRequest);
        }
    }

    edit() {
        this.isEditModalInitialized = true;
        var sub = this._configurationVersionService.refreshVersion().subscribe(() => {
            setTimeout(() => {
                this.createOrEditModal.show(this.widgetConfigurationInDB);
            }, 0);
        });
        this.subs.push(sub);
    }

    onConfigurationChange(newConfig: CreateOrEditTableWidgetConfigurationDto) {
        if (newConfig.id.toString() !== this.widgetConfigurationInDB?.configuration) {
            this.saveConfiguration(newConfig.id.toString());
        }
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.refreshWidget();
    }

    refreshWidget(): void {
        this.widgetRefresh.emit();
        this.isLoading = true;
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this._tableWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    if (result.tableWidgetConfiguration) {
                        this.tableWidgetConfDb = result.tableWidgetConfiguration;
                        this.tableWidgetConfiguration = {
                            dateRange: DateRangeAndRefreshModelNew.createItem(result.tableWidgetConfiguration.dateRange),
                            parameters: JSON.parse(result.tableWidgetConfiguration.configuration),
                            components: JSON.parse(result.tableWidgetConfiguration.components),
                            designOptions: result.tableWidgetConfiguration.designOptions
                                ? JSON.parse(result.tableWidgetConfiguration.designOptions)
                                : {
                                      headerBackgroundColor: '#ffffff',
                                      borderColor: '#cccccc',
                                      borderWeight: 1,
                                      borderStyle: 'solid',
                                      bandedRows: false,
                                  },
                        };
                        this.configColumnWidthes = this.tableWidgetConfiguration.parameters
                            .map((x) => x.style)
                            .filter((x) => !!x);
                        this.columnWidthes = JSON.parse(JSON.stringify(this.configColumnWidthes));
                        this.headerBgColor = this.tableWidgetConfiguration.designOptions.headerBackgroundColor;
                        this.headerTextColor = this.tableWidgetConfiguration.designOptions.headerTextColor;
                        this.borderColor = this.tableWidgetConfiguration.designOptions.borderColor;
                        this.cellFontFamily = this.tableWidgetConfiguration.designOptions.cellFontFamily;
                        this.headerFontFamily = this.tableWidgetConfiguration.designOptions.headerFontFamily;
                        this.fetch();
                    }
                });
            this.subs.push(sub);
        }
    }

    getPQSTableData = (input: TableWidgetRequest) => {
        let extractedTags;
        const originalComponentTagsMap = new Map<string, string[]>();
        this.tableWidgetConfiguration.components.components.forEach((comp) => {
            if (comp.key && comp.data?.tags) {
                originalComponentTagsMap.set(comp.key, [...comp.data.tags]);
            }
        });
        var sub = this._tenantDashboardService
            .pQSTableWidgetData(input)
            .pipe(
                catchError((error) => {
                    this.isLoading = false;
                    return throwError(() => error);
                }),
                switchMap((response: TableWidgetResponse) => {
                    this._tableWidgetDataSourseBuilder.formatParameterNames(response.items);
                    this.tableWidgetConfiguration.parameters.forEach((param) => {
                        if (!param.name.endsWith(param.quantity)) {
                            param.name += ` ${param.quantity}`;
                        }
                    });

                    this.pqsTableDataResponse = response;

                    extractedTags = input.rows.tags.map((t) => `${t.id}`);
                    extractedTags = this.tableWidgetConfiguration.components.pickListState.target
                        .map((x) => x.key)
                        .filter((option) => extractedTags.includes(option));
                    const getComponentByTagsRequest = new GetComponentByTagsRequest();
                    getComponentByTagsRequest.tags = extractedTags;
                    return this._componentsService.componentByTags(getComponentByTagsRequest).pipe(
                        switchMap((componentByTagsResponse) => {
                            let componentIds = [];
                            if (componentByTagsResponse) {
                                componentIds = componentByTagsResponse
                                    .flatMap((c) => c.componentIds)
                                    .filter((v, i, a) => a.indexOf(v) === i);
                            }
                            const getComponentSlimInfosRequest = new GetComponentSlimInfosRequest();
                            getComponentSlimInfosRequest.componentIds = componentIds;
                            return this._componentsService.componentSlimsInfo(getComponentSlimInfosRequest);
                        }),
                        map((slimResponse) => {
                            const componentsByIds = slimResponse;
                            this.tableWidgetConfiguration.components.components = componentsByIds.map((c) => ({
                                id: c.componentId,
                                name: c.componentName,
                                key: c.componentId,
                                label: c.componentName,
                                data: { tags: originalComponentTagsMap.get(c.componentId) || [] },
                                children: c.feeders.map((f) => ({ id: f.id, label: f.name })),
                            }));
                            this.tableWidgetConfiguration.components.feeders = componentsByIds.flatMap((c) =>
                                c.feeders.map(
                                    (f) =>
                                        new FeederComponentInfo({
                                            id: f.id,
                                            name: f.name,
                                            componentId: c.componentId,
                                            compName: c.componentName,
                                        }),
                                ),
                            );
                            return this.pqsTableDataResponse;
                        }),
                    );
                }),
            )
            .subscribe((response: TableWidgetResponse) => {
                const transformedItems = this._tableWidgetDataSourseBuilder.convertComponentsTagsArrayToProps(
                    this.tableWidgetConfiguration.components,
                    response.items,
                );

                const treeData = this.buildTreeData(transformedItems, extractedTags);

                const emptyTagIds = new Set<string>(
                    treeData
                        .filter(
                            (n) =>
                                n.id.startsWith('tag_') &&
                                Object.keys(n)
                                    .filter((k) => !['id', 'parentId', 'name', 'expanded', 'children'].includes(k))
                                    .every((k) => n[k] == null),
                        )
                        .map((n) => n.id),
                );

                const tagParentMap = new Map<string, string | null>();
                treeData.filter((n) => emptyTagIds.has(n.id)).forEach((n) => tagParentMap.set(n.id, n.parentId));

                const reparented = treeData.map((n) => {
                    let pid = n.parentId;
                    while (pid && tagParentMap.has(pid)) {
                        pid = tagParentMap.get(pid);
                    }
                    return pid !== n.parentId ? { ...n, parentId: pid } : n;
                });

                const cleaned = reparented.filter((n) => !(n.id.startsWith('tag_') && emptyTagIds.has(n.id)));

                const parameters = this.createcolumnDataUnitTypeName(
                    this.tableWidgetConfiguration.parameters,
                    response.items,
                );
                this.columns = this.buildColumns(parameters);
                //this.columns = this.buildColumns(this.tableWidgetConfiguration.parameters);
                this.dataSource = cleaned;
            });
        this.subs.push(sub);
    };

    onContentReady() {
        this.isLoading = false;

        if (!this.observer) {
            this.runDelayed(() => {
                const header = this.treeContainer.nativeElement.querySelector('.dx-treelist-headers');

                if (!header) return;

                this.observer = new MutationObserver(() => {
                    if (this.editState) {
                        const widths = Array.from(header.querySelectorAll('col')).map((c: any) => c.offsetWidth);
                        this.widthChanges$.next(widths);
                    }
                });

                this.observer.observe(header, { attributes: true, attributeFilter: ['style'], subtree: true });
            });
        }
    }

    onDateRangeFilterChange = (dateRange) => {
        if (
            !dateRange ||
            dateRange.length !== 2 ||
            (this.selectedDateRange[0] === dateRange[0] && this.selectedDateRange[1] === dateRange[1])
        ) {
            return;
        }

        this.selectedDateRange[0] = dateRange[0];
        this.selectedDateRange[1] = dateRange[1];
        this.pqsTableConfigRequest.startDate = dateRange[0];
        this.pqsTableConfigRequest.endDate = dateRange[1];
        this.runDelayed(() => {
            this.getPQSTableData(this.pqsTableConfigRequest);
        });
    };

    onEditModelClose(isSaved) {
        if (!isSaved && !this.widgetConfigurationInDB?.configuration) {
            abp.event.trigger(
                'app.dashboard.removeWidget',
                this.widgetConfigurationInDB.widgetGuid,
                'Widgets_Tenant_PQSTable',
            );
        }
    }

    subDateRangeFilter() {
        this.subscribeToEvent('app.dashboardFilters.dateRangePicker.onDateChange', this.onDateRangeFilterChange);
    }

    hasEventParameter(): boolean {
        return this.tableWidgetConfiguration?.parameters?.some((param) => param.type === ColumnType.Event);
    }

    ngOnDestroy() {
        this.subs.forEach((sub) => sub.unsubscribe());
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.widthChangeStream$.next(null);
        this.widthChangeStream$.complete();
    }

    private prepareDataRange(): [DateTime, DateTime] {
        const state: DateRangeAndRefreshModelNew = this.tableWidgetConfiguration?.dateRange
            ? this.tableWidgetConfiguration?.dateRange
            : DateRangeAndRefreshModelNew.createItem('');
        var [startDate, endDate] = this._dateRangeService.getDateRangeFromNewState(state);

        return [DateTime.fromJSDate(startDate), DateTime.fromJSDate(endDate)];
    }

    private formatNumber(value: number, dataUnitType: DataUnitType): string {
        const token = dataUnitType?.id !== 41 && dataUnitType?.id !== 255 ? this.l(dataUnitType.tokenCode) : '';

        if (value === 0) {
            return `0${token}`;
        }

        let absValue = Math.abs(value);
        let suffix = '';

        if (absValue >= 1) {
            if (absValue >= 1e3 && absValue < 1e6) {
                value /= 1e3;
                suffix = 'K';
            } else if (absValue >= 1e6 && absValue < 1e9) {
                value /= 1e6;
                suffix = 'M';
            } else if (absValue >= 1e9 && absValue < 1e12) {
                value /= 1e9;
                suffix = 'G';
            }
        } else {
            let strValue = value.toExponential();
            let leadingZeros = Math.abs(parseInt(strValue.split('e')[1]));

            if (leadingZeros >= 1 && leadingZeros <= 3) {
                value *= 1e3;
                suffix = 'm';
            } else if (leadingZeros >= 4 && leadingZeros <= 6) {
                value *= 1e6;
                suffix = 'μ';
            } else if (leadingZeros >= 7 && leadingZeros <= 9) {
                value *= 1e9;
                suffix = 'n';
            }
        }

        return `${value.toFixed(2)}${suffix}`;
    }

    private calculateColumnFirstColumnAndDefaultWidth(widths: string[]): [number, number] {
        const defaultWidth = 100 / (this.columns.length + 1);
        const firstColumnWidth =
            100 -
            widths.reduce((accumulator, width) => {
                return accumulator + Number.parseFloat(width.replace('%', ''));
            }, 0);

        return [firstColumnWidth, defaultWidth];
    }

    private calculateWidthes(newWidths: number[]) {
        const totalWidth = newWidths.reduce((accumulator, currentValue) => {
            return accumulator + currentValue;
        }, 0);

        newWidths.shift(); // we do not save width of the first column (tree column)

        this.columnWidthes = [];

        newWidths.forEach((w) => {
            this.columnWidthes.push(`${(w * 100) / totalWidth}%`);
        });
    }

    private onColumnWidthsStabilized(): void {
        var i = 0;
        const [firstColumnWidth, defaultWidth] = this.calculateColumnFirstColumnAndDefaultWidth(this.columnWidthes);

        this.columns?.forEach((element) => {
            if (i == 0) {
                element.width = `${firstColumnWidth}%`;
            } else {
                element.width = this.columnWidthes[i - 1];
            }
            i++;
        });

        this.isResizeIgnored = true;

        this.columns = [...this.columns];

        setTimeout(() => {
            this.isResizeIgnored = false;
        }, 500);
    }

    private checkAndSaveColumns() {
        if (this.configColumnWidthes !== this.columnWidthes) {
            let i = 0;

            let parameters = JSON.parse(this.tableWidgetConfDb.configuration) as WidgetParametersColumn[];
            parameters.forEach((p) => {
                p.style = this.columnWidthes[i];
                i++;
            });
            const request: CreateOrEditTableWidgetConfigurationDto = new CreateOrEditTableWidgetConfigurationDto({
                id: this.tableWidgetConfDb.id,
                dateRange: this.tableWidgetConfDb.dateRange,
                components: this.tableWidgetConfDb.components,
                configuration: safeStringify(parameters),
                designOptions: this.tableWidgetConfDb.designOptions,
            });
            var sub = this._tableWidgetConfigurationsServiceProxy.createOrEdit(request).subscribe();
            this.subs.push(sub);
        }
    }

    private createNode(id: string, parentId: string | null, name: string, dataUnitType: DataUnitType): any {
        // name = `${name}_XX`;
        return {
            id,
            parentId,
            name,
            expanded: true,
            dataUnitType,
        };
    }

    private getDurationValue(param: any, responseItem: any): any {
        try {
            const paramData = typeof param.data === 'string' ? JSON.parse(param.data) : param.data;
            return paramData.aggregationInSeconds;
        } catch (e) {
            return this.formatRawData(responseItem);
        }
    }

    private calculateSeverity(value: number, cfg: AdvancedSettingsConfig): number {
        // only applies when fixed limits are in use
        if (cfg.setLimits === Limit.Fixed) {
            const lower = cfg.lowerLimit;
            const upper = cfg.upperLimit;

            if (upper > 0 && value > upper) {
                return ((value - upper) / upper) * 100;
            }
            if (lower > 0 && value < lower) {
                return ((lower - value) / lower) * 100;
            }
        }
        return 0;
    }

    private processParameters(node: any, criteria: (item: any, field: string) => boolean): void {
        this.tableWidgetConfiguration.parameters.forEach((param) => {
            const field = param.name;
            const resp = this.pqsTableDataResponse.items.find((item) => criteria(item, field));
            if (!resp) {
                return;
            }
            const raw = this.formatRawData(resp);
            node[field] = raw;
            node[field + '_severity'] = this.calculateSeverity(
                typeof raw === 'number' ? raw : parseFloat(raw as any),
                param.advancedSettings || { setLimits: Limit.None, lowerLimit: 0, upperLimit: 0 },
            );
            if (!node.advancedSettings) node.advancedSettings = {};
            node.advancedSettings[field] = param.advancedSettings;
        });
    }

    private buildTreeData(transformedItems: any[], extractedTags: string[]): any[] {
        const treeData: any[] = [];
        const treeMap = new Map<string, any>();

        const feedersByComponent = transformedItems
            .filter((i) => !!i.feederId)
            .reduce((map, item) => {
                const key = `comp_${item.componentName}`;
                const arr = map.get(key) || [];
                arr.push(item);
                map.set(key, arr);
                return map;
            }, new Map<string, any[]>());

        transformedItems.forEach((transformedItem) => {
            const tagPath = extractedTags.map((tag) => transformedItem[tag]).filter(Boolean);
            let parentId: string | null = null;
            tagPath.forEach((tag, index) => {
                const tagKey = `tag_${tagPath.slice(0, index + 1).join('_')}`;
                if (!treeMap.has(tagKey)) {
                    const tagNode = this.createNode(tagKey, parentId, tag, transformedItem.dataUnitType);
                    this.processParameters(
                        tagNode,
                        (item, field) =>
                            item.tag?.tagId === extractedTags[index] &&
                            item.tag?.tagValue === tag &&
                            item.parameterName === field,
                    );
                    treeData.push(tagNode);
                    treeMap.set(tagKey, tagNode);
                }
                parentId = tagKey;
            });

            if (transformedItem.componentId && !transformedItem.feederId) {
                const componentKey = `comp_${transformedItem.componentName}`;
                if (!treeMap.has(componentKey)) {
                    const componentNode = this.createNode(
                        componentKey,
                        parentId,
                        transformedItem.componentName,
                        transformedItem.dataUnitType,
                    );
                    this.processParameters(
                        componentNode,
                        (item, field) =>
                            item.componentId === transformedItem.componentId &&
                            !item.feederId &&
                            item.parameterName === field,
                    );
                    treeData.push(componentNode);
                    treeMap.set(componentKey, componentNode);
                }
            }

            if (transformedItem.feederId) {
                const componentKey = `comp_${transformedItem.componentName}`;
                const feederCount = feedersByComponent.get(componentKey)?.length || 0;
                if (feederCount > 1) {
                    const feederKey = `feeder_${transformedItem.feederName}_${transformedItem.componentName}`;
                    if (!treeMap.has(feederKey)) {
                        const feederNode = this.createNode(
                            feederKey,
                            transformedItems.some((i) => i.componentId === transformedItem.componentId && !i.feederId)
                                ? componentKey
                                : parentId,
                            transformedItem.feederName,
                            transformedItem.dataUnitType,
                        );
                        treeData.push(feederNode);
                        treeMap.set(feederKey, feederNode);
                    }
                    const feederNode = treeMap.get(feederKey);
                    this.processParameters(
                        feederNode,
                        (item, field) =>
                            item.feederId === transformedItem.feederId &&
                            item.componentId === transformedItem.componentId &&
                            item.parameterName === field,
                    );
                } else {
                    const componentNode = treeMap.get(componentKey);
                    if (componentNode) {
                        this.processParameters(
                            componentNode,
                            (item, field) =>
                                item.feederId === transformedItem.feederId &&
                                item.componentId === transformedItem.componentId &&
                                item.parameterName === field,
                        );
                    } else {
                        const componentKey = `comp_${transformedItem.componentName}`;
                        if (!treeMap.has(componentKey)) {
                            const componentNode = this.createNode(
                                componentKey,
                                parentId,
                                transformedItem.componentName,
                                transformedItem.dataUnitType,
                            );
                            this.processParameters(
                                componentNode,
                                (item, field) =>
                                    item.componentId === transformedItem.componentId && item.parameterName === field,
                            );
                            treeData.push(componentNode);
                            treeMap.set(componentKey, componentNode);
                        }
                    }
                }
            }
        });

        return treeData;
    }

    private createcolumnDataUnitTypeName(
        parameters: WidgetParametersColumn[],
        responseItems: ITableWidgetResponseItem[],
    ): TreeWidgetParametersColumn[] {
        const result: TreeWidgetParametersColumn[] = [];

        parameters.forEach((param) => {
            const responseItem = responseItems?.find((item) => item.parameterName === param.name);

            if (
                responseItem?.dataUnitType?.tokenCode &&
                responseItem?.dataUnitType?.id !== 41 &&
                responseItem?.dataUnitType?.id !== 255
            ) {
                const token = this.l(responseItem.dataUnitType.tokenCode);
                result.push({ ...param, columnDataUnitTypeName: token });
            } else {
                result.push({ ...param, columnDataUnitTypeName: '' });
            }
        });

        return result;
    }

    private buildColumns(parameters: TreeWidgetParametersColumn[]): any[] {
        const defaultWidth = 100 / (parameters.length + 1);
        const firstColumnWidth =
            100 -
            parameters.reduce((accumulator, parameter) => {
                var width = parameter.style ? Number.parseFloat(parameter.style.replace('%', '')) : defaultWidth;
                return accumulator + width;
            }, 0);
        const defaultWidthStyle = `${defaultWidth}%`;
        return [
            { dataField: 'name', caption: '', width: `${firstColumnWidth}%` },
            ...parameters.map((param, idx) => {
                let caption = '';

                if (param.columnDataUnitTypeName?.toLowerCase() === 'count') {
                    caption = `${param.name}\n${this.l('Count')}`;
                } else if (param.columnDataUnitTypeName) {
                    caption = `${param.name}\n[${param.columnDataUnitTypeName}]`;
                } else {
                    caption = param.name;
                }

                const base = {
                    dataField: param.name,
                    caption: caption,
                    // caption: param.name,
                    dataType: 'number',
                    filterOperations: ['<', '>', '=', '<>'],
                    width: param.style ?? defaultWidthStyle,
                    calculateDisplayValue: (rowData: any) => {
                        let res = '';
                        if (typeof rowData[param.name] === 'number') {
                            res = this.formatNumber(rowData[param.name], rowData.dataUnitType);
                        } else {
                            const parsed = parseFloat(rowData[param.name]);
                            res = isNaN(parsed) ? rowData[param.name] : parsed.toFixed(2);
                        }
                        // ? this.formatNumber(rowData[param.name], rowData.dataUnitType)
                        // : rowData[param.name]

                        return res;
                    },
                    calculateFilterExpression: (filterValue: any, filterOperation: any) => [
                        param.name,
                        filterOperation,
                        filterValue,
                    ],
                    calculateSortValue: (rowData: any) =>
                        this.sortMode === 'color' ? rowData[param.name + '_severity'] : rowData[param.name],
                    sortOrder: this.sortMode === 'color' ? 'desc' : undefined,
                    sortIndex: this.sortMode === 'color' ? 0 : undefined,
                    cellTemplate: (container, options) => {
                        const field = options.column.dataField;
                        const value = options.text;
                        const settings = options.data?.advancedSettings?.[field];

                        const span = document.createElement('span');
                        span.innerText = value != null ? value.toString() : '';

                        const bg = getBackgroundColor(value, settings);
                        if (settings) {
                            const bg = getBackgroundColor(value, settings);
                            if (bg) {
                                container.style.backgroundColor = bg;
                            }
                        }
                        container.appendChild(span);
                    },
                };
                if (param.type === ColumnType.Event) {
                    return {
                        ...base,
                        calculateDisplayValue: (rowData: any) => {
                            const field = this.eventDisplayMode === 'duration' ? param.name + '_duration' : param.name;
                            return typeof rowData[field] === 'number'
                                ? this.formatNumber(rowData[field], rowData.dataUnitType)
                                : rowData[field];
                        },
                        calculateFilterExpression: (filterValue: any, filterOperation: any) => {
                            const field = this.eventDisplayMode === 'duration' ? param.name + '_duration' : param.name;
                            return [field, filterOperation, filterValue];
                        },
                    };
                }
                return base;
            }),
        ];
    }

    private compareValues(
        value: string,
        operator: string,
        filterValue: string,
        type: 'number' | 'duration' | 'string' = 'number',
    ): boolean {
        let left: number | string;
        let right: number | string;
        if (type === 'number') {
            left = parseFloat(value.replace(/,/g, ''));
            right = parseFloat(filterValue.replace(/,/g, ''));
        } else if (type === 'duration') {
            const parseDuration = (d: string) => {
                const parts = d.split(':').map(Number);
                if (parts.length === 3) {
                    return parts[0] * 3600 + parts[1] * 60 + parts[2];
                } else if (parts.length === 2) {
                    return parts[0] * 60 + parts[1];
                }
                return Number(parts[0]);
            };
            left = parseDuration(value);
            right = parseDuration(filterValue);
        } else {
            left = value;
            right = filterValue;
        }
        switch (operator) {
            case '<':
                return left < right;
            case '>':
                return left > right;
            case '==':
                return left === right;
            case '!=':
                return left !== right;
            case '<=':
                return left <= right;
            case '>=':
                return left >= right;
            default:
                return false;
        }
    }

    private formatRawData(i: ITableWidgetResponseItem) {
        let rawValue: string | number;
        if (
            i.missingBaseParameterInfo &&
            i.missingBaseParameterInfo.status !== PQZStatus.OK &&
            i.missingBaseParameterInfo.message
        ) {
            // … your existing “No data” message cleanup …
            rawValue = i.missingBaseParameterInfo.message;
        } else if (i.calculated !== undefined && i.calculated !== null) {
            const s = i.calculated.toString().replace(/Fake/g, '');
            // if dataUnitType is the raw numeric id:
            const unitId = typeof i.dataUnitType === 'number' ? i.dataUnitType : i.dataUnitType.id;

            rawValue =
                unitId === 18 || unitId === 19 // <-- “18” is your Percentage code
                    ? `${s}`
                    : parseFloat(s);
        }

        if (typeof rawValue === 'number') {
            rawValue = rawValue.toFixed(2);
        }

        return rawValue;
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

    prepareExcludedFlagged(excludeFlagged: ExcludeFlagged, selectedEvents: EventClass[]): EventClass[] {
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

    private getFeedersByTagModel(tag: any, allFeeders: FeederComponentInfo[]): FeederComponentInfo[] {
        if (!tag || !tag) {
            return [];
        }

        let result = [];

        for (let child of tag.children?.filter((c) => typeof c === 'object') ?? []) {
            if (child.type === 'Feeder') {
                if (!allFeeders.some((f) => f.id === child.id && f.componentId === child.parentKey)) {
                    continue;
                }
                result.push(
                    new FeederComponentInfo({
                        id: child.id,
                        name: child.label,
                        componentId: child.parentKey,
                        compName: '',
                    }),
                );
            } else {
                result.push(...this.getFeedersByTagModel(child, allFeeders));
            }
        }

        return result;
    }
}
function getBackgroundColor(value: number | null | undefined, settings: AdvancedSettingsConfig): string | null {
  if (!settings) return null;

  const isNoData = 
    value === null ||
    value === undefined ||
    (typeof value === 'string' && ['DB_OUT_OF_RANGE', 'NO_DATA', 'N/A', ''].includes((value as string).trim()));

  const {
    lowerLimit,
    upperLimit,
    okColor,
    noDataColor,
    showOkColor,
    showNoDataColor,
    colorScheme,
    gradientFromColor,
    gradientToColor,
    outOfLimitColor
  } = settings;

  if (isNoData && showNoDataColor && noDataColor) {
    return noDataColor;
  }

  if (
    showOkColor &&
    lowerLimit != null &&
    upperLimit != null &&
    value != null &&
    value >= lowerLimit &&
    value <= upperLimit &&
    okColor
  ) {
    return okColor;
  }

  if (colorScheme === ColorSchema.None) {
    return null;
  }

  if (
    colorScheme === ColorSchema.Gradient &&
    gradientFromColor &&
    gradientToColor &&
    lowerLimit != null &&
    upperLimit != null
  ) {
    const normalized = normalizeValue(value, lowerLimit, upperLimit);
    return interpolateColor(gradientFromColor, gradientToColor, normalized);
  }

  if (
    colorScheme === ColorSchema.OutOfLimit &&
    outOfLimitColor &&
    lowerLimit != null &&
    upperLimit != null &&
    (value < lowerLimit || value > upperLimit)
  ) {
    return outOfLimitColor;
  }

  return null;
}

function normalizeValue(value: number, min: number, max: number): number {
  if (max === min) return 1; 
  return (value - min) / (max - min); 
}


function interpolateColor(from: string, to: string, ratio: number): string {
  const f = hexToRgb(from);
  const t = hexToRgb(to);
  if (!f || !t) return from;

  const r = Math.round(lerp(f.r, t.r, ratio));
  const g = Math.round(lerp(f.g, t.g, ratio));
  const b = Math.round(lerp(f.b, t.b, ratio));

  const alpha = 0.6;
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function lerp(a: number, b: number, t: number): number {
  if (t < 0) return a;     
  if (t > 1.5) return b;  
  return a + (b - a) * Math.min(t, 1); 
}

function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
  const clean = hex.replace('#', '');
  const bigint = parseInt(clean, 16);
  if (clean.length === 6) {
    return {
      r: (bigint >> 16) & 255,
      g: (bigint >> 8) & 255,
      b: bigint & 255,
    };
  }
  return null;
}

interface TableWidgetConfigurationModel {
    components: ComponentsState;
    dateRange: DateRangeAndRefreshModelNew;
    parameters: WidgetParametersColumn[];
    designOptions: TableDesignOptions;
}
