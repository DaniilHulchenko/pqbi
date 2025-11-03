import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { QuantityUnits } from '../enums/quantity-units';
import { ComponentsState } from '../models/components-state';
import { AdvancedSettingsConfig } from '../common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';
import { CardWidgetAdvancedSettingsConfig } from './CardWidgetAdvancedSettingsConfig';
import { GaugeWidgetAdvancedSettingsConfig } from './gauge-widget-advanced-settings-config';

export interface AddBaseParameterEventCallBack {
    parameter: Parameter;
    componentsState: ComponentsState;
    quantity: QuantityUnits;
    advancedSettings?: AdvancedSettingsConfig;
    cardWidgetAdvancedSettings?: CardWidgetAdvancedSettingsConfig;
    gaugeWidgetAdvancedSettings?: GaugeWidgetAdvancedSettingsConfig;
}

export interface EditBaseParameterEventCallBack extends AddBaseParameterEventCallBack {
    id: string;
}
