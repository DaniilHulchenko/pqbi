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

    barChartdConfiguration: BarChartWidgetConfigurationDto;
    barChartType = BarChartType;
    dataSource: any;

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
        if(this.isNew){
            this.runDelayed(() => this.edit());
        }
    }

    onNameEdit(){
        this.renameModal.show(this.widgetName);
    }

    fetch() {
        let dateRangeState: DateRangeState = DateRangeState.fromJSON(this.barChartdConfiguration.dateRange);
        let range: [DateTime, DateTime] = this._dateRangeService.getDateRangeFromState(dateRangeState);

        this.barChartRequest = {
            config: undefined,
            components: JSON.parse(this.barChartdConfiguration.components),
            events: JSON.parse(this.barChartdConfiguration.events),
            startDate: range[0],
            endDate: range[1],
        };

        this._tenantDashboardService
            .pQSBarChartWidgetData(this.barChartRequest)
            .subscribe((response: BarChartResponse) => {
                this.dataSource = this.transformData(response.components);
            });
    }

    edit() {
        this.createOrEditModal.show(this.widgetConfigurationInDB);
    }

    onConfigurationChange(newConfig: CreateOrEditBarChartWidgetConfigurationDto){
        this.saveConfiguration(newConfig.id.toString());
        this.refreshWidget();
    }

    refreshWidget(): void {
        if(this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration){
            this._barChartWidgetConfigurationsServiceProxy.getBarChartWidgetConfigurationForView(+this.widgetConfigurationInDB.configuration).subscribe(result => {
                this.barChartdConfiguration = result.barChartWidgetConfiguration;
                if(this.barChartdConfiguration){
                    this.fetch();
                }
            });
        }
    }

    convertEventsDataByType(type: BarChartType, events: any[]): any {
        switch (this.barChartdConfiguration.type) {
            case BarChartType.Plain:
                return events.reduce((acc, event) => acc + event.data, 0);
            case BarChartType.Clustered:
                return this.transformData(this.dataSource.components);
            case BarChartType.Stacked:
                return this.transformData(this.dataSource.components);
            default:
                break;
        }
    }

    private transformData(components: any[]): any[] {
        let transformedData: any[] = [];

        components.forEach(component => {
          component.events.forEach(event => {
            transformedData.push({
              componentName: component.name,
              eventName: event.name,
              eventCount: event.data
            });
          });

          // If a component has no events, add a zero count entry
          if (component.events.length === 0) {
            transformedData.push({
              componentName: component.name,
              eventName: 'No Events',
              eventCount: 0
            });
          }
        });

        return transformedData;
      }

}
