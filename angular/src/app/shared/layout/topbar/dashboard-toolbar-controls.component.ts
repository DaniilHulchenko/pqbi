import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    HostBinding,
    Injector,
    OnDestroy,
    OnInit,
} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardToolbarService } from '@app/shared/services/dashboard-toolbar.service';
import {
    DashboardConfigurationService,
    DashboardConfigurationState,
} from '@app/shared/common/customizable-dashboard/dashboard-configuration.service';
import { EditModeService } from '@app/shared/services/edit-mode-service.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'dashboard-toolbar-controls',
    templateUrl: './dashboard-toolbar-controls.component.html',
    styleUrls: ['./dashboard-toolbar-controls.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardToolbarControlsComponent extends AppComponentBase implements OnInit, OnDestroy {
    @HostBinding('style.display') public display = 'flex';

    showControls = false;
    editModeEnabled = false;
    selectedWidgetNameFontSize?: number;
    backgroundColor = this.bgColor;

    readonly widgetNameFontSizes = [12, 14, 16, 18, 20, 22, 24, 26, 28, 30];
    readonly defaultWidgetTitleSize = 20;
    readonly widgetTitleSizeLabel: string;

    private readonly subscriptions = new Subscription();

    constructor(
        injector: Injector,
        private dashboardToolbarService: DashboardToolbarService,
        private dashboardConfigurationService: DashboardConfigurationService,
        private editModeService: EditModeService,
        private cdr: ChangeDetectorRef,
    ) {
        super(injector);
        this.widgetTitleSizeLabel = this.getWidgetTitleSizeLabel();
    }

    ngOnInit(): void {
        super.ngOnInit();
        const storedBackgroundColor = this.dashboardConfigurationService.getStoredBackgroundColor();
        if (storedBackgroundColor) {
            this.backgroundColor = storedBackgroundColor;
        }

        this.subscriptions.add(
            this.dashboardToolbarService.isDashboardActive().subscribe((isActive) => {
                this.showControls = isActive;
                this.cdr.markForCheck();
            }),
        );

        this.subscriptions.add(
            this.editModeService.getEditMode().subscribe((enabled) => {
                this.editModeEnabled = enabled;
                this.cdr.markForCheck();
            }),
        );

        this.subscriptions.add(
            this.dashboardConfigurationService.getConfiguration().subscribe((configuration) => {
                this.applyConfiguration(configuration);
            }),
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
        super.ngOnDestroy();
    }

    toggleEditMode(): void {
        if (!this.canEditDashboard()) {
            return;
        }

        this.editModeService.setEditMode(!this.editModeEnabled);
    }

    onWidgetNameFontSizeChange(size: number | undefined): void {
        this.dashboardConfigurationService.updateWidgetNameFontSize(size ? +size : undefined);
    }

    onBackgroundColorChange(color: string): void {
        this.dashboardConfigurationService.updateBackgroundColor(color || this.backgroundColor);
    }

    canEditDashboard(): boolean {
        return this.permission.isGranted('Pages.Tenant.Dashboard.Edit');
    }

    private applyConfiguration(configuration: DashboardConfigurationState): void {
        this.selectedWidgetNameFontSize = configuration.widgetNameFontSize;
        this.backgroundColor = configuration.backgroundColor || this.backgroundColor;
        this.cdr.markForCheck();
    }

    private getWidgetTitleSizeLabel(): string {
        const label = this.l('WidgetTitleSize');

        if (!label || label.startsWith('[') || label === 'WidgetTitleSize') {
            return 'Widget Title Size';
        }

        return label;
    }
}
