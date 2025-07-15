import { DateTime } from 'luxon';
import { DateRangeUnits } from '../enums/date-range-selection-units';
import { IDateRangeState } from '../interfaces/date-range-state';
import safeStringify from 'fast-safe-stringify';

export class DateRangeState implements IDateRangeState {
    rangeOption: DateRangeUnits;
    startDate?: DateTime<boolean>;
    endDate?: DateTime<boolean>;

    constructor(rangeState: IDateRangeState){
        this.rangeOption = rangeState.rangeOption;
        this.startDate = rangeState.startDate;
        this.endDate = rangeState.endDate;
    }

    static fromJSON(json: string): DateRangeState {
        let object = JSON.parse(json);
        return new DateRangeState({
            rangeOption: object.rangeOption,
            startDate: object.startDate ? DateTime.fromISO(object.startDate) : null,
            endDate: object.endDate ? DateTime.fromISO(object.endDate) : null
        });
    }

    toJSON(): string {
        let object = {
            rangeOption: this.rangeOption,
            startDate: this.startDate?.toJSON() ?? null,
            endDate: this.endDate?.toJSON() ?? null
        }
        return safeStringify(object);
    }
}
