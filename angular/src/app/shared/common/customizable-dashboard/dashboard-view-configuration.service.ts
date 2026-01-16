import { Injectable, OnInit } from '@angular/core';
import { WidgetViewDefinition, WidgetFilterViewDefinition } from './definitions';
import { DashboardCustomizationConst } from './DashboardCustomizationConsts';
import { FilterDateRangePickerComponent } from './filters/filter-date-range-picker/filter-date-range-picker.component';
import { WidgetPQSComponent } from './widgets/widget-pqs/widget-pqs.component';
import { WidgetPQSTableComponent } from './widgets/widget-pqs-table/widget-pqs-table.component';
import { WidgetPqsBarChartComponent } from './widgets/widget-pqs-bar-chart/widget-pqs-bar-chart.component';
import { WidgetPqsCardComponent } from './widgets/widget-pqs-card/widget-pqs-card.component';
import { WidgetPqsGaugeComponent } from './widgets/widget-pqs-gauge/widget-pqs-gauge.component';

@Injectable({
    providedIn: 'root',
})
export class DashboardViewConfigurationService {
    public WidgetViewDefinitions: WidgetViewDefinition[] = [];
    public widgetFilterDefinitions: WidgetFilterViewDefinition[] = [];

    constructor() {
        this.initializeConfiguration();
    }

    private initializeConfiguration() {
        let filterDateRangePicker = new WidgetFilterViewDefinition(
            DashboardCustomizationConst.filters.filterDateRangePicker,
            FilterDateRangePickerComponent
        );
        //add your filters here
        this.widgetFilterDefinitions.push(filterDateRangePicker);

        let pqsTrend = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.PQSTrend,
            WidgetPQSComponent
        );

        let pqsBarChart = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.PQSBarChart,
            WidgetPqsBarChartComponent
        );

        let pqsTable = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.PQSTable,
            WidgetPQSTableComponent
        );

        let pqsCard = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.PQSCard,
            WidgetPqsCardComponent
        );

        let pqsGauge = new WidgetViewDefinition(
            DashboardCustomizationConst.widgets.tenant.PQSGauge,
            WidgetPqsGaugeComponent
        );

        //add your host side widgets here
        this.WidgetViewDefinitions.push(pqsTrend);
        this.WidgetViewDefinitions.push(pqsTable);
        this.WidgetViewDefinitions.push(pqsBarChart);
        this.WidgetViewDefinitions.push(pqsCard);
        this.WidgetViewDefinitions.push(pqsGauge);
    }
}
