import { Component, Injector, ViewChild, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardCustomizationConst } from '@app/shared/common/customizable-dashboard/DashboardCustomizationConsts';
import { CustomizableDashboardComponent } from '@app/shared/common/customizable-dashboard/customizable-dashboard.component';

@Component({
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
})
export class DashboardComponent extends AppComponentBase {
    @ViewChild(CustomizableDashboardComponent)
    private child: CustomizableDashboardComponent;
    dashboardName = DashboardCustomizationConst.dashboardNames.defaultTenantDashboard;

    constructor(injector: Injector) {
        super(injector);
    }

    canDeactivate(): boolean {
        if (this.child.hasPendingChanges) {
          return confirm(this.l('UnsavedChangesWarning'));
        }
        return true;
    }
}
