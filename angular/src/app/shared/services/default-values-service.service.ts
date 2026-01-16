import { Injectable } from '@angular/core';
import { map, Observable, of, shareReplay, tap, switchMap, forkJoin } from 'rxjs';
import { CreateOrEditDefaultValueDto, DefaultValuesServiceProxy, GetDefaultValueForEditOutput } from '@shared/service-proxies/service-proxies';
import { ConfigurationVersionService } from './configuration-version-service.service';
import { CacheStorageService } from './cache-storage.service';
import { DefaultValueKeys } from '@shared/DefaultValueKeys';
import { UtcOffsetModel } from '@app/shared/models/utc-offset-model';
import { DateTimeDisplayFormatModel } from '@app/shared/models/date-time-display-format-model';

/**
 * Interface for all default values
 */
export interface AllDefaultValues {
    utcOffset: UtcOffsetModel;
    dateTimeDisplayFormat: DateTimeDisplayFormatModel;
    firstDayOfWeek: string;
    numberOfDecimals: number;
    numberOfDecimalsForPercentage: number;
    advancedParameterColors: any[];
    defaultColors: Record<string, string>;
}

@Injectable({
    providedIn: 'root',
})
export class DefaultValuesService {
    private readonly CACHE_PREFIX = 'default_value_';
    private _pendingRequests = new Map<string, Observable<string>>();

    constructor(
        private _defaultValuesServiceProxy: DefaultValuesServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    ) {
        this.configurationVersionService.getVersionChanged$().subscribe((versionChanged: boolean) => {
            if (versionChanged) {
                this.clearCache();
            }
        });
    }

    private clearCache(): void {
        this.cacheStorage.clearByPattern(this.CACHE_PREFIX);
        this._pendingRequests.clear();
    }

    cacheValue(name: string, value: string): void {
        this.cacheStorage.set(this.CACHE_PREFIX + name, value);
    }

    private getValue(name: string): Observable<string> {
        const cacheKey = this.CACHE_PREFIX + name;
        const cachedValue = this.cacheStorage.get<string>(cacheKey);
        if (cachedValue) {
            return of(cachedValue);
        }

        if (this._pendingRequests.has(name)) {
            return this._pendingRequests.get(name)!;
        }

        const request$ = this._defaultValuesServiceProxy
            .getDefaultValueByName(name)
            .pipe(
                map((result: GetDefaultValueForEditOutput) => result.defaultValue?.value),
                tap((value: string) => {
                    this.cacheStorage.set(cacheKey, value);
                    this._pendingRequests.delete(name);
                }),
                shareReplay({ bufferSize: 1, refCount: false })
            );

        this._pendingRequests.set(name, request$);
        return request$;
    }

    createOrEdit(value: CreateOrEditDefaultValueDto) : Observable<void> {
        return this._defaultValuesServiceProxy
            .createOrEdit(value)
            .pipe(
                tap(() => {
                    const cacheKey = this.CACHE_PREFIX + value.name;
                    this.cacheStorage.set(cacheKey, value.value);
                    this.configurationVersionService.refreshVersion().subscribe();
                })
            );
    }

    /**
     * Gets UTC offset default value as UtcOffsetModel
     * @returns Observable of UtcOffsetModel
     */
    getUtcOffset(): Observable<UtcOffsetModel> {
        return this.getValue(DefaultValueKeys.utcOffsetSettingName).pipe(
            map((value: string) => UtcOffsetModel.fromJson(value) || new UtcOffsetModel())
        );
    }

    /**
     * Gets date and time display format default value as DateTimeDisplayFormatModel
     * @returns Observable of DateTimeDisplayFormatModel
     */
    getDateTimeDisplayFormat(): Observable<DateTimeDisplayFormatModel> {
        return this.getValue(DefaultValueKeys.dateTimeDisplayFormatSettingName).pipe(
            map((value: string) => DateTimeDisplayFormatModel.fromJson(value) || new DateTimeDisplayFormatModel())
        );
    }

    /**
     * Gets first day of week default value
     * @returns Observable of string ('Auto', 'Sunday', or 'Monday')
     */
    getFirstDayOfWeek(): Observable<string> {
        return this.getValue(DefaultValueKeys.defaultFirstDayOfWeekSettingName).pipe(
            map((value: string) => value || 'Auto')
        );
    }

    /**
     * Gets default number of decimals default value
     * @returns Observable of number
     */
    getDefaultNumberOfDecimals(): Observable<number> {
        return this.getValue(DefaultValueKeys.defaultNumberOfDecimalsSettingName).pipe(
            map((value: string) => {
                const parsed = parseInt(value, 10);
                return isNaN(parsed) ? 2 : parsed;
            })
        );
    }

    /**
     * Gets default number of decimals for percentage default value
     * @returns Observable of number
     */
    getDefaultNumberOfDecimalsForPercentage(): Observable<number> {
        return this.getValue(DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName).pipe(
            map((value: string) => {
                const parsed = parseInt(value, 10);
                return isNaN(parsed) ? 2 : parsed;
            })
        );
    }

    /**
     * Gets default icon ID default value
     * @returns Observable of number or null
     */
    getDefaultIconId(): Observable<number | null> {
        return this.getValue(DefaultValueKeys.defaultIconSettingName).pipe(
            map((value: string) => {
                if (!value) return null;
                const parsed = parseInt(value, 10);
                return isNaN(parsed) ? null : parsed;
            })
        );
    }

    /**
     * Gets default tags pick list state default value as parsed JSON object
     * @returns Observable of parsed PickListState object or null
     */
    getDefaultTagsPickListStateParsed<T = any>(): Observable<T | null> {
        return this.getValue(DefaultValueKeys.defaultTagsPickListStateSettingName).pipe(
            map((value: string) => {
                if (!value) return null;
                try {
                    return JSON.parse(value) as T;
                } catch (e) {
                    return null;
                }
            })
        );
    }

    /**
     * Gets advanced parameter colors default value as parsed JSON array
     * @returns Observable of array of advanced parameter color objects
     */
    getAdvancedParameterColors(): Observable<any[]> {
        return this.getValue(DefaultValueKeys.advancedParameterColorsSettingName).pipe(
            map((value: string) => {
                if (!value) return [];
                try {
                    const parsed = JSON.parse(value);
                    return Array.isArray(parsed) ? parsed : [];
                } catch (e) {
                    return [];
                }
            })
        );
    }

    /**
     * Gets a default color value by key
     * @param colorKey Key from DefaultValueKeys.defaultColors (e.g., 'v1n', 'v2n', etc.)
     * @returns Observable of string (hex color value)
     */
    getDefaultColor(colorKey: keyof typeof DefaultValueKeys.defaultColors): Observable<string> {
        const key = DefaultValueKeys.defaultColors[colorKey];
        return this.getValue(key).pipe(
            map((value: string) => value || '#000000')
        );
    }

    /**
     * Gets all default colors as an object
     * @returns Observable of object with color keys and hex values
     */
    getAllDefaultColors(): Observable<Record<string, string>> {
        const colorKeys = Object.keys(DefaultValueKeys.defaultColors) as Array<keyof typeof DefaultValueKeys.defaultColors>;
        
        if (colorKeys.length === 0) {
            return of({});
        }

        const requests = colorKeys.reduce((acc, key) => {
            acc[key] = this.getDefaultColor(key);
            return acc;
        }, {} as Record<string, Observable<string>>);

        return forkJoin(requests);
    }

    /**
     * Gets all default values with a single backend request.
     * Uses smart caching - checks local storage first, loads only missing values from backend.
     * @returns Observable of all default values
     */
    getAllDefaultValues(): Observable<AllDefaultValues> {
        // Collect all keys that need to be loaded
        const allKeys: string[] = [
            DefaultValueKeys.utcOffsetSettingName,
            DefaultValueKeys.dateTimeDisplayFormatSettingName,
            DefaultValueKeys.defaultFirstDayOfWeekSettingName,
            DefaultValueKeys.defaultNumberOfDecimalsSettingName,
            DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName,
            DefaultValueKeys.advancedParameterColorsSettingName,
            ...Object.values(DefaultValueKeys.defaultColors),
        ];

        // Check cache for all keys
        const cachedValues = new Map<string, string>();
        const missingKeys: string[] = [];

        allKeys.forEach(key => {
            const cacheKey = this.CACHE_PREFIX + key;
            const cached = this.cacheStorage.get<string>(cacheKey);
            if (cached !== null && cached !== undefined) {
                cachedValues.set(key, cached);
            } else {
                missingKeys.push(key);
            }
        });

        // If all values are cached, return them immediately
        if (missingKeys.length === 0) {
            return of(this.parseAllDefaultValues(cachedValues));
        }

        // Load missing values with a single backend request
        return this._defaultValuesServiceProxy.getDefaultValueByNames(missingKeys).pipe(
            map((results: GetDefaultValueForEditOutput[]) => {
                // Cache and merge loaded values
                results.forEach((result) => {
                    const key = result.defaultValue?.name;
                    const value = result.defaultValue?.value;
                    if (key && value !== null && value !== undefined) {
                        const cacheKey = this.CACHE_PREFIX + key;
                        this.cacheStorage.set(cacheKey, value);
                        cachedValues.set(key, value);
                    }
                });

                // Fill missing values with defaults
                allKeys.forEach(key => {
                    if (!cachedValues.has(key)) {
                        cachedValues.set(key, '');
                    }
                });

                return this.parseAllDefaultValues(cachedValues);
            })
        );
    }

    /**
     * Parses cached values into AllDefaultValues object
     */
    private parseAllDefaultValues(values: Map<string, string>): AllDefaultValues {
        // Parse UTC offset
        const utcOffsetValue = values.get(DefaultValueKeys.utcOffsetSettingName) || '';
        const utcOffset = UtcOffsetModel.fromJson(utcOffsetValue) || new UtcOffsetModel();

        // Parse date time display format
        const dateTimeDisplayFormatValue = values.get(DefaultValueKeys.dateTimeDisplayFormatSettingName) || '';
        const dateTimeDisplayFormat = DateTimeDisplayFormatModel.fromJson(dateTimeDisplayFormatValue) || new DateTimeDisplayFormatModel();

        // Parse first day of week
        const firstDayOfWeek = values.get(DefaultValueKeys.defaultFirstDayOfWeekSettingName) || 'Auto';

        // Parse number of decimals
        const numberOfDecimalsValue = values.get(DefaultValueKeys.defaultNumberOfDecimalsSettingName) || '2';
        const numberOfDecimals = parseInt(numberOfDecimalsValue, 10) || 2;

        // Parse number of decimals for percentage
        const numberOfDecimalsForPercentageValue = values.get(DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName) || '2';
        const numberOfDecimalsForPercentage = parseInt(numberOfDecimalsForPercentageValue, 10) || 2;

        // Parse advanced parameter colors
        const advancedParameterColorsValue = values.get(DefaultValueKeys.advancedParameterColorsSettingName) || '[]';
        let advancedParameterColors: any[] = [];
        if (advancedParameterColorsValue) {
            try {
                const parsed = JSON.parse(advancedParameterColorsValue);
                advancedParameterColors = Array.isArray(parsed) ? parsed : [];
            } catch (e) {
                advancedParameterColors = [];
            }
        }

        // Parse default colors
        const defaultColors: Record<string, string> = {};
        Object.entries(DefaultValueKeys.defaultColors).forEach(([key, valueKey]) => {
            const colorValue = values.get(valueKey);
            defaultColors[key] = colorValue || '#000000';
        });

        return {
            utcOffset,
            dateTimeDisplayFormat,
            firstDayOfWeek,
            numberOfDecimals,
            numberOfDecimalsForPercentage,
            advancedParameterColors,
            defaultColors,
        };
    }
}
