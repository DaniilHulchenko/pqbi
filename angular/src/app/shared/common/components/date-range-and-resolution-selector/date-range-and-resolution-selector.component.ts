import { Component, forwardRef, Injector, OnInit, ChangeDetectorRef } from '@angular/core';
import { DateRangeType } from '@app/shared/enums/date-range-type';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { DxRadioGroupModule, DxSelectBoxModule, DxNumberBoxModule, DxDateBoxModule } from 'devextreme-angular';
import { UtilsModule } from '@shared/utils/utils.module';
import { DateRangeAndResolutionModel } from '@app/shared/models/date-range-and-resolution-model';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    selector: 'dateRangeAndResolutionSelector',
    //standalone: true
,
    imports: [
        DxRadioGroupModule,
        DxSelectBoxModule,
        DxNumberBoxModule,
        DxDateBoxModule,
        CommonModule,
        UtilsModule,
        FormsModule,
    ],
    templateUrl: './date-range-and-resolution-selector.component.html',
    styleUrl: './date-range-and-resolution-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DateRangeAndResolutionSelectorComponent),
            multi: true,
        },
    ],
})
export class DateRangeAndResolutionSelectorComponent extends AppComponentBase implements ControlValueAccessor, OnInit {
    customUnitOptions: { value: RefreshSelectionCustomUnits; label: string }[];
    dateRangeUnitOptions: { value: RefreshSelectionCustomUnits; label: string }[];
    dateRangeOptions: DateRangeType[];
    DateRangeType = DateRangeType;

    selectedMode: DateRangeType = DateRangeType.Relative;

    resolutionValue: number;
    resolutionUnit: RefreshSelectionCustomUnits;

    relativeValue: number;
    relativeUnit: RefreshSelectionCustomUnits;

    fromDate;
    toDate;

    onChange: any = () => {};
    onTouched: any = () => {};

    private _defaultDateRangeSelectionState: DateRangeAndResolutionModel = new DateRangeAndResolutionModel(
        DateRangeType.Relative,
        30,
        RefreshSelectionCustomUnits.Day,
        null,
        null,
        new ResolutionState({
            resolutionUnit: ResolutionUnits.CUSTOM,
            customResolutionUnit: CustomResolutionUnits.MIN,
            customResolutionValue: 1,
        }),
    );

    RefreshSelectionCustomUnits = RefreshSelectionCustomUnits;

    constructor(private localizePipe: LocalizePipe, injector: Injector, private cdr: ChangeDetectorRef) {
        super(injector);
    }

    get minCustomValue(): number {
        return this.resolutionUnit === RefreshSelectionCustomUnits.Sec ? 3 : 0;
    }

    get isValueValid(): boolean {
        return this.isAutoResolution || (!!this.resolutionValue && this.resolutionValue > 0);
    }

    get isRefreshRateCustomUnitValid(): boolean {
        return this.resolutionUnit !== null && this.resolutionUnit !== undefined;
    }

    writeValue(value: DateRangeAndResolutionModel): void {
        let isInvoke = false;
        if (!value) {
            value = this._defaultDateRangeSelectionState;
            isInvoke = true;
        }

        if (value.resolution?.resolutionUnit === ResolutionUnits.AUTO) {
            this.resolutionUnit = RefreshSelectionCustomUnits.Auto;
            this.resolutionValue = null;
        } else {
            this.resolutionUnit = this.mapResolutionUnitToRefreshUnit(value.resolution?.customResolutionUnit);
            this.resolutionValue = value.resolution?.customResolutionValue;
        }

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

    displayFormat: string = 'dd/MM/yyyy HH:mm';
    calendarOptions: { firstDayOfWeek?: number } = {};

    ngOnInit(): void {
        super.ngOnInit();
        this.fillDateRangeOptions();
        this.fillDateRangeUnitOptions();
        this.fillResolutionUnitOptions();
        
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

    invokeOnChange() {
        var resoution;

        if (this.isAutoResolution) {
            this.resolutionValue = null;
        }

        if (this.resolutionUnit === RefreshSelectionCustomUnits.Auto) {
            resoution = new ResolutionState({
                resolutionUnit: ResolutionUnits.AUTO,
            });
        } else {
            resoution = new ResolutionState({
                resolutionUnit: ResolutionUnits.CUSTOM,
                customResolutionUnit: this.mapRefreshUnitToResolutionUnit(this.resolutionUnit),
                customResolutionValue: this.resolutionValue,
            });
        }

        var invoke = new DateRangeAndResolutionModel(
            this.selectedMode,
            this.selectedMode === DateRangeType.Relative ? this.relativeValue : null,
            this.selectedMode === DateRangeType.Relative ? this.relativeUnit : null,
            this.selectedMode === DateRangeType.Range ? this.fromDate : null,
            this.selectedMode === DateRangeType.Range ? this.toDate : null,
            resoution,
        );
        this.onChange(invoke);
    }

    isValid(): boolean {
        const isResolutionValid =
            this.isAutoResolution || (this.resolutionValue > 0 && this.resolutionUnit !== null && this.resolutionUnit !== undefined);
        return this.selectedMode === DateRangeType.Range
            ? this.fromDate && this.toDate && this.fromDate < this.toDate && isResolutionValid
            : this.relativeValue > 0 && !!this.relativeUnit && isResolutionValid;
    }

    private get isAutoResolution(): boolean {
        return this.resolutionUnit === RefreshSelectionCustomUnits.Auto;
    }

    private fillDateRangeOptions() {
        this.dateRangeOptions = [DateRangeType.Relative, DateRangeType.Range];
    }

    private fillDateRangeUnitOptions() {
        this.dateRangeUnitOptions = [
            { value: RefreshSelectionCustomUnits.Sec, label: this.localizePipe.transform('Sec') },
            { value: RefreshSelectionCustomUnits.Min, label: this.localizePipe.transform('Min') },
            { value: RefreshSelectionCustomUnits.Hour, label: this.localizePipe.transform('Hour') },
            { value: RefreshSelectionCustomUnits.Day, label: this.localizePipe.transform('Days') },
            { value: RefreshSelectionCustomUnits.Week, label: this.localizePipe.transform('Week') },
            { value: RefreshSelectionCustomUnits.Month, label: this.localizePipe.transform('Month') },
            { value: RefreshSelectionCustomUnits.Year, label: this.localizePipe.transform('Year') },
        ];
    }

    private fillResolutionUnitOptions() {
        this.customUnitOptions = [
            { value: RefreshSelectionCustomUnits.Auto, label: this.localizePipe.transform('Auto') },
            { value: RefreshSelectionCustomUnits.Sec, label: this.localizePipe.transform('Sec') },
            { value: RefreshSelectionCustomUnits.Min, label: this.localizePipe.transform('Min') },
            { value: RefreshSelectionCustomUnits.Hour, label: this.localizePipe.transform('Hour') },
            { value: RefreshSelectionCustomUnits.Day, label: this.localizePipe.transform('Days') },
            { value: RefreshSelectionCustomUnits.Week, label: this.localizePipe.transform('Week') },
            { value: RefreshSelectionCustomUnits.Month, label: this.localizePipe.transform('Month') },
            { value: RefreshSelectionCustomUnits.Year, label: this.localizePipe.transform('Year') },
        ];
    }

    private mapResolutionUnitToRefreshUnit(unit: CustomResolutionUnits): RefreshSelectionCustomUnits {
        switch (unit) {
            case CustomResolutionUnits.SEC:
                return RefreshSelectionCustomUnits.Sec;
            case CustomResolutionUnits.MIN:
                return RefreshSelectionCustomUnits.Min;
            case CustomResolutionUnits.HOUR:
                return RefreshSelectionCustomUnits.Hour;
            case CustomResolutionUnits.DAY:
                return RefreshSelectionCustomUnits.Day;
            case CustomResolutionUnits.WEEK:
                return RefreshSelectionCustomUnits.Week;
            case CustomResolutionUnits.MONTH:
                return RefreshSelectionCustomUnits.Month;
            case CustomResolutionUnits.YEAR:
                return RefreshSelectionCustomUnits.Year;
            default:
                return RefreshSelectionCustomUnits.Auto;
        }
    }

    private mapRefreshUnitToResolutionUnit(unit: RefreshSelectionCustomUnits): CustomResolutionUnits {
        switch (unit) {
            case RefreshSelectionCustomUnits.Sec:
                return CustomResolutionUnits.SEC;
            case RefreshSelectionCustomUnits.Min:
                return CustomResolutionUnits.MIN;
            case RefreshSelectionCustomUnits.Hour:
                return CustomResolutionUnits.HOUR;
            case RefreshSelectionCustomUnits.Day:
                return CustomResolutionUnits.DAY;
            case RefreshSelectionCustomUnits.Week:
                return CustomResolutionUnits.WEEK;
            case RefreshSelectionCustomUnits.Month:
                return CustomResolutionUnits.MONTH;
            case RefreshSelectionCustomUnits.Year:
                return CustomResolutionUnits.YEAR;
            default:
                return CustomResolutionUnits.SEC;
        }
    }
}
