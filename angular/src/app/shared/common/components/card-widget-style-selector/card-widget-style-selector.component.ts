import { Component, forwardRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { DxSelectBoxModule } from 'devextreme-angular';
import { UtilsModule } from '@shared/utils/utils.module';
import { CardWidgetStyleType } from '@shared/service-proxies/service-proxies';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'cardWidgetStyleSelector',
    standalone: true,
    imports: [ LocalizePipe, CommonModule, FormsModule, ReactiveFormsModule, DxSelectBoxModule, UtilsModule],
    templateUrl: './card-widget-style-selector.component.html',
    styleUrl: './card-widget-style-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => CardWidgetStyleSelectorComponent),
            multi: true,
        },
    ],
})
export class CardWidgetStyleSelectorComponent implements ControlValueAccessor, OnInit {
    selectedValue: CardWidgetStyleType;
    options: { value: CardWidgetStyleType; label: string }[] = [];

    onChange: any = () => {};
    onTouched: any = () => {};

    constructor(public localizePipe: LocalizePipe) {}

    ngOnInit(): void {
        this.setOptions();
    }

    writeValue(newValue: CardWidgetStyleType): void {
        this.selectedValue = newValue;
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    isStyleValid(): boolean {
        return !!this.selectedValue;
    }

    setOptions() {
        this.options = [
            { value: CardWidgetStyleType.RoundDial, label: this.localizePipe.transform('RoundDial') },
            { value: CardWidgetStyleType.ClassicBox, label: this.localizePipe.transform('ClassicBox') },
            { value: CardWidgetStyleType.CompactHorizontal, label: this.localizePipe.transform('CompactHorizontal') },
            { value: CardWidgetStyleType.IconTopValueBelow, label: this.localizePipe.transform('IconTopValueBelow') },
        ];
    }
}
