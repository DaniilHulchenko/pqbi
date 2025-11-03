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
    standalone: true,
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
        this.fillUnitOptions();
        this.fillCustomUnitOptions();
    }

    get minCustomValue(): number {
        return this.selectedValue?.customUnit === RefreshSelectionCustomUnits.Sec ? 5 : 0;
    }

    get isRefreshRateValid(): boolean {
        return this.selectedValue && !!this.selectedValue.unit;
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
        this.selectedValue.unit = event;
        this.onChange(this.selectedValue.ToValueInSeconds());
    }

    writeValue(valueInSeconds: number): void {
        this.selectedValue = new RefreshWidgetSelectionModel(this.refreshWidgetUnitValuePipe, this.refreshWidgetCustomUnitValuePipe);
        this.selectedValue.FromValueInSeconds(valueInSeconds);

        if (this.selectedValue.unit !== RefreshSelectionUnits.NEVER) {
            this.isDisabled = false;
        }
    }

    fillUnitOptions() {
        this.unitOptions = [
            { value: RefreshSelectionUnits.NEVER, label: this.localizePipe.transform('Never') },
            { value: RefreshSelectionUnits.SEC5, label: this.localizePipe.transform('Sec5') },
            { value: RefreshSelectionUnits.SEC10, label: this.localizePipe.transform('Sec10') },
            { value: RefreshSelectionUnits.MIN1, label: this.localizePipe.transform('Min1') },
            { value: RefreshSelectionUnits.MIN5, label: this.localizePipe.transform('Min5') },
            { value: RefreshSelectionUnits.MIN10, label: this.localizePipe.transform('Min10') },
            { value: RefreshSelectionUnits.HOUR1, label: this.localizePipe.transform('Hour1') },
            { value: RefreshSelectionUnits.HOUR12, label: this.localizePipe.transform('Hour12') },
            { value: RefreshSelectionUnits.DAY1, label: this.localizePipe.transform('Day1') },
            { value: RefreshSelectionUnits.INTERVAL, label: this.localizePipe.transform('Interval') },
        ];
    }

    fillCustomUnitOptions() {
        this.customUnitOptions = [
            { value: RefreshSelectionCustomUnits.Sec, label: this.localizePipe.transform('Sec') },
            { value: RefreshSelectionCustomUnits.Min, label: this.localizePipe.transform('Min') },
            { value: RefreshSelectionCustomUnits.Hour, label: this.localizePipe.transform('Hour') },
            { value: RefreshSelectionCustomUnits.Day, label: this.localizePipe.transform('Day') },
        ];
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }
}

class RefreshWidgetSelectionModel {
    constructor(private refreshWidgetUnitValuePipe: RefreshWidgetUnitValuePipe, private refreshWidgetCustomUnitValuePipe: RefreshWidgetCustomUnitValuePipe) {
        if (!this.refreshWidgetUnitValuePipe) {
            this.refreshWidgetUnitValuePipe = new RefreshWidgetUnitValuePipe();
        }

        if (!this.refreshWidgetCustomUnitValuePipe) {
            this.refreshWidgetCustomUnitValuePipe = new RefreshWidgetCustomUnitValuePipe();
        }
    }

    unit: RefreshSelectionUnits;
    value: number;
    customUnit: RefreshSelectionCustomUnits;

    ToValueInSeconds(): number {
        if (this.unit === RefreshSelectionUnits.INTERVAL) {
            return this.value * this.refreshWidgetCustomUnitValuePipe.transform(this.customUnit);
        } else {
            return this.refreshWidgetUnitValuePipe.transform(this.unit);
        }
    }

    FromValueInSeconds(valueInSeconds: number) {
        const refreshWidgetUnitValuePipe = new RefreshWidgetUnitValuePipe();
        const refreshWidgetCustomUnitValuePipe = new RefreshWidgetCustomUnitValuePipe();
        let result = new RefreshWidgetSelectionModel(refreshWidgetUnitValuePipe, refreshWidgetCustomUnitValuePipe);

        if (valueInSeconds < 0) {
            result.unit = RefreshSelectionUnits.NEVER;
        } else if (valueInSeconds === 0) {
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Day) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Day);
            if (value === 1) {
                result.unit = RefreshSelectionUnits.DAY1;
            } else {
                result.unit = RefreshSelectionUnits.INTERVAL;
                result.customUnit = RefreshSelectionCustomUnits.Day;
                result.value = value;
            }
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Hour) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Hour);
            if (value === 1) {
                result.unit = RefreshSelectionUnits.HOUR1;
            } else if (value === 12) {
                result.unit = RefreshSelectionUnits.HOUR12;
            } else {
                result.unit = RefreshSelectionUnits.INTERVAL;
                result.customUnit = RefreshSelectionCustomUnits.Hour;
                result.value = value;
            }
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Min) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Min);
            if (value === 1) {
                result.unit = RefreshSelectionUnits.MIN1;
            } else if (value === 5) {
                result.unit = RefreshSelectionUnits.MIN5;
            } else if (value === 10) {
                result.unit = RefreshSelectionUnits.MIN10;
            } else {
                result.unit = RefreshSelectionUnits.INTERVAL;
                result.customUnit = RefreshSelectionCustomUnits.Min;
                result.value = value;
            }
        } else if (valueInSeconds % refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Sec) === 0) {
            let value = valueInSeconds / refreshWidgetCustomUnitValuePipe.transform(RefreshSelectionCustomUnits.Sec);
            if (value === 5) {
                result.unit = RefreshSelectionUnits.SEC5;
            } else if (value === 10) {
                result.unit = RefreshSelectionUnits.SEC10;
            } else {
                result.unit = RefreshSelectionUnits.INTERVAL;
                result.customUnit = RefreshSelectionCustomUnits.Sec;
                result.value = value;
            }
        }

        this.unit = result.unit;
        this.value = result.value;
        this.customUnit = result.customUnit;
    }
}
