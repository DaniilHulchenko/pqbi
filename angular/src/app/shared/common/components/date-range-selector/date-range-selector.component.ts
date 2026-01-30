import { Component, forwardRef, OnInit, Injector, ChangeDetectorRef } from '@angular/core';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { RefreshWidgetCustomUnitValuePipe } from '@shared/common/pipes/refresh-widget-custom-unit-value.pipe';
import { RefreshWidgetUnitValuePipe } from '@shared/common/pipes/refresh-widget-unit-value.pipe';
import { DxRadioGroupModule, DxSelectBoxModule, DxNumberBoxModule, DxDateBoxModule } from 'devextreme-angular';
import { RefreshWidgetSelectionModel } from '../widget-refresh-selector/widget-refresh-selector.component';
import { UtilsModule } from "../../../../../shared/utils/utils.module";
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateRangeType } from '@app/shared/enums/date-range-type';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    selector: 'dateRangeSelector',
    standalone: false,
    imports: [
        DxRadioGroupModule,
        DxSelectBoxModule,
        DxNumberBoxModule,
        DxDateBoxModule,
        CommonModule,
        UtilsModule,
        FormsModule,
    ],
    templateUrl: './date-range-selector.component.html',
    styleUrl: './date-range-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DateRangeSelectorComponent),
            multi: true,
        },
    ],
})
export class DateRangeSelectorComponent extends AppComponentBase implements ControlValueAccessor, OnInit {
    customUnitOptions: { value: RefreshSelectionCustomUnits; label: string }[];
    dateRangeOptions: DateRangeType[];
    DateRangeType = DateRangeType;

    selectedMode: DateRangeType = DateRangeType.Relative;

    selectedRefreshValue: RefreshWidgetSelectionModel;

    refreshValue: number;
    refreshUnit: RefreshSelectionCustomUnits;

    relativeValue: number;
    relativeUnit: RefreshSelectionCustomUnits;

    fromDate;
    toDate;

    displayFormat: string = 'dd/MM/yyyy HH:mm';
    calendarOptions: { firstDayOfWeek?: number } = {};

    onChange: any = () => {};
    onTouched: any = () => {};

    private _defaultDateRangeSelectionState: DateRangeAndRefreshModelNew = new DateRangeAndRefreshModelNew(
        DateRangeType.Relative,
        30,
        RefreshSelectionCustomUnits.Day,
        null,
        null,
        60,
    );

    constructor(
        injector: Injector,
        private localizePipe: LocalizePipe,
        private refreshWidgetUnitValuePipe: RefreshWidgetUnitValuePipe,
        private refreshWidgetCustomUnitValuePipe: RefreshWidgetCustomUnitValuePipe,
        private cdr: ChangeDetectorRef,
    ) {
        super(injector);
        this.selectedRefreshValue = new RefreshWidgetSelectionModel(
            refreshWidgetUnitValuePipe,
            refreshWidgetCustomUnitValuePipe,
        );
    }

    get minCustomValue(): number {
        return this.selectedRefreshValue?.customUnit === RefreshSelectionCustomUnits.Sec ? 3 : 0;
    }

    get isValueValid(): boolean {
        return this.selectedRefreshValue && !!this.selectedRefreshValue.value;
    }

    get isRefreshRateCustomUnitValid(): boolean {
        return this.selectedRefreshValue && !!this.selectedRefreshValue.customUnit;
    }

    writeValue(value: DateRangeAndRefreshModelNew): void {
        let isInvoke = false;
        if (!value) {
            value = this._defaultDateRangeSelectionState;
            isInvoke = true;
        }

        this.selectedRefreshValue = new RefreshWidgetSelectionModel(
            this.refreshWidgetUnitValuePipe,
            this.refreshWidgetCustomUnitValuePipe,
        );
        this.selectedRefreshValue.FromValueInSeconds(value?.refreshIntervalInSeconds ?? 60);

        this.refreshValue = this.selectedRefreshValue.value;
        this.refreshUnit = this.selectedRefreshValue.customUnit;

        this.selectedMode = value.rangeUnit;
        this.relativeValue = value.relativeValue;
        this.relativeUnit = value.relativeUnit;
        this.fromDate = value.fromDate;
        this.toDate = value.toDate;

        if (isInvoke) {
            setTimeout(() => this.invokeOnChange(), 1000);
        }
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.fillDateRangeOptions();
        this.fillCustomUnitOptions();
        
        this.ensureDefaultValuesLoaded().subscribe(() => {
            this.displayFormat = this.getDevExtremeDateFormat();
            
            // Configure calendar first day of week
            const firstDayOfWeek = this.getFirstDayOfWeekNumber();
            if (firstDayOfWeek !== null) {
                this.calendarOptions = { firstDayOfWeek: firstDayOfWeek };
            } else {
                this.calendarOptions = {};
            }
            
            // Trigger change detection to update the view
            this.cdr.markForCheck();
        });
    }

    invokeRefreshChange() {
        this.selectedRefreshValue.value = this.refreshValue;
        this.selectedRefreshValue.customUnit = this.refreshUnit;
        this.invokeOnChange();
    }

    invokeOnChange() {
        var invoke = new DateRangeAndRefreshModelNew(
            this.selectedMode,
            this.selectedMode === DateRangeType.Relative ? this.relativeValue : null,
            this.selectedMode === DateRangeType.Relative ? this.relativeUnit : null,
            this.selectedMode === DateRangeType.Range ? this.fromDate : null,
            this.selectedMode === DateRangeType.Range ? this.toDate : null,
            this.selectedMode === DateRangeType.Relative 
                ? this.selectedRefreshValue?.ToValueInSeconds() ?? null
                : -1,
        );
        this.onChange(invoke);
    }

    isValid(): boolean {
        return this.selectedMode === DateRangeType.Range
            ? this.fromDate && this.toDate && this.fromDate < this.toDate
            : this.relativeValue > 0 && !!this.relativeUnit && this.refreshValue > 0 && !!this.refreshUnit;
    }

    private fillDateRangeOptions() {
        this.dateRangeOptions = [DateRangeType.Relative, DateRangeType.Range];
    }

    private fillCustomUnitOptions() {
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
}
