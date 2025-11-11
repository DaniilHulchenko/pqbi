import { DateRangeType } from "../enums/date-range-type";
import { RefreshSelectionCustomUnits } from "../enums/refresh-selection-custom-units";

export class DateRangeAndRefreshModelNew {
    rangeUnit: DateRangeType;
    relativeValue: number | null;
    relativeUnit: RefreshSelectionCustomUnits | null;
    fromDate: Date | null;
    toDate: Date | null;
    refreshIntervalInSeconds: number | null;

    constructor(
        rangeUnit: DateRangeType,
        relativeValue: number | null,
        relativeUnit: RefreshSelectionCustomUnits | null,
        fromDate: Date | null,
        toDate: Date | null,
        refreshIntervalInSeconds: number | null,
    ) {
        this.rangeUnit = rangeUnit;
        this.relativeValue = relativeValue;
        this.relativeUnit = relativeUnit;
        this.fromDate = fromDate;
        this.toDate = toDate;
        this.refreshIntervalInSeconds = refreshIntervalInSeconds;
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

    toJson(): string {
        return JSON.stringify({
            rangeUnit: this.rangeUnit,
            relativeValue: this.relativeValue,
            relativeUnit: this.relativeUnit,
            fromDate: this.fromDate ? this.fromDate.toISOString() : null,
            toDate: this.toDate ? this.toDate.toISOString() : null,
            refreshIntervalInSeconds: this.refreshIntervalInSeconds,
        });
    }

    static createItem(
        dateRangeJson: string,
        refreshIntervalInSeconds: number | null = null,
    ): DateRangeAndRefreshModelNew {
        const parsed = JSON.parse(dateRangeJson);

        var rangeValue = parsed.relativeValue && !parsed.fromDate ? parsed.relativeValue : 30;
        var rangeUnit = parsed.relativeUnit && !parsed.fromDate ? parsed.relativeUnit : RefreshSelectionCustomUnits.Day;

        return new DateRangeAndRefreshModelNew(
            parsed.rangeUnit ?? DateRangeType.Relative,
            rangeValue,
            rangeUnit,
            parsed.fromDate ? new Date(parsed.fromDate) : null,
            parsed.toDate ? new Date(parsed.toDate) : null,
            refreshIntervalInSeconds ?? parsed.refreshIntervalInSeconds ?? 60,
        );
    }

    static getRefreshIntervalInSecondsFromJson(dateRangeJson: string): number | null {
        const parsed = JSON.parse(dateRangeJson);
        return parsed.refreshIntervalInSeconds ?? null;
    }
}

