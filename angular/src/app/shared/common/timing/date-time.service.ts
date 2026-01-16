import { Injectable } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';
import { DateTime } from 'luxon';
import { UtcOffsetModel } from '@app/shared/models/utc-offset-model';
import { DefaultValuesService } from '@app/shared/services/default-values-service.service';
import { DateTimeDisplayFormatModel } from '@app/shared/models/date-time-display-format-model';

interface DateTimeFormatParts {
    date: string;
    time: string;
    dateTime: string;
    isHour12: boolean;
}

@Injectable()
export class DateTimeService {
    constructor(
        private _appLocalizationService: AppLocalizationService,
        private _defaultValuesService: DefaultValuesService
    ) {
        this.subscribeToDateTimeDisplayFormat();
    }

    private currentDisplayFormat: DateTimeDisplayFormatModel = new DateTimeDisplayFormatModel();
    private customFormatCache = new Map<string, DateTimeFormatParts>();

    private subscribeToDateTimeDisplayFormat(): void {
        this._defaultValuesService.getDateTimeDisplayFormat().subscribe({
            next: (format: DateTimeDisplayFormatModel) => {
                this.currentDisplayFormat = format || new DateTimeDisplayFormatModel();
            },
            error: () => {
                this.currentDisplayFormat = new DateTimeDisplayFormatModel();
            },
        });
    }

    createDateRangePickerOptions(): any {
        let options = {
            locale: {
                format: 'L',
                applyLabel: this._appLocalizationService.l('Apply'),
                cancelLabel: this._appLocalizationService.l('Cancel'),
                customRangeLabel: this._appLocalizationService.l('CustomRange'),
            },
            min: this.fromISODateString('1900-01-01'),
            minDate: this.fromISODateString('1900-01-01'),
            max: this.getDate(),
            maxDate: this.getDate(),
            opens: 'left',
            ranges: {},
        };

        options.ranges[this._appLocalizationService.l('Today')] = [this.getStartOfDay(), this.getEndOfDay()];
        options.ranges[this._appLocalizationService.l('Yesterday')] = [
            this.minusDays(this.getStartOfDay(), 1),
            this.minusDays(this.getEndOfDay(), 1),
        ];
        options.ranges[this._appLocalizationService.l('Last7Days')] = [
            this.minusDays(this.getStartOfDay(), 6),
            this.getEndOfDay(),
        ];
        options.ranges[this._appLocalizationService.l('Last30Days')] = [
            this.minusDays(this.getStartOfDay(), 29),
            this.getEndOfDay(),
        ];
        options.ranges[this._appLocalizationService.l('ThisMonth')] = [
            this.getDate().startOf('month'),
            this.getDate().endOf('month'),
        ];
        options.ranges[this._appLocalizationService.l('LastMonth')] = [
            this.getDate().startOf('month').minus({ months: 1 }),
            this.getDate().endOf('month').minus({ months: 1 }),
        ];

        return options;
    }

    getDate(): DateTime {
        if (abp.clock.provider.supportsMultipleTimezone) {
            return DateTime.local().setZone(abp.timing.timeZoneInfo.iana.timeZoneId);
        } else {
            return DateTime.local();
        }
    }

    getUTCDate(): DateTime {
        return DateTime.utc();
    }

    getYear(): number {
        return this.getDate().year;
    }

    getStartOfDay(): DateTime {
        return this.getDate().startOf('day');
    }

    getStartOfWeek(): DateTime {
        return this.getDate().startOf('week');
    }

    getStartOfDayForDate(date: DateTime | Date): DateTime {
        if (!date) {
            return date as DateTime;
        }

        if (date instanceof Date) {
            return this.getStartOfDayForDate(this.fromJSDate(date));
        }

        return date.startOf('day');
    }

    getStartOfDayMinusDays(daysFromNow: number): DateTime {
        let date = this.getDate();
        let newDate = this.minusDays(date, daysFromNow);
        return this.getStartOfDayForDate(newDate);
    }

    getEndOfDay(): DateTime {
        return this.getDate().endOf('day');
    }

    getEndOfDayForDate(date: DateTime | Date): DateTime {
        if (!date) {
            return date as DateTime;
        }

        if (date instanceof Date) {
            return this.getEndOfDayForDate(this.fromJSDate(date));
        }

        return date.endOf('day');
    }

    getEndOfDayPlusDays(daysFromNow: number): DateTime {
        let date = this.getDate();
        let newDate = this.plusDays(date, daysFromNow);
        return this.getEndOfDayForDate(newDate);
    }

    getEndOfDayMinusDays(daysFromNow: number): DateTime {
        let date = this.getDate();
        let newDate = this.minusDays(date, daysFromNow);
        return this.getEndOfDayForDate(newDate);
    }

    plusDays(date: DateTime | Date, dayCount: number): DateTime {
        if (date instanceof Date) {
            return this.plusDays(this.fromJSDate(date), dayCount);
        }

        return date.plus({ days: dayCount });
    }

    plusSeconds(date: DateTime, seconds: number) {
        if (!date) {
            return date;
        }

        if (date instanceof Date) {
            return this.plusSeconds(this.fromJSDate(date), seconds);
        }

        return date.plus({ seconds: seconds });
    }

    minusDays(date: DateTime, dayCount: number): DateTime {
        return date.minus({ days: dayCount });
    }

    fromISODateString(date: string): DateTime {
        return DateTime.fromISO(date);
    }

    formatISODateString(dateText: string, format: string): string {
        let date = this.fromISODateString(dateText);
        return this.applyDisplayLocale(date).toFormat(format);
    }

    formatJSDate(jsDate: Date, format: string): string {
        let date = DateTime.fromJSDate(jsDate);
        return this.applyDisplayLocale(date).toFormat(format);
    }

    formatDate(date: DateTime | Date, format: string): string {
        if (date instanceof Date) {
            return this.formatDate(this.fromJSDate(date), format);
        }

        return this.applyDisplayLocale(date).toFormat(format);
    }

    formatDateForDisplay(date: DateTime | Date, displayFormat?: DateTimeDisplayFormatModel): string {
        const format = this.getLuxonDateFormat(displayFormat);
        return this.formatWithDisplayLocale(date, format, displayFormat);
    }

    formatTimeForDisplay(date: DateTime | Date, displayFormat?: DateTimeDisplayFormatModel): string {
        const format = this.getLuxonTimeFormat(displayFormat);
        return this.formatWithDisplayLocale(date, format, displayFormat);
    }

    formatDateTimeForDisplay(date: DateTime | Date, displayFormat?: DateTimeDisplayFormatModel): string {
        const format = this.getLuxonDateTimeFormat(displayFormat);
        return this.formatWithDisplayLocale(date, format, displayFormat);
    }

    formatWithDisplayLocale(
        date: DateTime | Date,
        format: string,
        displayFormat?: DateTimeDisplayFormatModel
    ): string {
        if (!date) {
            return '';
        }

        const dateTime = date instanceof Date ? this.fromJSDate(date) : date;
        return this.applyDisplayLocale(dateTime, displayFormat).toFormat(format);
    }

    getDiffInSeconds(maxDate: DateTime | Date, minDate: DateTime | Date) {
        if (maxDate instanceof Date && minDate instanceof Date) {
            return this.getDiffInSeconds(this.fromJSDate(maxDate), this.fromJSDate(minDate));
        }

        return (maxDate as DateTime).diff(minDate as DateTime, 'seconds');
    }

    createJSDate(year: number, month: number, day: number): Date {
        return this.createDate(year, month, day).toJSDate();
    }

    createDate(year: number, month: number, day: number): DateTime {
        if (abp.clock.provider.supportsMultipleTimezone) {
            return DateTime.utc(year, month + 1, day);
        } else {
            return DateTime.local(year, month + 1, day);
        }
    }

    createUtcDate(year: number, month: number, day: number): DateTime {
        return DateTime.utc(year, month + 1, day);
    }

    toUtcDate(date: DateTime | Date): DateTime {
        if (date instanceof Date) {
            return this.createUtcDate(date.getFullYear(), date.getMonth(), date.getDate());
        }

        return (date as DateTime).toUTC();
    }

    toJSDate(date: DateTime): Date {
        return date.toJSDate();
    }

    fromJSDate(date: Date): DateTime {
        return DateTime.fromJSDate(date);
    }

    getUserTimeZoneName(): string {
     const abpTz = abp?.timing?.timeZoneInfo?.iana?.timeZoneId;
    if (abp?.clock?.provider?.supportsMultipleTimezone && abpTz) {
        return abpTz;
    }

     const luxonTz = DateTime.local().zoneName;
    if (luxonTz && luxonTz !== "local" && luxonTz !== "UTC") {
        return luxonTz;
    }

     const browserTz = Intl.DateTimeFormat().resolvedOptions().timeZone;
    if (browserTz) {
        return browserTz;
    }

     return "UTC";
}


    fromNow(date: DateTime | Date): string {
        if (date instanceof Date) {
            return this.fromNow(this.fromJSDate(date));
        }

        return this.applyDisplayLocale(date).toRelative();
    }

    getLuxonDateFormat(displayFormat?: DateTimeDisplayFormatModel): string {
        const format = displayFormat || this.currentDisplayFormat;

        if (format.mode === 'manual' && format.manualDateFormat) {
            return format.manualDateFormat
                .replace(/yyyy/g, 'yyyy')
                .replace(/yy/g, 'yy')
                .replace(/mm/g, 'MM')
                .replace(/dd/g, 'dd');
        }

        if (format.mode === 'custom' && format.customCulture) {
            return this.getCustomCultureFormats(format.customCulture, 'luxon').date;
        }

        return 'dd/MM/yyyy';
    }

    getLuxonTimeFormat(displayFormat?: DateTimeDisplayFormatModel): string {
        const format = displayFormat || this.currentDisplayFormat;

        if (format.mode === 'manual' && format.manualTimeFormat) {
            return format.manualTimeFormat === '12 hours' ? 'hh:mm a' : 'HH:mm';
        }

        if (format.mode === 'custom' && format.customCulture) {
            return this.getCustomCultureFormats(format.customCulture, 'luxon').time;
        }

        return 'HH:mm';
    }

    getLuxonDateTimeFormat(displayFormat?: DateTimeDisplayFormatModel): string {
        const format = displayFormat || this.currentDisplayFormat;

        if (format.mode === 'custom' && format.customCulture) {
            return this.getCustomCultureFormats(format.customCulture, 'luxon').dateTime;
        }

        const dateFormat = this.getLuxonDateFormat(format);

        if (format.mode === 'manual' && format.manualTimeFormat) {
            return `${dateFormat} ${this.getLuxonTimeFormat(format)}`;
        }

        return `${dateFormat} ${this.getLuxonTimeFormat(format)}`;
    }

    getDevExtremeDateTimeFormat(displayFormat?: DateTimeDisplayFormatModel): string {
        const format = displayFormat || this.currentDisplayFormat;

        if (format.mode === 'manual' && format.manualDateFormat) {
            let dxFormat = format.manualDateFormat
                .replace(/yyyy/g, 'yyyy')
                .replace(/yy/g, 'yy')
                .replace(/mm/g, 'MM')
                .replace(/dd/g, 'dd');

            const timeFormat = format.manualTimeFormat === '12 hours' ? 'hh:mm tt' : 'HH:mm';
            return `${dxFormat} ${timeFormat}`;
        }

        if (format.mode === 'custom' && format.customCulture) {
            return this.getCustomCultureFormats(format.customCulture, 'devextreme').dateTime;
        }

        return 'dd/MM/yyyy HH:mm';
    }

    getTimezoneOffset(ianaTimezoneId: string): number {
        let hourAndMinuteOffset = DateTime.fromJSDate(new Date(), { zone: ianaTimezoneId }).toFormat('ZZ');
        let multiplier = hourAndMinuteOffset[0] === '-' ? -1 : +1;
        let hourParts = hourAndMinuteOffset.replace('-', '').replace('+', '').split(':');
        let hourOffset = hourParts[0];
        let minuteOffset = hourParts[1];
        return multiplier * (parseInt(hourOffset) * 60 + parseInt(minuteOffset));
    }

    // only changes timezone of given date without changing the date itself
    changeTimeZone(date: Date, ianaTimezoneId: string): Date {
        let utcDateString = new Date(date.getTime() - (date.getTimezoneOffset() * 60000)).toISOString();

        if (utcDateString.indexOf('T') < 0) {
            throw 'Invalid date format';
        }

        // construct a new DateTime from utcDateString
        let dateAndTimeParts = utcDateString.split('T');
        let dateParts = dateAndTimeParts[0].split('-');
        let timeParts = dateAndTimeParts[1].split('.')[0].split(':');

        let year = parseInt(dateParts[0]);
        let month = parseInt(dateParts[1]);
        let day = parseInt(dateParts[2]);

        let hour = parseInt(timeParts[0]);
        let minute = parseInt(timeParts[1]);
        let second = parseInt(timeParts[2]);

        return DateTime.fromObject({
            year: year,
            month: month,
            day: day,
            hour: hour,
            minute: minute,
            second: second
        }, { zone: ianaTimezoneId }).toJSDate();
    }

    changeDateTimeZone(date: DateTime, ianaTimezoneId: string): DateTime {
        let jsDate = date.toJSDate();
        let utcDate = this.changeTimeZone(jsDate, ianaTimezoneId);
        return DateTime.fromJSDate(utcDate);
    }

    /**
     * Determines if Monday is the first day of week based on settings
     * @param firstDayOfWeekSetting The first day of week setting ('Auto', 'Sunday', or 'Monday')
     * @returns true if Monday is first day, false if Sunday, or calculated from locale if Auto
     */
    IsMondayFirstDayOfWeek(firstDayOfWeekSetting: string): boolean {
        if (firstDayOfWeekSetting === 'Monday') {
            return true;
        } else if (firstDayOfWeekSetting === 'Sunday') {
            return false;
        } else {
            // Auto mode: determine from locale
            // Luxon's startOf('week') respects locale-specific week start
            try {
                const locale = DateTime.now().locale || 'en-US';
                // Create a test date and check what day the week starts on
                // In Luxon, weekday 1 = Monday, 7 = Sunday
                const testDate = DateTime.now().setLocale(locale);
                const weekStart = testDate.startOf('week');
                // If week starts on Monday (weekday === 1), return true
                return weekStart.weekday === 1;
            } catch (e) {
                // Fallback: most locales use Monday as first day
                return true;
            }
        }
    }

    /**
     * Gets UTC offset in minutes based on settings
     * @param utcOffsetModel The UTC offset model containing mode and settings
     * @returns UTC offset in minutes (can be negative)
     */
    GetUtcOffsetMinutes(utcOffsetModel: UtcOffsetModel): number {
        if (!utcOffsetModel) {
            // Fallback: use system timezone
            return this.getSystemUtcOffsetMinutes();
        }

        if (utcOffsetModel.mode === 'manual') {
            // Manual mode: convert hours to minutes
            return utcOffsetModel.manualUtcOffset * 60;
        } else if (utcOffsetModel.mode === 'custom' && utcOffsetModel.customTimeZone) {
            // Custom timezone mode: get offset for the specified timezone
            return this.getTimezoneOffsetMinutes(utcOffsetModel.customTimeZone);
        } else {
            // Auto/timezone mode: use system timezone
            return this.getSystemUtcOffsetMinutes();
        }
    }

    /**
     * Gets UTC offset in minutes for the system timezone
     * @returns UTC offset in minutes
     */
    private getSystemUtcOffsetMinutes(): number {
        const timeZoneName = this.getUserTimeZoneName();
        if (timeZoneName && timeZoneName !== 'UTC') {
            return this.getTimezoneOffsetMinutes(timeZoneName);
        }
        // Fallback: use browser's timezone offset
        return -new Date().getTimezoneOffset();
    }

    /**
     * Gets UTC offset in minutes for a specific IANA timezone
     * @param ianaTimezoneId IANA timezone identifier (e.g., 'America/New_York')
     * @returns UTC offset in minutes
     */
    private getTimezoneOffsetMinutes(ianaTimezoneId: string): number {
        try {
            const now = DateTime.now().setZone(ianaTimezoneId);
            const offsetInMinutes = now.offset;
            return offsetInMinutes;
        } catch (e) {
            // If timezone is invalid, fallback to system timezone
            return this.getSystemUtcOffsetMinutes();
        }
    }

    private applyDisplayLocale(date: DateTime, displayFormat?: DateTimeDisplayFormatModel): DateTime {
        const format = displayFormat || this.currentDisplayFormat;
        if (format.mode === 'custom' && format.customCulture) {
            return date.setLocale(format.customCulture);
        }

        return date;
    }

    private getCustomCultureFormats(
        customCulture: string,
        target: 'luxon' | 'devextreme'
    ): DateTimeFormatParts {
        const cacheKey = `${customCulture}-${target}`;
        const cached = this.customFormatCache.get(cacheKey);
        if (cached) {
            return cached;
        }

        const dateOptions: Intl.DateTimeFormatOptions = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
        };
        const timeOptions: Intl.DateTimeFormatOptions = {
            hour: '2-digit',
            minute: '2-digit',
        };
        const dateTimeOptions: Intl.DateTimeFormatOptions = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
        };

        const timeFormatter = new Intl.DateTimeFormat(customCulture, timeOptions);
        const hour12 = this.getHour12(timeFormatter.resolvedOptions());

        const formats: DateTimeFormatParts = {
            date: this.getFormatFromIntlParts(customCulture, dateOptions, target, hour12),
            time: this.getFormatFromIntlParts(customCulture, timeOptions, target, hour12),
            dateTime: this.getFormatFromIntlParts(customCulture, dateTimeOptions, target, hour12),
            isHour12: hour12,
        };

        this.customFormatCache.set(cacheKey, formats);
        return formats;
    }

    private getFormatFromIntlParts(
        locale: string,
        options: Intl.DateTimeFormatOptions,
        target: 'luxon' | 'devextreme',
        hour12: boolean
    ): string {
        const formatter = new Intl.DateTimeFormat(locale, options);
        const parts = formatter.formatToParts(new Date(Date.UTC(2006, 10, 22, 13, 45, 0)));

        return parts
            .map((part) => this.mapIntlPartToFormat(part, target, hour12))
            .join('');
    }

    private mapIntlPartToFormat(
        part: Intl.DateTimeFormatPart,
        target: 'luxon' | 'devextreme',
        hour12: boolean
    ): string {
        switch (part.type) {
            case 'year':
                return 'yyyy';
            case 'month':
                return 'MM';
            case 'day':
                return 'dd';
            case 'hour':
                return hour12 ? 'hh' : 'HH';
            case 'minute':
                return 'mm';
            case 'second':
                return 'ss';
            case 'dayPeriod':
                return target === 'devextreme' ? 'tt' : 'a';
            case 'literal':
                return part.value;
            default:
                return part.value;
        }
    }

    private getHour12(options: Intl.ResolvedDateTimeFormatOptions): boolean {
    return options.hour12 === true;
}
}
