import { Component, forwardRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { DxDateBoxModule, DxNumberBoxModule, DxSelectBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { UtilsModule } from '@shared/utils/utils.module';
import { RefreshSelectionUnits } from '@app/shared/enums/refresh-selection-units';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { RefreshWidgetUnitValuePipe } from '@shared/common/pipes/refresh-widget-unit-value.pipe';
import { RefreshWidgetCustomUnitValuePipe } from '@shared/common/pipes/refresh-widget-custom-unit-value.pipe';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'widgetRefreshSelector',
    standalone: false,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DxDateBoxModule,
        DxTextBoxModule,
        DxSelectBoxModule,
        DxNumberBoxModule,
        UtilsModule,
    ],
    templateUrl: './widget-refresh-selector.component.html',
    styleUrl: './widget-refresh-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => WidgetRefreshSelectorComponent),
            multi: true,
        },
    ],
})
export class WidgetRefreshSelectorComponent implements ControlValueAccessor, OnInit {
    selectedValue: RefreshWidgetSelectionModel;

    refreshSelectionUnits = RefreshSelectionUnits;
    unitOptions: { value: RefreshSelectionUnits; label: string }[];
    customUnitOptions: { value: RefreshSelectionCustomUnits; label: string }[];

    isDisabled: boolean = false;

    onChange: any = () => {};
    onTouched: any = () => {};

    constructor(
        private localizePipe: LocalizePipe,
        private refreshWidgetUnitValuePipe: RefreshWidgetUnitValuePipe,
        private refreshWidgetCustomUnitValuePipe: RefreshWidgetCustomUnitValuePipe,
    ) {
        this.selectedValue = new RefreshWidgetSelectionModel(
            refreshWidgetUnitValuePipe,
            refreshWidgetCustomUnitValuePipe,
        );
    }

    ngOnInit(): void {
        this.fillCustomUnitOptions();
    }

    get minCustomValue(): number {
        return this.selectedValue?.customUnit === RefreshSelectionCustomUnits.Sec ? 5 : 0;
    }

    get isRefreshRateValid(): boolean {
        return !!this.selectedValue;
    }

    get isValueValid(): boolean {
        return this.selectedValue && !!this.selectedValue.value;
    }

    get isRefreshRateCustomUnitValid(): boolean {
        return this.selectedValue && !!this.selectedValue.customUnit;
    }

    invokeOnChange() {
        this.onChange(this.selectedValue.ToValueInSeconds());
    }

    onUnitChange(event) {
        if (event === RefreshSelectionUnits.NEVER || event === RefreshSelectionUnits.INTERVAL) {
            this.selectedValue.value = null;
            this.selectedValue.customUnit = null;
        }
        this.onChange(this.selectedValue.ToValueInSeconds());
    }

    writeValue(valueInSeconds: number): void {
        this.selectedValue = new RefreshWidgetSelectionModel(this.refreshWidgetUnitValuePipe, this.refreshWidgetCustomUnitValuePipe);
        this.selectedValue.FromValueInSeconds(valueInSeconds);
    }

    fillCustomUnitOptions() {
        this.customUnitOptions = [
            { value: RefreshSelectionCustomUnits.Sec, label: this.localizePipe.transform('Sec') },
            { value: RefreshSelectionCustomUnits.Min, label: this.localizePipe.transform('Min') },
            { value: RefreshSelectionCustomUnits.Hour, label: this.localizePipe.transform('Hour') },
            { value: RefreshSelectionCustomUnits.Day, label: this.localizePipe.transform('Days') },
            { value: RefreshSelectionCustomUnits.Week, label: this.localizePipe.transform('Week') },
            { value: RefreshSelectionCustomUnits.Month, label: this.localizePipe.transform('Month') },
            { value: RefreshSelectionCustomUnits.Year, label: this.localizePipe.transform('Year') },
        ];
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
}

export class RefreshWidgetSelectionModel {
    constructor(private refreshWidgetUnitValuePipe: RefreshWidgetUnitValuePipe, private refreshWidgetCustomUnitValuePipe: RefreshWidgetCustomUnitValuePipe) {
        if (!this.refreshWidgetUnitValuePipe) {
            this.refreshWidgetUnitValuePipe = new RefreshWidgetUnitValuePipe();
        }

        if (!this.refreshWidgetCustomUnitValuePipe) {
            this.refreshWidgetCustomUnitValuePipe = new RefreshWidgetCustomUnitValuePipe();
        }
    }

    value: number;
    customUnit: RefreshSelectionCustomUnits;

    ToValueInSeconds(): number {
        return this.value * this.refreshWidgetCustomUnitValuePipe.transform(this.customUnit);
    }

    FromValueInSeconds(valueInSeconds: number) {
        const refreshWidgetUnitValuePipe = new RefreshWidgetUnitValuePipe();
        const refreshWidgetCustomUnitValuePipe = new RefreshWidgetCustomUnitValuePipe();
        let result = new RefreshWidgetSelectionModel(refreshWidgetUnitValuePipe, refreshWidgetCustomUnitValuePipe);
        
        if (valueInSeconds <= 0) {
            this.customUnit = RefreshSelectionCustomUnits.Auto;
            this.value = 1;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Year) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Year);
            result.customUnit = RefreshSelectionCustomUnits.Year;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Month) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Month);
            result.customUnit = RefreshSelectionCustomUnits.Month;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Week) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Week);
            result.customUnit = RefreshSelectionCustomUnits.Week;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Day) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Day);
            result.customUnit = RefreshSelectionCustomUnits.Day;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Hour) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Hour);
            result.customUnit = RefreshSelectionCustomUnits.Hour;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Min) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Min);
            result.customUnit = RefreshSelectionCustomUnits.Min;
            result.value = value;
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Sec) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Sec);
            result.customUnit = RefreshSelectionCustomUnits.Sec;
            result.value = value;
        }

        this.value = result.value;
        this.customUnit = result.customUnit;
    }
}
