import { Component, forwardRef, Input, Output, OnInit, EventEmitter } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import {
    CustomParameterDto,
    CustomParametersServiceProxy,
    PagedResultDtoOfGetCustomParameterForViewDto,
} from '@shared/service-proxies/service-proxies';
import { DxScrollViewModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { UtilsModule } from '../../../../../shared/utils/utils.module';

@Component({
    selector: 'customParameterSelector',
    standalone: true,
    imports: [FormsModule, DxScrollViewModule, ListboxModule, UtilsModule],
    templateUrl: './custom-parameter-selector.component.html',
    styleUrl: './custom-parameter-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => CustomParameterSelectorComponent),
            multi: true,
        },
    ],
})
export class CustomParameterSelectorComponent implements OnInit, ControlValueAccessor {
    @Input() height: string;
    @Input() customParameterTypes: string[] | undefined;
    @Output() customParameterChanged: EventEmitter<CustomParameterDto> = new EventEmitter();

    selectedCustomParameter: number;

    customParameters!: CustomParameterDto[];

    constructor(private _customParameterServiceProxy: CustomParametersServiceProxy) {}

    get scrollHeight() {
        return this.height ? `height: ${this.height};` : '';
    }

    ngOnInit(): void {
        this.customParameters = [];
        if (this.customParameterTypes) {
            for (let type of this.customParameterTypes) {
                this._customParameterServiceProxy
                    .getAll(undefined, undefined, undefined, type, undefined, undefined, undefined, undefined,0, 100)
                    .subscribe((result: PagedResultDtoOfGetCustomParameterForViewDto) => {
                        this.customParameters.push(...result.items.map((item) => item.customParameter));
                        this.customParameters = [...this.customParameters];
                        if(this.selectedCustomParameter) {
                            this.updateModelEmit()
                        }
                    });
            }
        } else {
            this._customParameterServiceProxy
                    .getAll(undefined, undefined, undefined, undefined, undefined, undefined, undefined,  undefined, 0, 100)
                    .subscribe((result: PagedResultDtoOfGetCustomParameterForViewDto) => {
                        this.customParameters = result.items.map((item) => item.customParameter);
                        if(this.selectedCustomParameter) {
                            this.updateModelEmit()
                        }
                    });
        }
    }

    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(obj: any): void {
        this.selectedCustomParameter = obj;
        this.updateModelEmit();
    }

    onSelectionChanged() {
        this.onChange(this.selectedCustomParameter);
        this.updateModelEmit();
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    private updateModelEmit() {
        const selectedCustomParameterModel = this.customParameters.find(p => p.id == this.selectedCustomParameter);
        if (selectedCustomParameterModel) {
            this.customParameterChanged.emit(selectedCustomParameterModel);
        }
    }
}
