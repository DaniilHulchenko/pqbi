import { Component, OnInit, Injector, Inject, ElementRef } from '@angular/core';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetOnResizeEventHandler, WIDGETONRESIZEEVENTHANDLERTOKEN } from '../../customizable-dashboard.component';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';

class ProfitSharePieChart extends DashboardChartBase {
    chartData: any[] = [];
    scheme: any = {
        name: 'custom',
        selectable: true,
        group: 'Ordinal',
        domain: ['#00c5dc', '#ffb822', '#716aca'],
    };

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init(data: number[]) {
        let formattedData = [];
        for (let i = 0; i < data.length; i++) {
            formattedData.push({
                name: this.getChartItemName(i),
                value: data[i],
                color: this.scheme.domain[i],
            });
        }

        this.chartData = formattedData;
    }

    getChartItemName(index: number) {
        if (index === 0) {
            return 'Product Sales';
        }

        if (index === 1) {
            return 'Online Courses';
        }

        if (index === 2) {
            return 'Custom Development';
        }

        return 'Other';
    }

    reload() {
        this.showLoading();
        this._dashboardService.getProfitShare().subscribe((data) => {
            this.init(data.profitShares);
            this.hideLoading();
        });
    }
}

@Component({
    selector: 'app-widget-profit-share',
    templateUrl: './widget-profit-share.component.html',
    styleUrls: ['./widget-profit-share.component.css'],
})
export class WidgetProfitShareComponent extends WidgetComponentBaseComponent implements OnInit {
    profitSharePieChart: ProfitSharePieChart;

    constructor(
        injector: Injector,
        dateRangeService: DateRangeService,
        private _dashboardService: TenantDashboardServiceProxy,
        private elementReference: ElementRef,
        @Inject(WIDGETONRESIZEEVENTHANDLERTOKEN) private _widgetOnResizeEventHandler: WidgetOnResizeEventHandler
    ) {
        super(injector, elementReference, dateRangeService);

        this.profitSharePieChart = new ProfitSharePieChart(this._dashboardService);

        _widgetOnResizeEventHandler.onResize.subscribe(() => {
            this.profitSharePieChart.reload();
        });
    }

    ngOnInit() {
        this.profitSharePieChart.reload();
    }
}
