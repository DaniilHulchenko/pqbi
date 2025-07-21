import { Component, ElementRef, Injector, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import {
    BarChartResponse,
    BarChartType,
    BarChartWidgetConfigurationDto,
    BarChartWidgetConfigurationsServiceProxy,
    CreateOrEditBarChartWidgetConfigurationDto,
    TenantDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { CreateOrEditBarChartConfigurationComponent } from './create-or-edit-bar-chart-configuration/create-or-edit-bar-chart-configuration.component';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { DateTime } from 'luxon';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';

@Component({
    selector: 'widgetPqsBarChart',
    templateUrl: './widget-pqs-bar-chart.component.html',
    styleUrl: './widget-pqs-bar-chart.component.css',
})
export class WidgetPqsBarChartComponent extends WidgetComponentBaseComponent implements OnInit {
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditBarChartConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;

    barChartRequest: any;
    barChartConfiguration: BarChartWidgetConfigurationDto;
    barChartType = BarChartType;
    dataSource: any;
    plainArgumentField: 'componentName' | 'eventName' = 'componentName';

    constructor(
        injector: Injector,
        private _barChartWidgetConfigurationsServiceProxy: BarChartWidgetConfigurationsServiceProxy,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        elementReference: ElementRef,
        dateRangeService: DateRangeService
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

        this.barChartRequest = {
            config: undefined,
            components: JSON.parse(this.barChartConfiguration.components),
            startDate: range[0],
            endDate: range[1],
        };

        this._tenantDashboardService
            .pQSBarChartWidgetData(this.barChartRequest)
            .subscribe((response: BarChartResponse) => {
                const comps = response.components;
                if (this.barChartConfiguration.type === BarChartType.Plain) {
                    this.dataSource = this.getPlainData(comps);
                } else {
                    this.dataSource = this.transformData(comps);
                }
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
                .subscribe(result => {
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
            return components[0].events.map(e => ({
                eventName: e.name,
                eventCount: e.data
            }));
        }

        this.plainArgumentField = 'componentName';
        return components.map(c => ({
            componentName: c.name,
            eventCount: c.events[0]?.data ?? 0
        }));
    }

    private transformData(components: any[]): any[] {
        const transformed: any[] = [];
        components.forEach(component => {
            (component.events || []).forEach(event => {
                transformed.push({
                    componentName: component.name,
                    eventName: event.name,
                    eventCount: event.data
                });
            });
            if (!component.events?.length) {
                transformed.push({
                    componentName: component.name,
                    eventName: 'No Events',
                    eventCount: 0
                });
            }
        });
        return transformed;
    }
}
