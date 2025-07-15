import { Injectable } from '@angular/core';
import { CustomResolutionUnits } from '../enums/custom-resolution-selection-units';

@Injectable({
    providedIn: 'root',
})
export class CustomResolutionUnitsComparer {
    private readonly _options = Object.keys(CustomResolutionUnits);

    greaterOrEqual(unit1: CustomResolutionUnits, unit2: CustomResolutionUnits): boolean {
        return this._options.indexOf(unit1) >= this._options.indexOf(unit2);
    }

    greater(unit1: CustomResolutionUnits, unit2: CustomResolutionUnits): boolean {
        return this._options.indexOf(unit1) > this._options.indexOf(unit2);
    }

    lessOrEqual(unit1: CustomResolutionUnits, unit2: CustomResolutionUnits): boolean {
        return this.greaterOrEqual(unit2, unit1);
    }

    less(unit1: CustomResolutionUnits, unit2: CustomResolutionUnits): boolean {
        return this.greater(unit2, unit1);
    }

}
