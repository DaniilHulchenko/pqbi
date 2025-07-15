import { Injectable } from '@angular/core';
import { DateRangeUnits } from '../enums/date-range-selection-units';
import { DateTime, Duration } from 'luxon';
import { IDateRangeState } from '../interfaces/date-range-state';

@Injectable({
    providedIn: 'root',
})
export class DateRangeService {
    getDateRangeFromUnit(unit: DateRangeUnits, resolutionMinutes: number = null): [DateTime, DateTime] {
        const now = DateTime.local();
        let start: DateTime, end: DateTime;

        switch (unit) {
            case DateRangeUnits.THIS_MONTH:
                start = now.startOf('month');
                end = now;
                break;
            case DateRangeUnits.THIS_WEEK:
                start = now.startOf('week');
                end = now;
                break;
            case DateRangeUnits.TODAY:
                start = now.startOf('day');
                end = now.startOf('second');
                break;
            case DateRangeUnits.LAST_30_DAYS:
                start = now.minus({ days: 30 }).startOf('day').startOf('second');
                end = now.startOf('day').startOf('second');
                break;
            case DateRangeUnits.LAST_7_DAYS:
                start = now.minus({ days: 7 }).startOf('day').startOf('second');
                end = now.startOf('day').startOf('second');
                break;
            case DateRangeUnits.LAST_24_HOURS:
                start = now.minus({ hours: 24 }).startOf('second');
                end = now.startOf('second');
                break;
            case DateRangeUnits.LAST_HOUR:
                start = now.minus({ hours: 1 }).startOf('second');
                end = now.startOf('second');
                break;
            default:
                start = null;
                end = null;
        }

        if (resolutionMinutes && unit !== DateRangeUnits.CUSTOM) {
            start = this.roundDateTimeToResolution(start, resolutionMinutes);
            end = this.roundDateTimeToResolution(end, resolutionMinutes, true);
        }

        return [start, end];
    }

    getDateRangeFromState(state: IDateRangeState): [DateTime, DateTime] {
        let range: [DateTime, DateTime];

        if (state.rangeOption === DateRangeUnits.CUSTOM) {
            range = [state.startDate, state.endDate];
        } else {
            range = this.getDateRangeFromUnit(state.rangeOption, state.resolutionMinutes);
        }

        return range;
    }

    private roundDateTimeToResolution(dateTime: DateTime, resolutionMinutes: number, roundUp: boolean = false): DateTime {
        if (!dateTime || !resolutionMinutes) {
            return dateTime;
        }

        const resolutionDuration = Duration.fromObject({ minutes: resolutionMinutes });
        const rounded = dateTime
            .minus({ minutes: dateTime.minute % resolutionMinutes, seconds: dateTime.second, milliseconds: dateTime.millisecond });

        return roundUp ? rounded.plus(resolutionDuration) : rounded;
    }
}
