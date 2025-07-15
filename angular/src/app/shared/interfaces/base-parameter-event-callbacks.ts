import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { QuantityUnits } from '../enums/quantity-units';
import { ComponentsState } from '../models/components-state';
import { AdvancedSettingsConfig } from '../common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';

export interface AddBaseParameterEventCallBack {
    parameter: Parameter;
    componentsState: ComponentsState;
    quantity: QuantityUnits;
    advancedSettings?: AdvancedSettingsConfig;
}

export interface EditBaseParameterEventCallBack extends AddBaseParameterEventCallBack {
    id: string;
}
