import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { CommonModule } from '@node_modules/@angular/common';
import { DxTooltipModule } from 'devextreme-angular';
import { ListboxModule } from 'primeng/listbox';
import { UtilsModule } from '../../../../../shared/utils/utils.module';

@Component({
    selector: 'quantitySelector',
    standalone: true,
    imports: [FormsModule, ListboxModule, DxTooltipModule, CommonModule, UtilsModule],
    templateUrl: './quantity-selector.component.html',
    styleUrl: './quantity-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => QuantitySelectorComponent),
            multi: true,
        },
    ],
})
export class QuantitySelectorComponent implements ControlValueAccessor {
    @Input() multiple = false;
    @Input() showTooltip = false;
    uniqueTooltipId = `tooltip-quantity-${Math.random().toString(36).substr(2, 9)}`;
    quantityOptions = [];
    selectedQuantities: QuantityUnits | QuantityUnits[] = this.multiple ? [] : null;

    @Input()
    set allowedQuantities(value: QuantityUnits[]) {
        if (value) {
            this.updateQuantityOptions(value);
        }
    }

    updateQuantityOptions(newOptions: QuantityUnits[]): void {
        this.quantityOptions = newOptions.map(quantity => ({
            label: QuantityUnits[quantity],
            value: quantity,
        }));
    }

    writeValue(obj: QuantityUnits[]): void {
        this.selectedQuantities = obj;
    }

    onChange: any = () => {};
    onTouched: any = () => {};

    onQuantityChange() {
        this.onChange(this.selectedQuantities);
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
}
