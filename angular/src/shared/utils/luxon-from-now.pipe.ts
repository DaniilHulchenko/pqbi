import { Pipe, PipeTransform } from '@angular/core';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

@Pipe({ name: 'luxonFromNow' })
export class LuxonFromNowPipe implements PipeTransform {
    constructor(private dateTimeService: DateTimeService) {}

    transform(value: DateTime) {
        if (!value) {
            return '';
        }

        return this.dateTimeService.fromNow(value);
    }
}
