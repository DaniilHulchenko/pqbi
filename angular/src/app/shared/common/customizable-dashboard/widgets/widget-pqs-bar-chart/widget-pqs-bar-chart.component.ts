import { Component, ElementRef, Injector, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import {
    BarChartRequest,
    BarChartResponse,
    BarChartType,
    BarChartWidgetConfigurationDto,
    BarParameter,
    CreateOrEditBarChartWidgetConfigurationDto,
    CustomWidgetTableData,
    DataUnitType,
    DimensionSelector,
    DimensionType,
    EventClass,
    FeederComponentInfo,
    GroupsServiceProxy,
    TableWidgetEvent,
    TenantDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { CreateOrEditBarChartConfigurationComponent } from './create-or-edit-bar-chart-configuration/create-or-edit-bar-chart-configuration.component';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { ComponentsState } from '@app/shared/models/components-state';
import { ColumnType } from '@app/shared/enums/column-type';
import safeStringify from 'fast-safe-stringify';
import { ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { Guid } from 'guid-ts';
import { Observable, of, forkJoin, Subscription, Subject, timer } from 'rxjs';
import { catchError, map, takeUntil } from 'rxjs/operators';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { BarchartWidgetConfigurationService } from '@app/shared/services/widget-configurations/barchart-widget-configuration.service';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';


@Component({
    selector: 'widgetPqsBarChart',
    templateUrl: './widget-pqs-bar-chart.component.html',
    styleUrl: './widget-pqs-bar-chart.component.css',
})
export class WidgetPqsBarChartComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy {
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditBarChartConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;

    barChartRequest: BarChartRequest;
    barChartConfiguration: BarChartWidgetConfigurationDto;
    barChartType = BarChartType;
    dataSource: any;
    dataUnitType: DataUnitType;
    refreshRate: number | null = null;

    private subs: Subscription[] = [];
    private stopStream$ = new Subject();

    constructor(
        injector: Injector,
        private _barChartWidgetConfigurationService: BarchartWidgetConfigurationService,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _groupService: GroupsServiceProxy,
        elementReference: ElementRef,
        dateRangeService: DateRangeService,
        private _configurationVersionService: ConfigurationVersionService,
        private _dateTimeService: DateTimeService,

    ) {
        super(injector, elementReference, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSBarChart');
    }

    customizeTooltip = ({ valueText, seriesName }) => ({
        text: seriesName ? `${seriesName}: ${valueText}` : `${valueText}`,
    });

    ngOnInit(): void {
        super.ngOnInit();
        if (this.isNew) {
            this.runDelayed(() => this.edit());
        }
    }

    onNameEdit(): void {
        this.renameModal.show(this.widgetConfigurationInDB?.name);
    }

    customizeLabelText = (e: any) => {
        if (this.dataUnitType && this.dataUnitType.id) {
            const token = this.getToken(this.dataUnitType);
            return `${e.valueText} ${token}`;
        }
        return e.valueText;
    };

    getToken(dataUnitType: DataUnitType): string {
        return dataUnitType?.id !== 41 && dataUnitType?.id !== 255 && dataUnitType?.tokenCode
            ? this.l(dataUnitType.tokenCode)
            : '';
    }

    fetch(): void {
        const config = JSON.parse(this.barChartConfiguration.configuration);

        var sub = forkJoin({
            seriesBy: this.mapSerieses(config.series),
            category: this.mapSerieses(config.xUnit),
        }).subscribe(({ seriesBy, category }) => {
            const componentsState: ComponentsState = JSON.parse(this.barChartConfiguration.components);
            const formattedFeeders =
                componentsState?.feeders?.map((f) => {
                    const c = componentsState?.components?.find((c) => c.key === f.componentId);
                    return new FeederComponentInfo({
                        componentId: f.componentId,
                        id: f.id,
                        name: f.name,
                        compName: c.label,
                    });
                }) ?? [];
            const tableWidgetConfigurationComponents = componentsState?.components ?? [];
            const formattedComponents = tableWidgetConfigurationComponents
                .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
                .map((c) => new FeederComponentInfo({ componentId: c.key, id: null, name: null, compName: c.label }));

            this.barChartRequest = new BarChartRequest({
                seriesBy: seriesBy,
                category: category,
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
                            // optionally handle unknown types
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
                widgetName: this.widgetConfigurationInDB?.name,
                startDate: null,
                endDate: null,
                userTimeZone: this._dateTimeService.getUserTimeZoneName(),
                refreshRateInSeconds: 0,
                isRealTime: false,
            });

            this.refreshRate = DateRangeAndRefreshModelNew.getRefreshIntervalInSecondsFromJson(this.barChartConfiguration?.dateRange);

            if (this.refreshRate && this.refreshRate !== -1) {
                var subTimer = timer(0, this.refreshRate * 1000)
                    .pipe(takeUntil(this.stopStream$))
                    .subscribe(() => {
                        const range = this.prepareDataRange();
                        this.barChartRequest.startDate = range[0];
                        this.barChartRequest.endDate = range[1];
                        this.barChartRequest.refreshRateInSeconds = this.refreshRate;
                        this.barChartRequest.isRealTime = true;
                        
                        var sub = this._tenantDashboardService.pQSBarChartWidgetData(this.barChartRequest).subscribe({
                            next: (response: BarChartResponse) => {
                                const groups = response?.groups || [];
                                this.dataUnitType = response?.dataUnitType;
                                if (this.barChartConfiguration?.type === BarChartType.Plain) {
                                    this.dataSource = this.getPlainData(groups);
                                } else {
                                    this.dataSource = this.transformData(groups);
                                }
                            },
                            error: (err) => {
                                console.error('Bar chart data load failed', err);
                            },
                        });
                        this.subs.push(sub);
                    });
                this.subs.push(subTimer);
            } else {
                const range = this.prepareDataRange();
                this.barChartRequest.startDate = range[0];
                this.barChartRequest.endDate = range[1];
                this.barChartRequest.refreshRateInSeconds = 0;
                this.barChartRequest.isRealTime = false;
                var sub = this._tenantDashboardService.pQSBarChartWidgetData(this.barChartRequest).subscribe({
                    next: (response: BarChartResponse) => {
                        const groups = response?.groups || [];
                        this.dataUnitType = response?.dataUnitType;
                        if (this.barChartConfiguration?.type === BarChartType.Plain) {
                            this.dataSource = this.getPlainData(groups);
                        } else {
                            this.dataSource = this.transformData(groups);
                        }
                    },
                    error: (err) => {
                        console.error('Bar chart data load failed', err);
                    },
                });
                this.subs.push(sub);
            }
        });
        this.subs.push(sub);
    }

    edit(): void {
        this.isEditModalInitialized = true;
        var sub = this._configurationVersionService.refreshVersion().subscribe(() => {
            setTimeout(() => {
                this.createOrEditModal.show(this.widgetConfigurationInDB);
            }, 200);
        });
        this.subs.push(sub);
    }

    onEditModelClose(isSaved) {
        if (!isSaved && !this.widgetConfigurationInDB?.configuration) {
            abp.event.trigger(
                'app.dashboard.removeWidget',
                this.widgetConfigurationInDB.widgetGuid,
                'Widgets_Tenant_PQSBarChart',
            );
        }
    }

    onConfigurationChange(newConfig: CreateOrEditBarChartWidgetConfigurationDto): void {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        
        if (newConfig.id.toString() !== this.widgetConfigurationInDB?.configuration) {
            this.saveConfiguration(newConfig.id.toString());
        } else {
            this.refreshWidget();
        }
    }

    refreshWidget(): void {
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this._barChartWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.barChartConfiguration = result.barChartWidgetConfiguration;
                    if (this.barChartConfiguration) {
                        this.fetch();
                    }
                });
            this.subs.push(sub);
        }
    }

    private prepareDataRange(): [DateTime, DateTime] {
        const state: DateRangeAndRefreshModelNew = this.barChartConfiguration?.dateRange
            ? DateRangeAndRefreshModelNew.createItem(this.barChartConfiguration.dateRange)
            : DateRangeAndRefreshModelNew.createItem('');
        var [startDate, endDate] = this._dateRangeService.getDateRangeFromNewState(state);

        return [DateTime.fromJSDate(startDate), DateTime.fromJSDate(endDate)];
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

    private mapSerieses(seriesBy: string): Observable<DimensionSelector> {
        const result = new DimensionSelector({
            name: '',
            type: null,
            id: null,
        });

        if (Guid.isValid(seriesBy)) {
            result.type = DimensionType.CustomGroup;
            result.id = seriesBy;
            return this._groupService.getGroupForView(seriesBy).pipe(
                map((response) => {
                    if (response.group) {
                        result.name = response.group.name;
                    } else {
                        result.name = 'Unknown Group';
                    }
                    return result;
                }),
                catchError(() => {
                    result.name = 'Unknown Group';
                    return of(result);
                }),
            );
        } else {
            switch (seriesBy) {
                case 'components':
                    result.type = DimensionType.Feeders;
                    break;
                case 'parameters':
                    result.type = DimensionType.Parameters;
                    break;
                case 'time':
                    result.type = DimensionType.Dates;
                    break;
            }
            return of(result);
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
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.subs.forEach((sub) => sub.unsubscribe());
    }
}
