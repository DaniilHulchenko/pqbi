import { Component, forwardRef, Input, OnInit } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { EventClassDescription, PQSRestApiServiceProxy } from '@shared/service-proxies/service-proxies';
import { DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { UtilsModule } from '../../../../../shared/utils/utils.module';

@Component({
    selector: 'eventSelector',
    standalone: true,
    imports: [DxScrollViewModule, FormsModule, ListboxModule, UtilsModule],
    templateUrl: './event-selector.component.html',
    styleUrl: './event-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => EventSelectorComponent),
            multi: true,
        },
    ],
})
export class EventSelectorComponent implements ControlValueAccessor, OnInit {
    @Input() height: string;

    selectedEvent: EventClassDescription;
    events: EventClassDescription[] = [];

    constructor(private _pqsRestApiServiceProxy: PQSRestApiServiceProxy) {}

    get scrollHeight() {
        return this.height ? `height: ${this.height};` : '';
    }

    ngOnInit(): void {
        this._pqsRestApiServiceProxy.pQSEvents().subscribe((result: EventClassDescription[]) => {
            this.events = result;
        });
    }

    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(obj: EventClassDescription): void {
        this.selectedEvent = obj;
    }

    onSelectionChange() {
        this.onChange(this.selectedEvent);
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
}
