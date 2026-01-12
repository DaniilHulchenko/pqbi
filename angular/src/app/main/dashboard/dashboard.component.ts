import { Component, Injector, ViewChild, ViewEncapsulation, OnInit, OnDestroy, HostListener } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardCustomizationConst } from '@app/shared/common/customizable-dashboard/DashboardCustomizationConsts';
import { CustomizableDashboardComponent } from '@app/shared/common/customizable-dashboard/customizable-dashboard.component';
import { EditModeService } from '@app/shared/services/edit-mode-service.service';

@Component({
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.less'],
    encapsulation: ViewEncapsulation.None,
})
export class DashboardComponent extends AppComponentBase implements OnInit, OnDestroy {
    @ViewChild(CustomizableDashboardComponent)
    private child: CustomizableDashboardComponent;
    dashboardName = DashboardCustomizationConst.dashboardNames.defaultTenantDashboard;
    initialPageName: string;

    constructor(
        injector: Injector,
        private route: ActivatedRoute,
        private editModeService: EditModeService
    ) {
        super(injector);
    }

    ngOnInit(): void {
        // Use snapshot for synchronous access to ensure initialPageName is set
        // before the child component initializes
        this.initialPageName = this.route.snapshot.params['pageName'];
    }

    @HostListener('window:beforeunload', ['$event'])
    beforeUnloadHandler(event: BeforeUnloadEvent): void {
        if (this.isEditModeEnabled()) {
            event.preventDefault();
        }
    }

    canDeactivate(): boolean {
        if (this.isEditModeEnabled()) {
          return confirm(this.l('UnsavedChangesWarning'));
        }
        return true;
    }

    private isEditModeEnabled(): boolean {
        if (this.child && this.child.editModeEnabled) {
            return true;
        }

        return this.editModeService.getEditModeValue();
    }
}
