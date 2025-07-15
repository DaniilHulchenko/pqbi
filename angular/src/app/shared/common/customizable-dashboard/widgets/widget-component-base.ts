import { AppComponentBase } from '@shared/common/app-component-base';
import { OnDestroy, Injector, Component, OnInit, ElementRef } from '@angular/core';
import { timer, Subscription, of, switchMap } from 'rxjs';
import { CreateOrEditWidgetConfigurationDto, GetWidgetConfigurationForEditOutput, WidgetConfigurationsServiceProxy } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { DateRangeService } from '@app/shared/services/date-range-service';

@Component({ template: '' })
export class WidgetComponentBaseComponent extends AppComponentBase implements OnInit, OnDestroy {
    delay = 300;
    timer: Subscription;
    editState = false;
    protected widgetConfigurationInDB: CreateOrEditWidgetConfigurationDto;
    protected widgetName: string;
    protected widgetGuid: string;
    protected isNew: boolean;
    protected _defaultWidgetName: string;

    private widgetConfigurationServiceProxy: WidgetConfigurationsServiceProxy;

    constructor(injector: Injector, protected elementRef: ElementRef, protected _dateRangeService: DateRangeService) {
        super(injector);
        this.widgetConfigurationServiceProxy = injector.get(WidgetConfigurationsServiceProxy);
    }

    ngOnInit(): void {
        this.widgetName = this.elementRef.nativeElement.parentElement.dataset.name;
        this.widgetGuid = this.elementRef.nativeElement.parentElement.dataset.guid;
        this.isNew = this.elementRef.nativeElement.parentElement.dataset.isnew;

        if (this.isNew) {
            this.editState = true;
            this.widgetName = this._defaultWidgetName;
        }

        abp.event.on('app.dashboardEdit.onEditStateChange', (editState) => {
            this.editState = editState;
        });

        this.widgetConfigurationServiceProxy
            .getWidgetConfigurationForEditByWidgetId(this.widgetGuid)
            .subscribe((result) => {
                if (result.widgetConfiguration) {
                    this.widgetConfigurationInDB = result.widgetConfiguration;
                } else {
                    this.widgetConfigurationInDB = new CreateOrEditWidgetConfigurationDto();
                    this.widgetConfigurationInDB.widgetGuid = this.widgetGuid;
                }

                if (!this.widgetConfigurationInDB.name) {
                    this.widgetConfigurationInDB.name = this._defaultWidgetName;
                    this.widgetName = this._defaultWidgetName;
                } else {
                    this.widgetName = this.widgetConfigurationInDB.name;
                }

                this.refreshWidget();
            });
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
            widgetGuid: this.widgetGuid,
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
            });
    }

    saveName(newName: string){
        this.widgetName = newName;
        this.widgetConfigurationInDB.name = newName;
        this.saveConfiguration(this.widgetConfigurationInDB.configuration);
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
