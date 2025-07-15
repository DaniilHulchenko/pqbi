import { DateTime } from 'luxon';
import { DateRangeUnits } from '../enums/date-range-selection-units';

export interface IDateRangeState {
    rangeOption: DateRangeUnits;
    startDate?: DateTime;
    endDate?: DateTime;
    resolutionMinutes?: number;
}
