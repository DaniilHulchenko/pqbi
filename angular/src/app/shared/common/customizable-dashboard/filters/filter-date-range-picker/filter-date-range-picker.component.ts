import { Component, Injector, Input, Output, EventEmitter } from '@angular/core';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DateTime } from 'luxon';

@Component({
    selector: 'app-filter-date-range-picker',
    templateUrl: './filter-date-range-picker.component.html',
    styleUrls: ['./filter-date-range-picker.component.css'],
})
export class FilterDateRangePickerComponent extends AppComponentBase {
    @Input() selectedDateRange: DateTime[];

    constructor(injector: Injector, private _dateTimeService: DateTimeService) {
        super(injector);
    }

    onChange() {
        abp.event.trigger('app.dashboardFilters.dateRangePicker.onDateChange', this.selectedDateRange);
    }
}
