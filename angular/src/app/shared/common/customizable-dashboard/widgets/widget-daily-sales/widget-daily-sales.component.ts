import { Component, OnInit, Injector, ElementRef } from '@angular/core';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';

class DailySalesLineChart extends DashboardChartBase {
    chartData: any[];
    scheme: any = {
        name: 'green',
        selectable: true,
        group: 'Ordinal',
        domain: ['#34bfa3'],
    };

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init(data) {
        this.chartData = [];
        for (let i = 0; i < data.length; i++) {
            this.chartData.push({
                name: i + 1,
                value: data[i],
            });
        }
    }

    reload() {
        this.showLoading();
        this._dashboardService.getDailySales().subscribe((result) => {
            this.init(result.dailySales);
            this.hideLoading();
        });
    }
}

@Component({
    selector: 'app-widget-daily-sales',
    templateUrl: './widget-daily-sales.component.html',
    styleUrls: ['./widget-daily-sales.component.css'],
})
export class WidgetDailySalesComponent extends WidgetComponentBaseComponent implements OnInit {
    dailySalesLineChart: DailySalesLineChart;

    constructor(injector: Injector, dateRangeService: DateRangeService, private _tenantdashboardService: TenantDashboardServiceProxy, private elementReference: ElementRef) {
        super(injector, elementReference, dateRangeService);
        this.dailySalesLineChart = new DailySalesLineChart(this._tenantdashboardService);
    }

    ngOnInit() {
        this.dailySalesLineChart.reload();
    }
}
