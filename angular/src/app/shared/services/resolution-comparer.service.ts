import { Injectable } from '@angular/core';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { CustomParameterDto, CustomParametersServiceProxy, GetCustomParameterForViewDto } from '@shared/service-proxies/service-proxies';
import { map, Observable } from 'rxjs';

@Injectable({
    providedIn: 'root',
})
export class ResolutionComparerService {

    constructor(private _customParameterServiceProxy: CustomParametersServiceProxy) {}

    /**
     * Function to convert a time period string to milliseconds
     * @param timePeriod Scada resolution
     * @returns {number} value in milliseconds
     */
    convertToMilliseconds(timePeriod: string): number {
      // Map resolution predefined time period strings to milliseconds
      const predefinedTimes: { [key: string]: number } = {
        IS100MS: 100,
        IS200MS: 200,
        IS1SEC: 1000 * 1,
        IS3SEC: 1000 * 3,
        IS5SEC: 1000 * 5,
        IS1MIN: 1000 * 60 * 1,
        IS3MIN: 1000 * 60 * 3,
        IS5MIN: 1000 * 60 * 5,
        IS10MIN: 1000 * 60 * 10,
        IS1HOUR: 1000 * 60 * 60 * 1,
        IS2HOUR: 1000 * 60 * 60 * 2,
        IS1DAY: 1000 * 60 * 60 * 24 * 1,
        IS1WEEK: 1000 * 60 * 60 * 24 * 7 * 1,
        IS1MONTH: 1000 * 60 * 60 * 24 * 30 * 1,
    };

      if (predefinedTimes[timePeriod] !== undefined) {
            return predefinedTimes[timePeriod];
        } else if (timePeriod.startsWith('ISX')) {
            const seconds = parseFloat(timePeriod.slice(3));
            return seconds * 1000; // Convert seconds to milliseconds
        } else {
            throw new Error(`Invalid resolution: ${timePeriod}`);
        }
    }

    /**
    * Function to compare two time periods
    * Example usage:
    *  compareResolutions("IS1SEC", "ISX400"); // -600 (since 1 second < 400 seconds)
    *  compareResolutions("IS3MIN", "IS1MIN"); // 120000 (since 3 minutes > 1 minute)
    *
    * @param {string} time1
    * @param {string} time2
    * @returns {number}
    */
    compareResolutions(time1: string, time2: string): number {
        const time1Ms = this.convertToMilliseconds(time1);
        const time2Ms = this.convertToMilliseconds(time2);
        return time1Ms - time2Ms; // Positive if time1 > time2, 0 if equal, negative if time1 < time2
    }

    /**
     * Function to find the minimum and maximum values in an array of time periods ['IS1SEC', 'IS3MIN', 'IS1HOUR'] => [1000, 3600000]
     *                                                                                                      |-------------------|
     *                                                                                  |--------------------------------|
     *
     * @param {string[]} values
     * @returns {[number, number]}
     */
    findMinMaxValues(values: string[]): [number, number] {
        if (values.length === 0) {
            throw new Error('Array of values cannot be empty');
        }

        const millisecondsValues = values.map(this.convertToMilliseconds);
        const minValue = Math.min(...millisecondsValues);
        const maxValue = Math.max(...millisecondsValues);

        return [minValue, maxValue];
    }

    /**
     * Fetch all customParameter baseParameter children resolutions by customParameter id (reside in data property)
     * @param {number} id
     * @returns {Observable<string[]>}
     */
    getResolutionsFromCustomParameter(id: number): Observable<number[]> {
      return this._customParameterServiceProxy.getCustomParameterForView(id).pipe(
          map((response: GetCustomParameterForViewDto) => {
              let customParameter: CustomParameterDto = response.customParameter;
              let baseParameters: Parameter[] = JSON.parse(customParameter.customBaseDataList);
              let resolutions: number[] = baseParameters.map((parameter: Parameter) => parameter.resolution);
              return resolutions;
          }),
      );
  }

}


