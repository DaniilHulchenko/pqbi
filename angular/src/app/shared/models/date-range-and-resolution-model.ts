import { DateRangeType } from '../enums/date-range-type';
import { RefreshSelectionCustomUnits } from '../enums/refresh-selection-custom-units';
import { ResolutionState } from './resolution-state';

export class DateRangeAndResolutionModel {
    rangeUnit: DateRangeType;
    relativeValue: number | null;
    relativeUnit: RefreshSelectionCustomUnits | null;
    fromDate: Date | null;
    toDate: Date | null;
    resolution: ResolutionState | null;

    constructor(
        rangeUnit: DateRangeType,
        relativeValue: number | null,
        relativeUnit: RefreshSelectionCustomUnits | null,
        fromDate: Date | null,
        toDate: Date | null,
        resolution: ResolutionState | null,
    ) {
        this.rangeUnit = rangeUnit;
        this.relativeValue = relativeValue;
        this.relativeUnit = relativeUnit;
        this.fromDate = fromDate;
        this.toDate = toDate;
        this.resolution = resolution;
    }

    toDateRangeJson(): string {
        return JSON.stringify({
            rangeUnit: this.rangeUnit,
            relativeValue: this.relativeValue,
            relativeUnit: this.relativeUnit,
            fromDate: this.fromDate ? this.fromDate.toISOString() : null,
            toDate: this.toDate ? this.toDate.toISOString() : null,
        });
    }

    static createItem(
        dateRangeJson: string,
        resolution: ResolutionState | null = null,
    ): DateRangeAndResolutionModel {
        const parsed = JSON.parse(dateRangeJson);

        var rangeValue = parsed.relativeValue && !parsed.fromDate ? parsed.relativeValue : 30;
        var rangeUnit = parsed.relativeUnit && !parsed.fromDate ? parsed.relativeUnit : RefreshSelectionCustomUnits.Day;

        return new DateRangeAndResolutionModel(
            parsed.rangeUnit ?? DateRangeType.Relative,
            rangeValue,
            rangeUnit,
            parsed.fromDate ? new Date(parsed.fromDate) : null,
            parsed.toDate ? new Date(parsed.toDate) : null,
            resolution,
        );
    }
}
