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
    plainArgumentField: 'componentName' | 'eventName' = 'componentName';

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
        text: `${seriesName}: ${valueText}`,
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
        const state: DateRangeState = DateRangeState.fromJSON(this.barChartConfiguration.dateRange);
        const range: [DateTime, DateTime] = this._dateRangeService.getDateRangeFromState(state);

        var config = JSON.parse(this.barChartConfiguration.configuration);

        var seriesBy = this.mapSerieses(config.series[0]);
        var category = this.mapSerieses(config.xUnit);

        var componentsState: ComponentsState = JSON.parse(this.barChartConfiguration.components);
        let formattedFeeders = componentsState?.feeders?.map((f) => new FeederComponentInfo(f)) ?? [];
        let tableWidgetConfigurationComponents = componentsState?.components ?? [];
        let formattedComponents = tableWidgetConfigurationComponents
            .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
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
            startDate: range[0],
            endDate: range[1],
            userTimeZone: 1,
        });

        this._tenantDashboardService
            .pQSBarChartWidgetData(this.barChartRequest)
            .subscribe((response: BarChartResponse) => {
                // const comps = response.components;
                // if (this.barChartConfiguration.type === BarChartType.Plain) {
                //     this.dataSource = this.getPlainData(comps);
                // } else {
                //     this.dataSource = this.transformData(comps);
                // }
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

    private getPlainData(components: any[]): any[] {
        const totalComponents = components.length;
        const totalEvents = components.reduce((sum, c) => sum + (c.events?.length || 0), 0);

        if (totalComponents === 1 && totalEvents > 1) {
            this.plainArgumentField = 'eventName';
            return components[0].events.map((e) => ({
                eventName: e.name,
                eventCount: e.data,
            }));
        }

        this.plainArgumentField = 'componentName';
        return components.map((c) => ({
            componentName: c.name,
            eventCount: c.events[0]?.data ?? 0,
        }));
    }

    private transformData(components: any[]): any[] {
        const transformed: any[] = [];
        components.forEach((component) => {
            (component.events || []).forEach((event) => {
                transformed.push({
                    componentName: component.name,
                    eventName: event.name,
                    eventCount: event.data,
                });
            });
            if (!component.events?.length) {
                transformed.push({
                    componentName: component.name,
                    eventName: 'No Events',
                    eventCount: 0,
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
