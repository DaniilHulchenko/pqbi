import { QuantityUnits } from '../enums/quantity-units';
import { ColumnType } from '../enums/column-type';
import { ComponentsState } from '../models/components-state';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { AdvancedSettingsConfig } from '../common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';
import { CardWidgetAdvancedSettingsConfig } from './CardWidgetAdvancedSettingsConfig';
import { GaugeWidgetAdvancedSettingsConfig } from './gauge-widget-advanced-settings-config';


export interface WidgetParametersColumn extends Parameter{
    id: string;
    componentsState?: ComponentsState;
    name: string;
    quantity: QuantityUnits;
    type: ColumnType;
    data: string | number;
    advancedSettings?: AdvancedSettingsConfig;
    cardWidgetAdvancedSettings?: CardWidgetAdvancedSettingsConfig;
    gaugeWidgetAdvancedSettings?: GaugeWidgetAdvancedSettingsConfig;
    style: string | null;
}
