import { AppComponentBase } from '@shared/common/app-component-base';
import { OnDestroy, Injector, Component, OnInit, ElementRef } from '@angular/core';
import { timer, Subscription, of, switchMap } from 'rxjs';
import { CreateOrEditWidgetConfigurationDto, GetWidgetConfigurationForEditOutput, WidgetConfigurationsServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { WidgetUpdateModel } from '@app/shared/models/widget-update-model';
import { DashboardConfigurationService } from '../dashboard-configuration.service';

@Component({ template: '' })
export class WidgetComponentBaseComponent extends AppComponentBase implements OnInit, OnDestroy {
    delay = 300;
    timer: Subscription;
    editState = false;
    protected widgetConfigurationInDB: CreateOrEditWidgetConfigurationDto;
    protected isNew: boolean;
    protected isEditModalInitialized = false;
    protected _defaultWidgetName: string;
    protected globalWidgetNameFontSize?: number;

    widgetNameFontSize?: string;

    private widgetConfigurationServiceProxy: WidgetConfigurationsServiceProxy;
    private dashboardConfigurationService: DashboardConfigurationService | null;
    private widgetNameFontSizeSubscription?: Subscription;

    constructor(injector: Injector, protected elementRef: ElementRef, protected _dateRangeService: DateRangeService) {
        super(injector);
        this.widgetConfigurationServiceProxy = injector.get(WidgetConfigurationsServiceProxy);
        this.dashboardConfigurationService = injector.get(DashboardConfigurationService, null);
    }

    ngOnInit(): void {
        this.isNew = this.elementRef.nativeElement.parentElement.dataset.isnew;

        this.widgetConfigurationInDB = new CreateOrEditWidgetConfigurationDto();
        this.widgetConfigurationInDB.id = this.elementRef.nativeElement.parentElement.dataset.id;
        this.widgetConfigurationInDB.widgetGuid = this.elementRef.nativeElement.parentElement.dataset.guid;
        this.widgetConfigurationInDB.name = this.elementRef.nativeElement.parentElement.dataset.displayname;
        this.widgetConfigurationInDB.configuration = this.elementRef.nativeElement.parentElement.dataset.configuration;
        this.initializeWidgetNameFontSize();

        if (this.isNew) {
            this.editState = true;
            this.widgetConfigurationInDB.name = this._defaultWidgetName;
        }

        abp.event.on('app.dashboardEdit.onEditStateChange', (editState) => {
            setTimeout(() => {this.editState = editState; this.isEditModalInitialized = false;},0);
        });
        this.registerDashboardActionHandlers();

        this.refreshWidget();
    }

        protected registerDashboardActionHandlers(): void {
        this.editRequestHandler = (payload: any) => {
            if (payload?.widgetGuid === this.widgetConfigurationInDB?.widgetGuid) {
                this.onEditRequested(payload);
            }
        };

        this.renameRequestHandler = (payload: any) => {
            if (payload?.widgetGuid === this.widgetConfigurationInDB?.widgetGuid) {
                this.onRenameRequested(payload);
            }
        };

        abp.event.on('app.dashboard.editWidgetRequested', this.editRequestHandler);
        abp.event.on('app.dashboard.renameWidgetRequested', this.renameRequestHandler);
    }

    protected onEditRequested(payload: any): void {}

    protected onRenameRequested(payload: any): void {}

    /**
     * Run methods delayed. If runDelay called multiple time before its delay, only run last called.
     * @param method Method to call
     */
    runDelayed(method: () => void) {
        if (this.timer && !this.timer.closed) {
            this.timer.unsubscribe();
        }

        this.timer = timer(this.delay).subscribe(() => {
            method();
        });
    }

    saveConfiguration(configuration: string){
        let requestBody: CreateOrEditWidgetConfigurationDto = new CreateOrEditWidgetConfigurationDto({
            id: this.widgetConfigurationInDB?.id,
            name: this.widgetConfigurationInDB?.name,
            widgetGuid: this.widgetConfigurationInDB.widgetGuid,
            configuration: configuration,
            lastModifiedOn: DateTime.now(),
        });

        of(requestBody)
            .pipe(
                // debounceTime(100),
                switchMap(() => this.widgetConfigurationServiceProxy.createOrEdit(requestBody)),
            )
            .subscribe((result: GetWidgetConfigurationForEditOutput) => {
                this.widgetConfigurationInDB = result.widgetConfiguration;
                this.refreshWidget();

                let event: WidgetUpdateModel = {
                    id: this.widgetConfigurationInDB.id,
                    name: this.widgetConfigurationInDB.name,
                    guid: this.widgetConfigurationInDB.widgetGuid,
                    configuration: this.widgetConfigurationInDB.configuration,
                };
                
                abp.event.trigger('app.dashboard.saveWidget', event);
            });
    }

    saveName(newName: string){
        this.widgetConfigurationInDB.name = newName;
        this.saveConfiguration(this.widgetConfigurationInDB.configuration);
        abp.event.trigger('app.dashboard.renameWidget', this.widgetConfigurationInDB.widgetGuid, newName);
    }

     protected resolveWidgetNameFontSize(localSize?: number, defaultSize?: string): string | undefined {
        if (this.globalWidgetNameFontSize) {
            return `${this.globalWidgetNameFontSize}px`;
        }

        if (localSize) {
            return `${localSize}px`;
        }

        return defaultSize;
    }

    //This method should be overriten for refreshing widget
    refreshWidget(){
    };

    ngOnDestroy(): void {
        if (this.widgetNameFontSizeSubscription && !this.widgetNameFontSizeSubscription.closed) {
            this.widgetNameFontSizeSubscription.unsubscribe();
        }
        if (this.timer && !this.timer.closed) {
            this.timer.unsubscribe();
        }
        abp.event.off('app.dashboard.editWidgetRequested', this.editRequestHandler);
        abp.event.off('app.dashboard.renameWidgetRequested', this.renameRequestHandler);
        super.ngOnDestroy();
    }
    private initializeWidgetNameFontSize(): void {
        const globalFontSize = this.elementRef.nativeElement.parentElement.dataset.widgetNameFontSize;
        this.globalWidgetNameFontSize = globalFontSize ? Number(globalFontSize) : undefined;
        this.widgetNameFontSize = this.resolveWidgetNameFontSize();

        if (this.dashboardConfigurationService) {
            this.widgetNameFontSizeSubscription = this.dashboardConfigurationService
                .getWidgetNameFontSize()
                .subscribe((widgetNameFontSize) => {
                    this.globalWidgetNameFontSize = widgetNameFontSize;
                    this.widgetNameFontSize = this.resolveWidgetNameFontSize();
                });
        }
    }
}
