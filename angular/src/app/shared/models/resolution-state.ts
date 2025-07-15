import { CustomResolutionUnits } from '../enums/custom-resolution-selection-units';
import { ResolutionUnits } from '../enums/resolution-selection-units';
import { IResolutionState } from '../interfaces/resolution-state';
import safeStringify from 'fast-safe-stringify';

export class ResolutionState implements IResolutionState {
    resolutionUnit: ResolutionUnits;
    customResolutionValue?: number;
    customResolutionUnit?: CustomResolutionUnits;

    constructor(state: IResolutionState) {
        this.resolutionUnit = state.resolutionUnit;
        this.customResolutionUnit = state.customResolutionUnit;
        this.customResolutionValue = state.customResolutionValue;
    }

    get isAuto(): boolean {
        return this.resolutionUnit === ResolutionUnits.AUTO;
    }

    static fromJSON(json: string): ResolutionState {
        let state: IResolutionState = JSON.parse(json);
        return new ResolutionState({
            resolutionUnit: state.resolutionUnit,
            customResolutionUnit: state.customResolutionUnit,
            customResolutionValue: state.customResolutionValue,
        });
    }

    toString(): string {
        return this.resolutionUnit === ResolutionUnits.CUSTOM
            ? `IS${this.customResolutionValue}${this.customResolutionUnit}`
            : this.resolutionUnit;
    }

    toJSON(): string {
        let object = {
            resolutionUnit: this.resolutionUnit,
            customResolutionValue: this.customResolutionValue,
            customResolutionUnit: this.customResolutionUnit,
        };
        return safeStringify(object);
    }

    equalsTo(other: ResolutionState): boolean {
        return (
            this.resolutionUnit === other.resolutionUnit &&
            this.customResolutionValue === other.customResolutionValue &&
            this.customResolutionUnit === other.customResolutionUnit
        );
    }


    calculateTotalSeconds() {

        if (!this.customResolutionUnit) {
            throw new Error(`CustomResolutionUnits missing.`)
        }

        const seconds = ResolutionState.getSecondCustomParameterUnits(this.customResolutionUnit);
        return seconds * (this.customResolutionValue ?? 1);
    }

    getSecondCustomParameterUnits(): number {

       return ResolutionState.getSecondCustomParameterUnits(this.customResolutionUnit);
    }

    static getSecondCustomParameterUnits(unit: CustomResolutionUnits): number {
        switch (unit) {
            case CustomResolutionUnits.MS:
            case CustomResolutionUnits.SEC:
                return 1;
            case CustomResolutionUnits.MIN:
                return 60;
            case CustomResolutionUnits.HOUR:
                return 60 * 60;
            case CustomResolutionUnits.DAY:
                return 60 * 60 * 24;
            case CustomResolutionUnits.WEEK:
                return 60 * 60 * 24 * 7;
            default:
                return 0;
        }
    }

}
