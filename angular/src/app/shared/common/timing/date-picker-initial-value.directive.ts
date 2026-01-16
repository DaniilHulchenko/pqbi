import { AfterViewInit, Directive, ElementRef, EventEmitter, Injector, Input, Output } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

@Directive({
    selector: '[datePickerInitialValue]',
})
export class DatePickerInitialValueSetterDirective implements AfterViewInit {
    @Input() ngModel;
    hostElement: ElementRef;

    constructor(injector: Injector, private _element: ElementRef, private _dateTimeService: DateTimeService) {
        this.hostElement = _element;
    }

    ngAfterViewInit(): void {
        if (this.ngModel) {
            setTimeout(() => {
                (this.hostElement.nativeElement as any).value = this._dateTimeService.formatDate(this.ngModel, 'D');
            });
        }
    }
}
