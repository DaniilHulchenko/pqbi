import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@node_modules/@angular/common';
import { DxButtonModule, DxColorBoxModule, DxNumberBoxModule, DxPopoverModule,
     DxPopupModule, DxRadioGroupModule, DxScrollViewModule, DxSelectBoxModule, DxTextBoxModule } from '@node_modules/devextreme-angular';
import { DataValueStatus, EventClassDescription, PQSRestApiServiceProxy } from '@shared/service-proxies/service-proxies';

@Component({
    standalone: true,
    imports: [DxPopupModule, DxScrollViewModule, DxRadioGroupModule, DxTextBoxModule,
         DxColorBoxModule, DxButtonModule, CommonModule, DxNumberBoxModule, DxSelectBoxModule],
    selector: 'advanced-settings',
    templateUrl: './advanced-settings.component.html',
    styleUrls: ['./advanced-settings.component.css']
})
export class AdvancedSettingsComponent implements OnInit, OnChanges {
    @Input() isBaseParameter = false;
    @Input() initialConfig: AdvancedSettingsConfig | null = null;
    @Output() advancedSettingsChanged = new EventEmitter<AdvancedSettingsConfig>();

    modalVisible = false;
    normalizationOptions: any[];
    flaggingEvents: EventClassDescription[] = [];

    normalizeValue = 'none';
    normalizeNominalValue = 0;
    excludeFlagged = 'none';
    defaultFlagEvent: EventClassDescription | null = null;
    setLimits = 'none';
    lowerLimit = 0;
    upperLimit = 0;
    limitFromNominal = false;
    limitFromNormalization = false;
    colorScheme = 'none';
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';
    tagValueCalculation = 'none';
    aligningIgnored = false;
    replaceAggregation = false;
    customAggregationFunc = '';

    aggregationFuncOptions = [
        { value: 'AVG', text: 'Average' },
        { value: 'MAX', text: 'Maximum' },
        { value: 'MIN', text: 'Minimum' }
    ];

    constructor(private _pqsApi: PQSRestApiServiceProxy) {}

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
        this._pqsApi.pQSEvents().subscribe(evts => this.flaggingEvents = evts);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.initialConfig && this.initialConfig) {
            const c = this.initialConfig;
            this.normalizeValue         = c.normalizeValue;
            this.normalizeNominalValue  = +c.normalizeNominalValue;
            this.excludeFlagged         = c.excludeFlagged;
            this.defaultFlagEvent       = c.defaultFlagEvent ?? null;
            this.setLimits              = c.setLimits;
            this.lowerLimit             = +c.lowerLimit;
            this.upperLimit             = +c.upperLimit;
            this.limitFromNominal       = c.limitFromNominal;
            this.limitFromNormalization = c.limitFromNormalization;
            this.colorScheme            = c.colorScheme;
            this.outOfLimitColor        = c.outOfLimitColor;
            this.gradientFromColor      = c.gradientFromColor;
            this.gradientToColor        = c.gradientToColor;
            this.okColor                = c.okColor;
            this.noDataColor            = c.noDataColor;
            this.tagValueCalculation    = c.tagValueCalculation;
            this.aligningIgnored        = c.aligningIgnored;
            this.replaceAggregation     = c.replaceAggregation;
            this.customAggregationFunc  = c.customAggregationFunc;
        }
    }

    getNormalizationOptions() {
        const opts = [{ value: 'none', text: 'No selection' }];
        if (this.isBaseParameter) {
            opts.push({ value: 'nominal', text: 'Yes – nominal' });
        }
        opts.push({ value: 'other', text: 'Yes – with other Normalization Value' });
        return opts;
    }

    show() {
        this.modalVisible = true;
    }

    hide() {
        this.modalVisible = false;
    }

    save() {
        const config: AdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue.toString(),
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent:
                this.excludeFlagged === 'defaultEvents'
                    ? this.defaultFlagEvent
                    : null,
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit.toString(),
            upperLimit: this.upperLimit.toString(),
            limitFromNominal: this.limitFromNominal,
            limitFromNormalization: this.limitFromNormalization,
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
            gradientFromColor: this.gradientFromColor,
            gradientToColor: this.gradientToColor,
            okColor: this.okColor,
            noDataColor: this.noDataColor,
            tagValueCalculation: this.tagValueCalculation,
            aligningIgnored: this.aligningIgnored,
            replaceAggregation: this.replaceAggregation,
            customAggregationFunc: this.customAggregationFunc,
        };
        this.advancedSettingsChanged.emit(config);
        this.hide();
    }
}

export interface AdvancedSettingsConfig {
    // normalisation
    normalizeValue?: string;
    normalizeNominalValue?: string;

    // flagging
    excludeFlagged?: string;
    defaultFlagEvent?: EventClassDescription | null;

    // limits
    setLimits?: string;
    lowerLimit?: string;
    upperLimit?: string;
    limitFromNominal?: boolean;
    limitFromNormalization?: boolean;

    // colors
    colorScheme?: string;
    outOfLimitColor?: string;
    gradientFromColor?: string;
    gradientToColor?: string;
    okColor?: string;
    noDataColor?: string;

    // tag‐value calc
    tagValueCalculation?: string;
    aligningIgnored?: boolean;
    replaceAggregation?: boolean;
    customAggregationFunc?: string;
}
