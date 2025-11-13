import { Injectable } from '@angular/core';
import { DateRangeUnits } from '../enums/date-range-selection-units';
import { DateTime, Duration } from 'luxon';
import { IDateRangeState } from '../interfaces/date-range-state';
import { DateRangeAndRefreshModelNew } from '../models/date-range-and-refresh-model-new';
import { DateRangeType } from '../enums/date-range-type';
import { RefreshSelectionCustomUnits } from '../enums/refresh-selection-custom-units';

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

    getDateRangeFromNewState(state: DateRangeAndRefreshModelNew): [Date, Date] {
        let range: [Date, Date];

        if (state.rangeUnit === DateRangeType.Range) {
            range = [state.fromDate, state.toDate];
        } else {
            range = this.getDateRangeFromNewUnit(state.relativeUnit, state.relativeValue);
        }

        return range;
    }

    getDateRangeFromNewUnit(unit: RefreshSelectionCustomUnits, value: number): [Date, Date] {
        switch (unit) {
            case RefreshSelectionCustomUnits.Sec:
                return [new Date(Date.now() - value * 1000), new Date()];   
            case RefreshSelectionCustomUnits.Min:
                return [new Date(Date.now() - value * 60 * 1000), new Date()];
            case RefreshSelectionCustomUnits.Hour:
                return [new Date(Date.now() - value * 60 * 60 * 1000), new Date()];
            case RefreshSelectionCustomUnits.Day:
                return [new Date(Date.now() - value * 24 * 60 * 60 * 1000), new Date()];
            case RefreshSelectionCustomUnits.Week:
                return [new Date(Date.now() - value * 7 * 24 * 60 * 60 * 1000), new Date()];
            case RefreshSelectionCustomUnits.Month:
                return [new Date(Date.now() - value * 30 * 24 * 60 * 60 * 1000), new Date()];
            case RefreshSelectionCustomUnits.Year:
                return [new Date(Date.now() - value * 365 * 24 * 60 * 60 * 1000), new Date()];
        }
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
