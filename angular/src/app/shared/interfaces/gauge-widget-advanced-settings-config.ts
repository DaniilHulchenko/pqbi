import { NormalizeEnum, EventClass } from '@shared/service-proxies/service-proxies';
import { ColorSchema, ExcludeFlagged, Limit } from '../enums/advanced-settings-options';

export interface GaugeWidgetAdvancedSettingsConfig {
    parameterName?: string;
    // normalization
    normalizeValue?: NormalizeEnum;
    normalizeNominalValue?: number;

    // flagging
    excludeFlagged?: ExcludeFlagged;
    defaultFlagEvent?: EventClass[] | null;

    // limits
    setLimits?: Limit;
    lowerLimit?: number;
    upperLimit?: number;
    limitFromNominal?: boolean;
    limitFromNormalization?: boolean;

    // colors
    colorScheme?: ColorSchema;
    outOfLimitColor?: string;

    // decimal points
    decimalPoints?: number;

    linkPage: string | null;

    titleFont?: { family?: string; size?: number | null; colorMode?: 'scheme' | 'custom'; customColor?: string };
    valueFont?: { family?: string; size?: number | null; colorMode?: 'scheme' | 'custom'; customColor?: string };

    // markers
    marker1?: string;
    marker2?: string;

    // units
    unit: Unit;

    segments?: Segment[];
}

export interface Segment {
    id: string;
    name: string;
    from: number;
    to: number;
    colorMode: 'scheme' | 'custom';
    color: string | null;
    weight?: number;
}

export interface Unit {
    unitType: 'auto' | 'selection';
    selectedUnit: string | null;
}
