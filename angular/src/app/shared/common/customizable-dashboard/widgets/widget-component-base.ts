import { AppComponentBase } from '@shared/common/app-component-base';
import { OnDestroy, Injector, Component, OnInit, ElementRef } from '@angular/core';
import { timer, Subscription, of, switchMap } from 'rxjs';
import { CreateOrEditWidgetConfigurationDto, GetWidgetConfigurationForEditOutput, WidgetConfigurationsServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { WidgetUpdateModel } from '@app/shared/models/widget-update-model';

@Component({ template: '' })
export class WidgetComponentBaseComponent extends AppComponentBase implements OnInit, OnDestroy {
    delay = 300;
    timer: Subscription;
    editState = false;
    protected widgetConfigurationInDB: CreateOrEditWidgetConfigurationDto;
    protected isNew: boolean;
    protected isEditModalInitialized = false;
    protected _defaultWidgetName: string;

    private widgetConfigurationServiceProxy: WidgetConfigurationsServiceProxy;

    constructor(injector: Injector, protected elementRef: ElementRef, protected _dateRangeService: DateRangeService) {
        super(injector);
        this.widgetConfigurationServiceProxy = injector.get(WidgetConfigurationsServiceProxy);
    }

    ngOnInit(): void {
        this.isNew = this.elementRef.nativeElement.parentElement.dataset.isnew;

        this.widgetConfigurationInDB = new CreateOrEditWidgetConfigurationDto();
        this.widgetConfigurationInDB.id = this.elementRef.nativeElement.parentElement.dataset.id;
        this.widgetConfigurationInDB.widgetGuid = this.elementRef.nativeElement.parentElement.dataset.guid;
        this.widgetConfigurationInDB.name = this.elementRef.nativeElement.parentElement.dataset.displayname;
        this.widgetConfigurationInDB.configuration = this.elementRef.nativeElement.parentElement.dataset.configuration;

        if (this.isNew) {
            this.editState = true;
            this.widgetConfigurationInDB.name = this._defaultWidgetName;
        }

        abp.event.on('app.dashboardEdit.onEditStateChange', (editState) => {
            setTimeout(() => {this.editState = editState; this.isEditModalInitialized = false;},0);
        });

        this.refreshWidget();
    }

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

    //This method should be overriten for refreshing widget
    refreshWidget(){
    };

    ngOnDestroy(): void {
        if (this.timer && !this.timer.closed) {
            this.timer.unsubscribe();
        }
        super.ngOnDestroy();
    }
}
