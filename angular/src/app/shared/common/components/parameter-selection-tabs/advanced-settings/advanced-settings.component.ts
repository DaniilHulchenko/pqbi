import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';
import { CommonModule } from '@node_modules/@angular/common';
import { DxButtonModule, DxColorBoxModule, DxNumberBoxModule,
     DxPopupModule, DxRadioGroupModule, DxScrollViewModule, DxSelectBoxModule, DxTextBoxModule } from '@node_modules/devextreme-angular';
import { EventClass, EventClassDescription, NormalizeEnum, PQSRestApiServiceProxy } from '@shared/service-proxies/service-proxies';
import { MultiSelectModule } from 'primeng/multiselect';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { RadioButtonModule } from 'primeng/radiobutton';
import { uniqBy } from 'lodash-es';

@Component({
    standalone: true,
    imports: [
        DxPopupModule,
        DxScrollViewModule,
        DxRadioGroupModule,
        DxTextBoxModule,
        DxColorBoxModule,
        DxButtonModule,
        CommonModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        MultiSelectModule,
        FormsModule,
        CheckboxModule,
        RadioButtonModule,
    ],
    selector: 'advanced-settings',
    templateUrl: './advanced-settings.component.html',
    styleUrls: ['./advanced-settings.component.css'],
})
export class AdvancedSettingsComponent implements OnInit, OnChanges {
    @Input() isBaseParameter = false;
    @Output() advancedSettingsChanged = new EventEmitter<AdvancedSettingsConfig>();
    @Input() config: AdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<AdvancedSettingsConfig>();

    modalVisible = false;
    normalizationOptions: any[];
    normalizeTypes = NormalizeEnum;
    excludeFlaggedTypes = ExcludeFlagged;
    limitTypes = Limit;
    colorSchemaTypes = ColorSchema ;
    flaggingEvents: EventClassDescription[] = [];

    normalizeValue = NormalizeEnum.NO;
    normalizeNominalValue = 0;
    excludeFlagged = ExcludeFlagged.None;
    selectedFlagEvents: EventClass[] = [];
    setLimits = Limit.None;
    lowerLimit = 0;
    upperLimit = 0;
    limitFromNominal = false;
    limitFromNormalization = false;
    colorScheme: ColorSchema | null = null;
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';
    aligningIgnored = false;
    replaceAggregation = false;
    customAggregationFunc = '';
    useOkColor = false;
    useNoDataColor = false;
    showOkColor?: boolean;
    showNoDataColor?: boolean;

    excludeFlaggedOptions = [
        { value: ExcludeFlagged.DefaultEvents, text: 'yes  (with Dip, Swell, Interrupt)' },
        { value: ExcludeFlagged.UserSelected, text: 'yes - with selected events' },
    ];

    limitOptions = [
        { value: Limit.Fixed, text: 'lower limit / upper limit' },
        { value: Limit.PercentNominal, text: 'lower limit % from nominal / Upper limit % from nominal' },
        {
            value: Limit.PercentNormalization,
            text: 'lower limit % from Normalization value / Upper limit % from Normalization value',
        },
    ];

    colorSchemaOptions = [
        { value: ColorSchema.OutOfLimit, text: 'Out-of-limit color' },
        { value: ColorSchema.Gradient, text: 'Out of limits gradient - from:   to:' },
    ];

    aggregationFuncOptions = [
        { value: 'AVG', text: 'Average' },
        { value: 'MAX', text: 'Maximum' },
        { value: 'MIN', text: 'Minimum' },
    ];

    constructor(private _pqsApi: PQSRestApiServiceProxy) {}

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
        this._pqsApi.pQSEvents().subscribe((evts) => {
            this.flaggingEvents = evts;
            this.flaggingEvents = uniqBy(this.flaggingEvents, (x) => x.eventClass);
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.config && this.config) {
            const c = this.config;
            this.normalizeValue = c.normalizeValue;
            this.normalizeNominalValue = +c.normalizeNominalValue;
            this.excludeFlagged = c.excludeFlagged;
            this.selectedFlagEvents = c.defaultFlagEvent ?? null;
            this.setLimits = c.setLimits;
            this.lowerLimit = +c.lowerLimit;
            this.upperLimit = +c.upperLimit;
            this.limitFromNominal = c.limitFromNominal;
            this.limitFromNormalization = c.limitFromNormalization;
            this.colorScheme = c.colorScheme;
            this.outOfLimitColor = c.outOfLimitColor;
            this.gradientFromColor = c.gradientFromColor;
            this.gradientToColor = c.gradientToColor;
            this.okColor = c.okColor;
            this.noDataColor = c.noDataColor;
            this.aligningIgnored = c.aligningIgnored;
            this.replaceAggregation = c.replaceAggregation;
            this.customAggregationFunc = c.customAggregationFunc;
        }
    }

    getNormalizationOptions() {
        const opts = [];
        // if (this.isBaseParameter) {
        opts.push({ value: NormalizeEnum.NOMINAL, text: 'yes - by nominal value' });
        // }
        opts.push({ value: NormalizeEnum.VALUE, text: 'yes - by Normalization Value:' });
        return opts;
    }

    show() {
        this.modalVisible = true;
    }

    hide() {
        this.reset();
        this.modalVisible = false;
    }

    onSelectReplaceAggregation(){
        console.log('Replace aggregation selected', this.replaceAggregation);
    }

    save() {
        const config: AdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent: this.excludeFlagged === ExcludeFlagged.UserSelected ? this.selectedFlagEvents : [],
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit,
            upperLimit: this.upperLimit,
            limitFromNominal: this.limitFromNominal,
            limitFromNormalization: this.limitFromNormalization,
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
            gradientFromColor: this.gradientFromColor,
            gradientToColor: this.gradientToColor,
            okColor: this.okColor,
            noDataColor: this.noDataColor,
            aligningIgnored: this.aligningIgnored,
            replaceAggregation: this.replaceAggregation,
            customAggregationFunc: this.customAggregationFunc,
            showOkColor: this.showOkColor,
            showNoDataColor: this.showNoDataColor,
        };
        this.configChange.emit(config);
        this.hide();
    }
    onSelectNormalize(value: NormalizeEnum) {
        this.normalizeValue = this.normalizeValue === value ? NormalizeEnum.NO : value;
    }
    onSelectExcludeFlagged(value: ExcludeFlagged) {
        this.excludeFlagged = this.excludeFlagged === value ? null : value;
    }
    onSelectLimitType(value: Limit) {
        this.setLimits = this.setLimits === value ? null : value;
    }
    onSelectColorScheme(value: ColorSchema) {
        this.colorScheme = this.colorScheme === value ? null : value;
    }
    onColorClosed(type: 'from' | 'to') {
        if (type === 'from' && !this.gradientFromColor) {
            this.gradientFromColor = '#000000';
        }
        if (type === 'to' && !this.gradientToColor) {
            this.gradientToColor = '#000000';
        }
    }
    private reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        this.setLimits = Limit.None;
        this.lowerLimit = 0;
        this.upperLimit = 0;
        this.limitFromNominal = false;
        this.limitFromNormalization = false;
        this.colorScheme = ColorSchema.None;
        this.outOfLimitColor = '';
        this.gradientFromColor = '';
        this.gradientToColor = '';
        this.okColor = '';
        this.noDataColor = '';
        this.aligningIgnored = false;
        this.replaceAggregation = false;
        this.customAggregationFunc = '';
        this.useOkColor = false;
        this.useNoDataColor = false;
        this.showOkColor = false;
        this.showNoDataColor = false;
    }
}


export interface AdvancedSettingsConfig {
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

    // tag‐value calc
    aligningIgnored?: boolean;
    replaceAggregation?: boolean;
    customAggregationFunc?: string;

    showOkColor?: boolean;
    showNoDataColor?: boolean;

}
