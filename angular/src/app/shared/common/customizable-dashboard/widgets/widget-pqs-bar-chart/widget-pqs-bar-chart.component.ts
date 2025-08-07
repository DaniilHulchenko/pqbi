import { Component, ElementRef, Injector, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import {
    BarChartRequest,
    BarChartResponse,
    BarChartType,
    BarChartWidgetConfigurationDto,
    BarChartWidgetConfigurationsServiceProxy,
    BarParameter,
    CreateOrEditBarChartWidgetConfigurationDto,
    CustomWidgetTableData,
    DimensionSelector,
    DimensionType,
    EventClass,
    FeederComponentInfo,
    TableWidgetEvent,
    TenantDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { CreateOrEditBarChartConfigurationComponent } from './create-or-edit-bar-chart-configuration/create-or-edit-bar-chart-configuration.component';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { DateTime } from 'luxon';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { ComponentsState } from '@app/shared/models/components-state';
import { ColumnType } from '@app/shared/enums/column-type';
import safeStringify from 'fast-safe-stringify';
import { ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';


@Component({
    selector: 'widgetPqsBarChart',
    templateUrl: './widget-pqs-bar-chart.component.html',
    styleUrl: './widget-pqs-bar-chart.component.css',
})
export class WidgetPqsBarChartComponent extends WidgetComponentBaseComponent implements OnInit {
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditBarChartConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;

    barChartRequest: BarChartRequest;
    barChartConfiguration: BarChartWidgetConfigurationDto;
    barChartType = BarChartType;
    dataSource: any;

    constructor(
        injector: Injector,
        private _barChartWidgetConfigurationsServiceProxy: BarChartWidgetConfigurationsServiceProxy,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        elementReference: ElementRef,
        dateRangeService: DateRangeService,
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
        this.renameModal.show(this.widgetName);
    }

    fetch(): void {
        const state: DateRangeState = this.barChartConfiguration?.dateRange
            ? DateRangeState.fromJSON(this.barChartConfiguration.dateRange)
            : new DateRangeState({ rangeOption: DateRangeUnits.LAST_7_DAYS, startDate: null, endDate: null });

        let [startDate, endDate] = this._dateRangeService.getDateRangeFromState(state);

if (!startDate || !endDate || startDate >= endDate) {
            [startDate, endDate] = this._dateRangeService.getDateRangeFromUnit(DateRangeUnits.LAST_7_DAYS);
        }

        // Ensure UTC and log range
        startDate = startDate.toUTC();
        endDate = endDate.toUTC();
        console.log('Bar chart date range', {
            startDate: startDate.toISO(),
            endDate: endDate.toISO(),
        });

const config = JSON.parse(this.barChartConfiguration.configuration);

        const seriesBy = this.mapSerieses(config.series[0]);
        const category = this.mapSerieses(config.xUnit);

        const componentsState: ComponentsState = JSON.parse(this.barChartConfiguration.components);
        const formattedFeeders = componentsState?.feeders?.map((f) => new FeederComponentInfo(f)) ?? [];
        const tableWidgetConfigurationComponents = componentsState?.components ?? [];
        const formattedComponents = tableWidgetConfigurationComponents            .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
            .map((c) => new FeederComponentInfo({ componentId: c.key, id: null, name: null, compName: c.label }));

        this.barChartRequest = new BarChartRequest({
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
            widgetName: this.widgetName,
            startDate: startDate,
            endDate: endDate,
            userTimeZone: 1,
        });

        console.log('Bar chart request', this.barChartRequest);

        this._tenantDashboardService.pQSBarChartWidgetData(this.barChartRequest).subscribe({
            next: (response: BarChartResponse) => {
                console.log('Bar chart response', response);
                const groups = response?.groups || [];
                if (this.barChartConfiguration?.type === BarChartType.Plain) {
                    this.dataSource = this.getPlainData(groups);
                } else {
                    this.dataSource = this.transformData(groups);
                }
                console.log('Bar chart data source', this.dataSource);
            },
            error: (err) => {
                console.error('Bar chart data load failed', err);
            },
        });
    }

    edit(): void {
        this.createOrEditModal.show(this.widgetConfigurationInDB);
    }

    onConfigurationChange(newConfig: CreateOrEditBarChartWidgetConfigurationDto): void {
        this.saveConfiguration(newConfig.id.toString());
        this.refreshWidget();
    }

    refreshWidget(): void {
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            this._barChartWidgetConfigurationsServiceProxy
                .getBarChartWidgetConfigurationForView(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.barChartConfiguration = result.barChartWidgetConfiguration;
                    if (this.barChartConfiguration) {
                        this.fetch();
                    }
                });
        }
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
            quantity
        });

        return tableWidgetEvent;
    }
}
