import { Component, OnInit, Injector, ElementRef } from '@angular/core';
import { DashboardChartBase } from '../dashboard-chart-base';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';

class GeneralStatsPieChart extends DashboardChartBase {
    public data = [];

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init(transactionPercent, newVisitPercent, bouncePercent) {
        this.data = [
            {
                name: 'Operations',
                value: transactionPercent,
            },
            {
                name: 'New Visits',
                value: newVisitPercent,
            },
            {
                name: 'Bounce',
                value: bouncePercent,
            },
        ];

        this.hideLoading();
    }

    reload() {
        this.showLoading();
        this._dashboardService.getGeneralStats().subscribe((result) => {
            this.init(result.transactionPercent, result.newVisitPercent, result.bouncePercent);
        });
    }
}
@Component({
    selector: 'app-widget-general-stats',
    templateUrl: './widget-general-stats.component.html',
    styleUrls: ['./widget-general-stats.component.css'],
})
export class WidgetGeneralStatsComponent extends WidgetComponentBaseComponent implements OnInit {
    generalStatsPieChart: GeneralStatsPieChart;
    constructor(injector: Injector, dateRangeService: DateRangeService, private _dashboardService: TenantDashboardServiceProxy, private elementReference: ElementRef) {
        super(injector, elementReference, dateRangeService);
        this.generalStatsPieChart = new GeneralStatsPieChart(this._dashboardService);
    }

    ngOnInit() {
        this.generalStatsPieChart.reload();
    }
}
