import { EventClass, NormalizeEnum } from "@shared/service-proxies/service-proxies";
import { ColorSchema, ExcludeFlagged, Limit } from "../enums/advanced-settings-options";

export interface CardWidgetAdvancedSettingsConfig {
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
    gradientFromColor?: string;
    gradientToColor?: string;
    okColor?: string;
    noDataColor?: string;

    showOkColor?: boolean;
    showNoDataColor?: boolean;

    // decimal points
    decimalPoints?: number;

    linkPage: string | null;

    icon: {
        file: string;
        iconId: string | null;
        defaultIconId?: string | null;
        defaultValueKey?: string | null;
        setAsDefaultIcon?: boolean;
        appearance: 'always' | 'limits';
        colorMode: 'scheme' | 'custom';
        customColor: string;
    };

    titleFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string };
    valueFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string };
}