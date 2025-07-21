import { Injectable } from '@angular/core';
import { ResolutionState } from '../models/resolution-state';
import { ResolutionUnits } from '../enums/resolution-selection-units';
import { CustomResolutionUnits } from '../enums/custom-resolution-selection-units';
import { CustomResolutionUnitsComparer } from './custom-resolution-units-comparer';
import {
    CustomParameterDto,
    CustomParametersServiceProxy,
    GetCustomParameterForViewDto,
} from '@shared/service-proxies/service-proxies';
import { Observable, of, switchMap } from 'rxjs';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { BaseState } from '../models/base-state';
import { BaseUnits } from '../enums/base-units';

@Injectable({
    providedIn: 'root',
})
export class ResolutionService {

    readonly _unitDisplayNames: Record<CustomResolutionUnits, string> = {
        [CustomResolutionUnits.MS]: 'Millisecond',
        [CustomResolutionUnits.SEC]: 'Second',
        [CustomResolutionUnits.MIN]: 'Minute',
        [CustomResolutionUnits.HOUR]: 'Hour',
        [CustomResolutionUnits.DAY]: 'Day',
        [CustomResolutionUnits.WEEK]: 'Week',
    };

    readonly _resolutionDisplayNames: Record<ResolutionUnits, string> = {
        [ResolutionUnits.AUTO]: 'Auto',
        [ResolutionUnits.CUSTOM]: 'Custom Resolution',
        [ResolutionUnits.IS1SEC]: '1 Second',
        [ResolutionUnits.IS10SEC]: '10 Seconds',
        [ResolutionUnits.IS1MIN]: '1 Minute',
        [ResolutionUnits.IS10MIN]: '10 Minutes',
        [ResolutionUnits.IS1HOUR]: '1 Hour',
    };

    constructor(
        private _customResolutionUnitsComparer: CustomResolutionUnitsComparer,
        private _customParameterServiceProxy: CustomParametersServiceProxy,
    ) { }


    getResolutionUnits(seconds: number): [ResolutionUnits, number] {
        // if(seconds > 3600)
        // {
        //     //hour
        //     if(seconds % 60 === 0)
        //     {
        //             //
        //     }
        // }
        switch (seconds) {
            case 1:
                return [ResolutionUnits.IS1SEC, 1];         // 1 second
            case 10:
                return [ResolutionUnits.IS10SEC, 10];       // 10 seconds
            case 60:
                return [ResolutionUnits.IS1MIN, 1];         // 1 minute
            case 600:
                return [ResolutionUnits.IS10MIN, 10];       // 10 minutes
            case 3600:
                return [ResolutionUnits.IS1HOUR, 1];        // 1 hour
            default:
                return [ResolutionUnits.CUSTOM, seconds];   // fallback to custom
        }
    }

    parseStateFromInt(seconds: number): ResolutionState {
        if (seconds <= 0) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.AUTO,
                customResolutionValue: 1,
            });
        }
    
        if (seconds >= 3600) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.IS1HOUR,
                customResolutionValue: seconds / 3600,
            });
        }
    
        if (seconds >= 600) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.IS10MIN,
                customResolutionValue: seconds / 600,
            });
        }
    
        if (seconds >= 60) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.IS1MIN,
                customResolutionValue: seconds / 60,
            });
        }
    
        if (seconds >= 10) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.IS10SEC,
                customResolutionValue: seconds / 10,
            });
        }
    
        if (seconds >= 1) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.IS1SEC,
                customResolutionValue: seconds,
            });
        }
    
        throw new Error("Invalid resolution seconds value: " + seconds);
    }
    

    parseStateFromString(valueStr: string, parseToCustomState: boolean = false): ResolutionState {
        if (valueStr in ResolutionUnits && !parseToCustomState) {
            return new ResolutionState({ resolutionUnit: ResolutionUnits[valueStr] });
        }

        let enumPattern = this.getEnumPattern(CustomResolutionUnits);
        let regexPattern = new RegExp(`^IS(\\d{1,5})(${enumPattern})$`);
        let match = valueStr.match(regexPattern);

        if (match) {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.CUSTOM,
                customResolutionValue: parseInt(match[1]),
                customResolutionUnit: CustomResolutionUnits[match[2]],
            });
        } else if (valueStr === 'AUTO') {
            return new ResolutionState({
                resolutionUnit: ResolutionUnits.AUTO,
                customResolutionValue: 1,
                customResolutionUnit: CustomResolutionUnits.HOUR,
            });
        }

        return null;
    }

    parseStateFromBaseState(baseModel: BaseState): ResolutionState {
        let resolutionState = new ResolutionState({ resolutionUnit: ResolutionUnits.CUSTOM });
        switch (baseModel.unit) {
            case BaseUnits.Cycle:
                resolutionState.customResolutionValue = baseModel.value * 20; // 20 is cycle value in MS
                resolutionState.customResolutionUnit = CustomResolutionUnits.MS;
                break;
            case BaseUnits.Second:
                resolutionState.customResolutionValue = baseModel.value;
                resolutionState.customResolutionUnit = CustomResolutionUnits.SEC;
                break;
            case BaseUnits.Minute:
                resolutionState.customResolutionValue = baseModel.value;
                resolutionState.customResolutionUnit = CustomResolutionUnits.MIN;
                break;
            case BaseUnits.Hour:
                resolutionState.customResolutionValue = baseModel.value;
                resolutionState.customResolutionUnit = CustomResolutionUnits.HOUR;
                break;
            case BaseUnits.Day:
                resolutionState.customResolutionValue = baseModel.value * 24;
                resolutionState.customResolutionUnit = CustomResolutionUnits.HOUR;
                break;
            case BaseUnits.Week:
                resolutionState.customResolutionValue = baseModel.value * 24 * 7;
                resolutionState.customResolutionUnit = CustomResolutionUnits.HOUR;
                break;
        }
        return resolutionState;
    }

    fromQuickSelectToCustomModel(resolution: ResolutionUnits): ResolutionState {
        return this.parseStateFromString(resolution, true);
    }

    findMinResolution(resolutions: ResolutionState[], returnInCustomState: boolean = false): ResolutionState {
        let minCustomResolutionUnit: CustomResolutionUnits =
            CustomResolutionUnits[Object.keys(CustomResolutionUnits).at(-1)];
        let minCustomResolutionValue = 99999;

        for (let resolution of resolutions) {
            let resolutionState;

            if (resolution.customResolutionValue && resolution.customResolutionUnit) {
                resolutionState = resolution;
            } else {
                resolutionState = this.fromQuickSelectToCustomModel(resolution.resolutionUnit);
            }

            if (
                this._customResolutionUnitsComparer.less(resolutionState.customResolutionUnit, minCustomResolutionUnit)
            ) {
                minCustomResolutionUnit = resolutionState.customResolutionUnit;
                minCustomResolutionValue = resolutionState.customResolutionValue;
            } else if (
                resolutionState.customResolutionUnit === minCustomResolutionUnit &&
                resolutionState.customResolutionValue < minCustomResolutionValue
            ) {
                minCustomResolutionValue = resolutionState.customResolutionValue;
            }
        }

        return new ResolutionState({
            resolutionUnit: ResolutionUnits.CUSTOM,
            customResolutionValue: minCustomResolutionValue,
            customResolutionUnit: minCustomResolutionUnit,
        });
    }

    findMaxResolution(resolutions: ResolutionState[]): ResolutionState {
        let maxCustomResolutionUnit: CustomResolutionUnits = CustomResolutionUnits.MS;
        let maxCustomResolutionValue = 1;

        for (let resolution of resolutions) {
            let resolutionState;

            if (resolution.customResolutionValue && resolution.customResolutionUnit) {
                resolutionState = resolution;
            } else {
                resolutionState = this.fromQuickSelectToCustomModel(resolution.resolutionUnit);
            }

            if (
                this._customResolutionUnitsComparer.greater(
                    resolutionState.customResolutionUnit,
                    maxCustomResolutionUnit,
                )
            ) {
                maxCustomResolutionUnit = resolutionState.customResolutionUnit;
                maxCustomResolutionValue = resolutionState.customResolutionValue;
            } else if (
                resolutionState.customResolutionUnit === maxCustomResolutionUnit &&
                resolutionState.customResolutionValue > maxCustomResolutionValue
            ) {
                maxCustomResolutionValue = resolutionState.customResolutionValue;
            }
        }

        return new ResolutionState({
            resolutionUnit: ResolutionUnits.CUSTOM,
            customResolutionValue: maxCustomResolutionValue,
            customResolutionUnit: maxCustomResolutionUnit,
        });
    }

    findMaxResolutionByCustomParameter(id: number): Observable<ResolutionState> {
        return this._customParameterServiceProxy.getCustomParameterForView(id).pipe(
            switchMap((response: GetCustomParameterForViewDto) => {
                let customParameter: CustomParameterDto = response.customParameter;
                let baseParameters: Parameter[] = JSON.parse(customParameter.stdpqsParametersList);
                let resolutions: ResolutionState[] = baseParameters.map((parameter: Parameter) =>
                    this.parseStateFromInt(parameter.resolution),
                );
                let result: ResolutionState = this.findMaxResolution(resolutions);
                return of(result);
            }),
        );
    }

    resolutionValueInMs(resolution: ResolutionState): number {
        let customResolution: ResolutionState;
        let result: number;

        if (resolution.resolutionUnit !== ResolutionUnits.CUSTOM) {
            customResolution = this.fromQuickSelectToCustomModel(resolution.resolutionUnit);
        } else {
            customResolution = resolution;
        }

        result =
            customResolution.customResolutionValue * this.getCustomUnitValue(customResolution.customResolutionUnit);

        return result;
    }

    resolutionValueInSeconds(resolution: ResolutionState): number {
        let customResolution: ResolutionState;
        let result: number;

        if (resolution.resolutionUnit !== ResolutionUnits.CUSTOM) {
            customResolution = this.fromQuickSelectToCustomModel(resolution.resolutionUnit);
        } else {
            customResolution = resolution;
        }

        result = customResolution.customResolutionValue * ResolutionState.getSecondCustomParameterUnits(customResolution.customResolutionUnit);
        return result;
    }

    getUIRepresentationForResolution(unit: ResolutionUnits): string {
        if (unit === ResolutionUnits.CUSTOM || unit === ResolutionUnits.AUTO) {
            return ResolutionUnits[unit];
        }

        let state = this.parseStateFromString(unit, true);

        return `${state.customResolutionValue} ${state.customResolutionUnit}`;
    }

    getUIRepresentationForResolutionState(resolution: ResolutionState): string {
        if (resolution.resolutionUnit === ResolutionUnits.CUSTOM) {
            return `${resolution.customResolutionValue} ${resolution.customResolutionUnit}`;
        }

        return this.getUIRepresentationForResolution(resolution.resolutionUnit);
    }

    greaterThan(resolution1: ResolutionState, resolution2: ResolutionState): boolean {
        if (
            resolution1.resolutionUnit === ResolutionUnits.AUTO ||
            resolution2.resolutionUnit === ResolutionUnits.AUTO
        ) {
            return false;
        }

        return this.resolutionValueInMs(resolution1) > this.resolutionValueInMs(resolution2);
    }

    greaterOrEqualThan(resolution1: ResolutionState, resolution2: ResolutionState): boolean {
        if (
            resolution1.resolutionUnit === ResolutionUnits.AUTO ||
            resolution2.resolutionUnit === ResolutionUnits.AUTO
        ) {
            return false;
        }

        return this.resolutionValueInMs(resolution1) >= this.resolutionValueInMs(resolution2);
    }

    lessThan(resolution1: ResolutionState, resolution2: ResolutionState): boolean {
        return this.greaterOrEqualThan(resolution2, resolution1);
    }

    lessOrEqualThan(resolution1: ResolutionState, resolution2: ResolutionState): boolean {
        return this.greaterThan(resolution2, resolution1);
    }

    getDisplayNameForCustomResolution(unit: CustomResolutionUnits): string {
        // const unitDisplayNames: Record<CustomResolutionUnits, string> = {
        //     [CustomResolutionUnits.MS]: 'Millisecond',
        //     [CustomResolutionUnits.SEC]: 'Second',
        //     [CustomResolutionUnits.MIN]: 'Minute',
        //     [CustomResolutionUnits.HOUR]: 'Hour',
        //     [CustomResolutionUnits.DAY]: 'Day',
        //     [CustomResolutionUnits.WEEK]: 'Week',
        // };

        return this._unitDisplayNames[unit] || unit;
    }

    getDisplayNameForResolution(unit: ResolutionUnits): string {
        // const resolutionDisplayNames: Record<ResolutionUnits, string> = {
        //     [ResolutionUnits.AUTO]: 'Auto',
        //     [ResolutionUnits.CUSTOM]: 'Custom Resolution',
        //     [ResolutionUnits.IS1SEC]: '1 Second',
        //     [ResolutionUnits.IS10SEC]: '10 Seconds',
        //     [ResolutionUnits.IS1MIN]: '1 Minute',
        //     [ResolutionUnits.IS10MIN]: '10 Minutes',
        //     [ResolutionUnits.IS1HOUR]: '1 Hour',
        // };

        return this._resolutionDisplayNames[unit] || unit;
    }

    // formatForRequest func is needed to support resolution units which are not present on BE (such as DAY WEEK).
    // it transforms into hour resolution
    formatForRequest(resolution: ResolutionState): string {
        const unitsToConvertToHours = [CustomResolutionUnits.DAY, CustomResolutionUnits.WEEK];

        let newResolution = new ResolutionState(resolution);

        if (newResolution.resolutionUnit === ResolutionUnits.AUTO) {
            return newResolution.resolutionUnit;
        }

        if (newResolution.resolutionUnit !== ResolutionUnits.CUSTOM) {
            newResolution = this.fromQuickSelectToCustomModel(newResolution.resolutionUnit);
        }

        if (unitsToConvertToHours.includes(newResolution.customResolutionUnit)) {
            switch (newResolution.customResolutionUnit) {
                case CustomResolutionUnits.DAY:
                    newResolution.customResolutionValue *= 24;
                    break;
                case CustomResolutionUnits.WEEK:
                    newResolution.customResolutionValue *= 7 * 24;
                    break;
            }
            newResolution.customResolutionUnit = CustomResolutionUnits.HOUR;
        }

        return newResolution.toString();
    }

    // formatForRequest222(resolutionState: ResolutionState): number {

    //     const seconds =  ResolutionState.getSecondCustomParameterUnits(resolutionState.customResolutionUnit);
    //     return seconds;
    // }

    // calculateTotalSeconds(resolutionState: ResolutionState): number {

    //     return resolutionState.calculateTotalSeconds();
    //     if(!resolutionState.customResolutionUnit)
    //     {
    //         throw new Error(`CustomResolutionUnits missing.`)
    //     }

    //     const seconds =  ResolutionState.getSecondCustomParameterUnits(resolutionState.customResolutionUnit);
    //     return  seconds * (resolutionState.customResolutionValue ?? 1);
    // }

    // formatFromRequest func is needed to support resolution units which are not present on BE (such as DAY WEEK).
    // it transforms from hour into unsupported on BE resolution units (Day, Week)
    formatFromRequest(request: ResolutionState) {
        let newResolution = new ResolutionState(request);

        if (newResolution.resolutionUnit === ResolutionUnits.AUTO) {
            return newResolution;
        }

        if (newResolution.resolutionUnit !== ResolutionUnits.CUSTOM) {
            newResolution = this.fromQuickSelectToCustomModel(newResolution.resolutionUnit);
        }

        if (newResolution.customResolutionUnit === CustomResolutionUnits.HOUR) {
            if (newResolution.customResolutionValue % (24 * 7) === 0) {
                newResolution.customResolutionUnit = CustomResolutionUnits.WEEK;
                newResolution.customResolutionValue /= (24 * 7);
            } else if (newResolution.customResolutionValue % 24 === 0) {
                newResolution.customResolutionUnit = CustomResolutionUnits.DAY;
                newResolution.customResolutionValue /= 24;
            }
        }

        return newResolution;
    }

    private getEnumPattern(enumObj: any): string {
        const enumValues = Object.keys(enumObj);
        return enumValues.join('|');
    }

    private getCustomUnitValue(unit: CustomResolutionUnits) {
        switch (unit) {
            case CustomResolutionUnits.MS:
                return 1;
            case CustomResolutionUnits.SEC:
                return 1 * 1000;
            case CustomResolutionUnits.MIN:
                return 1 * 1000 * 60;
            case CustomResolutionUnits.HOUR:
                return 1 * 1000 * 60 * 60;
            case CustomResolutionUnits.DAY:
                return 1 * 1000 * 60 * 60 * 24;
            case CustomResolutionUnits.WEEK:
                return 1 * 1000 * 60 * 60 * 24 * 7;
            default:
                return 0;
        }
    }


    // getCustomUnitValue222(unit: CustomResolutionUnits): number {
    //     switch (unit) {
    //         case CustomResolutionUnits.MS:
    //         case CustomResolutionUnits.SEC:
    //             return 1;
    //         case CustomResolutionUnits.MIN:
    //             return 60;
    //         case CustomResolutionUnits.HOUR:
    //             return 60 * 60;
    //         case CustomResolutionUnits.DAY:
    //             return 60 * 60 * 24;
    //         case CustomResolutionUnits.WEEK:
    //             return 60 * 60 * 24 * 7;
    //         default:
    //             return 0;
    //     }
    // }

}
