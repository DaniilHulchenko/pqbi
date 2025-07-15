import { AppConsts } from '@shared/AppConsts';
import { Component, ViewChild, Injector, Output, EventEmitter } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { GetCustomParameterForViewDto, CustomParameterDto } from '@shared/service-proxies/service-proxies';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    selector: 'viewCustomParameterModal',
    templateUrl: './view-customParameter-modal.component.html',
})
export class ViewCustomParameterModalComponent extends AppComponentBase {
    @ViewChild('createOrEditModal', { static: true }) modal: ModalDirective;
    @Output() modalSave: EventEmitter<any> = new EventEmitter<any>();

    active = false;
    saving = false;

    item: GetCustomParameterForViewDto;

    constructor(injector: Injector) {
        super(injector);
        this.item = new GetCustomParameterForViewDto();
        this.item.customParameter = new CustomParameterDto();
    }

    show(item: GetCustomParameterForViewDto): void {
        this.item = item;
        this.active = true;
        this.modal.show();
    }

    close(): void {
        this.active = false;
        this.modal.hide();
    }
}
