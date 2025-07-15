import { Component, OnInit, Injector, ElementRef } from '@angular/core';
import { DashboardChartBase } from '../dashboard-chart-base';
import { TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';

class MemberActivityTable extends DashboardChartBase {
    memberActivities: Array<any>;

    constructor(private _dashboardService: TenantDashboardServiceProxy) {
        super();
    }

    init() {
        this.reload();
    }

    reload() {
        this.showLoading();
        this._dashboardService.getMemberActivity().subscribe((result) => {
            this.memberActivities = result.memberActivities;
            this.hideLoading();
        });
    }
}

@Component({
    selector: 'app-widget-member-activity',
    templateUrl: './widget-member-activity.component.html',
    styleUrls: ['./widget-member-activity.component.css'],
})
export class WidgetMemberActivityComponent extends WidgetComponentBaseComponent implements OnInit {
    memberActivityTable: MemberActivityTable;

    constructor(injector: Injector, dateRangeService: DateRangeService, private _dashboardService: TenantDashboardServiceProxy, private elementReference: ElementRef) {
        super(injector, elementReference, dateRangeService);
        this.memberActivityTable = new MemberActivityTable(this._dashboardService);
    }

    ngOnInit() {
        this.memberActivityTable.init();
    }
}
