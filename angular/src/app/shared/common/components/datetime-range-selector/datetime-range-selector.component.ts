import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, Input } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { DateTime } from 'luxon';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { DxSelectBoxModule, DxDateBoxModule } from 'devextreme-angular';
import { map } from 'rxjs';
import { KeyValuePair } from '@app/shared/models/key-value-pair';
import { key } from 'localforage';
import { UtilsModule } from '../../../../../shared/utils/utils.module';

@Component({
    selector: 'datetimeRangeSelector',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DxDateBoxModule, DxSelectBoxModule, UtilsModule],
    templateUrl: './datetime-range-selector.component.html',
    styleUrls: ['./datetime-range-selector.component.css'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DatetimeRangeSelectorComponent),
            multi: true,
        },
    ],
})
export class DatetimeRangeSelectorComponent implements ControlValueAccessor {
    @Input() isVertical = false;
    state: DateRangeState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null });
    selectOptions: KeyValuePair<string, DateRangeUnits>[] = Object.entries(DateRangeUnits).map((obj) => new KeyValuePair(obj[0], obj[1]));
    startDate: Date;
    endDate: Date;
    dateRangeUnits = DateRangeUnits;

    constructor(private fb: FormBuilder) {}

    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(value: DateRangeState): void {
        if (value) {
            this.state.rangeOption = value.rangeOption;
            this.state.startDate = value.startDate;
            this.state.endDate = value.endDate;
            this.startDate = value.startDate?.toJSDate();
            this.endDate = value.endDate?.toJSDate();
        }
    }

    isTimeIntervalValid(): boolean {
        return !!this.state.rangeOption;
    }

    isCustomDateRangeValid(): boolean {
        return (
            this.state.rangeOption === this.dateRangeUnits.CUSTOM &&
            this.state.startDate &&
            this.state.endDate &&
            this.state.startDate < this.state.endDate
        );
    }

    isValid(): boolean {
        return this.isTimeIntervalValid() && (this.state.rangeOption !== this.dateRangeUnits.CUSTOM || this.isCustomDateRangeValid());
    }


    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    onStartDateChange(date: Date) {
        this.state.startDate = DateTime.fromJSDate(date);
        this.onDateTimeRangeChange();
    }

    onEndDateChange(date: Date) {
        this.state.endDate = DateTime.fromJSDate(date);
        this.onDateTimeRangeChange();
    }

    onDateTimeRangeChange(): void {
        this.onChange(this.state);
        this.onTouched();
    }

    onQuickSelectChange(newValue: DateRangeUnits): void {
        if (
            DateRangeUnits[this.state.rangeOption] === DateRangeUnits.CUSTOM &&
            DateRangeUnits[newValue] !== DateRangeUnits.CUSTOM
        ) {
            this.state.startDate = null;
            this.state.endDate = null;
        }
        this.state.rangeOption = newValue;
        this.onChange(this.state);
    }
}
