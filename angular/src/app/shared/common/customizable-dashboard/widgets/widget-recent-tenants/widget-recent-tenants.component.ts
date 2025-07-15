import { Component, ViewChild, Injector, ElementRef } from '@angular/core';
import { Table } from 'primeng/table';
import { HostDashboardServiceProxy, GetRecentTenantsOutput } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';

@Component({
    selector: 'app-widget-recent-tenants',
    templateUrl: './widget-recent-tenants.component.html',
    styleUrls: ['./widget-recent-tenants.component.css'],
})
export class WidgetRecentTenantsComponent extends WidgetComponentBaseComponent {
    @ViewChild('RecentTenantsTable', { static: true }) recentTenantsTable: Table;

    loading = true;
    recentTenantsData: GetRecentTenantsOutput;

    constructor(injector: Injector, dateRangeService: DateRangeService, private _hostDashboardServiceProxy: HostDashboardServiceProxy, private elementReference: ElementRef) {
        super(injector, elementReference, dateRangeService);
        this.loadRecentTenantsData();
    }

    loadRecentTenantsData() {
        this._hostDashboardServiceProxy.getRecentTenantsData().subscribe((data) => {
            this.recentTenantsData = data;
            this.loading = false;
        });
    }

    gotoAllRecentTenants(): void {
        window.open(
            abp.appPath +
                'app/admin/tenants?' +
                'creationDateStart=' +
                encodeURIComponent(this.recentTenantsData.tenantCreationStartDate.toString())
        );
    }
}
