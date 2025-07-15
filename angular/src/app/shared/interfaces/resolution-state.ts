import { CustomResolutionUnits } from '../enums/custom-resolution-selection-units';
import { ResolutionUnits } from '../enums/resolution-selection-units';

export interface IResolutionState {
    resolutionUnit: ResolutionUnits;
    customResolutionValue?: number;
    customResolutionUnit?: CustomResolutionUnits;
}
