import { FeederComponentInfo } from "@shared/service-proxies/service-proxies";

export interface Parameter {
    id?: number | string;
    name?: string;
    type?: string; // logical | channel | additional
    //feeder?: any;
    // groupType?: any;
    group?: string;
    phase?: string;
    // channel?: string; // TBD: both phase and channel are part of a phase object
    harmonics?: Harmonics | null;
    baseResolution?: string;
    quantity?: string;
    resolution: number;
    operator?: string;
    aggregationFunction?: string;
    fromComponents?: FeederComponentInfo;
    advancedSettings?: any;
}

export interface Harmonics {
    range?: string;
    rangeOn?: string;
    value?: number;
}

export interface FromComponent {
    componentId: string;
    id: number | undefined;
}
