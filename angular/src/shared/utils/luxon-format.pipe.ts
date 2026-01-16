import { Pipe, PipeTransform } from '@angular/core';
import { DateTime } from 'luxon';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';

@Pipe({ name: 'luxonFormat' })
export class LuxonFormatPipe implements PipeTransform {
    constructor(private dateTimeService: DateTimeService) {}

    transform(value: DateTime, format: string) {
        if (!value) {
            return '';
        }

        return this.dateTimeService.formatWithDisplayLocale(value, format);
    }
}
