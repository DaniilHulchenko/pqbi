import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { uniqBy } from 'lodash-es';
import {
    NormalizeEnum,
    EventClassDescription,
    EventClass,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AdvancedSettingsConfig } from '../../advanced-settings/advanced-settings.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    DxPopupModule,
    DxScrollViewModule,
    DxRadioGroupModule,
    DxTextBoxModule,
    DxColorBoxModule,
    DxButtonModule,
    DxNumberBoxModule,
    DxSelectBoxModule,
    DxFileUploaderModule,
} from 'devextreme-angular';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { EventService } from '@app/shared/services/event-service.service';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'cardWidgetParameterAdvancedSettings',
    standalone: true,
    imports: [
        DxPopupModule,
        DxScrollViewModule,
        DxRadioGroupModule,
        DxTextBoxModule,
        DxColorBoxModule,
        DxButtonModule,
        DxFileUploaderModule,
        CommonModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        MultiSelectModule,
        FormsModule,
        CheckboxModule,
        RadioButtonModule,
    ],
    templateUrl: './card-widget-parameter-advanced-settings.component.html',
    styleUrl: './card-widget-parameter-advanced-settings.component.css',
})
export class CardWidgetParameterAdvancedSettingsComponent implements OnInit, OnChanges, OnDestroy {
    @Output() advancedSettingsChanged = new EventEmitter<AdvancedSettingsConfig>();
    @Input() config: CardWidgetAdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<CardWidgetAdvancedSettingsConfig>();

    modalVisible = false;
    normalizationOptions: any[];
    normalizeTypes = NormalizeEnum;
    excludeFlaggedTypes = ExcludeFlagged;
    limitTypes = Limit;
    colorSchemaTypes = ColorSchema;
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
    colorScheme = ColorSchema.None;
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';
    useOkColor = false;
    useNoDataColor = false;
    showOkColor?: boolean;
    showNoDataColor?: boolean;
    decimalPoints = 2;
    titleFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 16,
        colorMode: 'custom',
        customColor: '#000000',
    };
    valueFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 26,
        colorMode: 'custom',
        customColor: '#000000',
    };
    icon: { file: string; appearance: 'always' | 'limits'; colorMode: 'scheme' | 'custom'; customColor: string } = {
        file: null,
        appearance: 'always',
        colorMode: 'custom',
        customColor: '#000000',
    };
    link = { page: null };

    decimalPointOptions = [0, 1, 2, 3];

    fontFamilies = ['Arial', 'Verdana', 'Tahoma', 'Times New Roman', 'Courier New'];

    appearanceOptions = [
        { id: 'always', text: 'show always' },
        { id: 'limits', text: 'show only if limits are exceeded' },
    ];

    pqbiPages: { id: string; name: string }[] = [];

    private subscription: Subscription;

    excludeFlaggedOptions = [
        { value: ExcludeFlagged.DefaultEvents, text: 'yes  (with Dip, Swell, Interrupt)' },
        { value: ExcludeFlagged.UserSelected, text: 'yes - with selected events' },
    ];

    limitOptions = [
        { value: Limit.Fixed, text: 'lower limit / upper limit' },
        { value: Limit.PercentNominal, text: 'lower limit % from nominal / Upper limit % from nominal' },
        {
            value: Limit.PercentNormalization,
            text: 'lower limit from Normalization value / Upper limit from Normalization value',
        },
    ];

    colorSchemaOptions = [
        { value: ColorSchema.None, text: 'No color highlight' },
        { value: ColorSchema.OutOfLimit, text: 'Out-of-limit color' },
        { value: ColorSchema.Gradient, text: 'Out of limits gradient - from:   to:' },
    ];

    aggregationFuncOptions = [
        { value: 'AVG', text: 'Average' },
        { value: 'MAX', text: 'Maximum' },
        { value: 'MIN', text: 'Minimum' },
    ];

    constructor(private _eventService: EventService, private dashboardPagesService: DashboardPagesService) {}

    onFileChanged(e: any) {
        const file = e.value[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = () => {
                this.icon.file = reader.result as string;
            };
            reader.readAsDataURL(file);
        }
    }

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
        this._eventService.pqsEvents().subscribe((evts) => {
            this.flaggingEvents = evts;
            this.flaggingEvents = uniqBy(this.flaggingEvents, (x) => x.eventClass);
        });
            this.subscription = this.dashboardPagesService.getPages().subscribe((pages) => {
            this.pqbiPages = pages.map((page) => ({ id: page.id, name: page.name }));
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
            this.useOkColor = !!this.okColor;
            this.useNoDataColor = !!this.noDataColor;
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.icon = this.normalizeIconSettings(c.icon);
            this.titleFont = this.normalizeFontSettings(c.titleFont, 16);
            this.valueFont = this.normalizeFontSettings(c.valueFont, 26);
        }
    }
    ngOnDestroy(): void {
        this.subscription?.unsubscribe();
    }

    getNormalizationOptions() {
        const opts = [];
        // if (this.isBaseParameter) {
        opts.push({ value: NormalizeEnum.NOMINAL, text: 'yes - by nominal value' });
        // }
        opts.push({ value: NormalizeEnum.VALUE, text: 'yes - by Normalization Value:' });
        return opts;
    }

    show(settings: CardWidgetAdvancedSettingsConfig | null = null) {
        if (settings) {
            this.config = settings;
            this.ngOnChanges({
                config: { currentValue: settings, previousValue: null, firstChange: true, isFirstChange: () => true },
            });
        }
        this.modalVisible = true;
    }

    hide() {
        this.modalVisible = false;
        this.reset();
    }

    onSelectReplaceAggregation() {}

    save() {
        const config: CardWidgetAdvancedSettingsConfig = {
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
            showOkColor: this.useOkColor,
            showNoDataColor: this.useNoDataColor,
            decimalPoints: this.decimalPoints,
            linkPage: this.link.page,
            icon: this.icon,
            titleFont: this.titleFont,
            valueFont: this.valueFont,
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
    reset() {
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
        this.useOkColor = false;
        this.useNoDataColor = false;
        this.showOkColor = false;
        this.showNoDataColor = false;
        this.decimalPoints = 2;
        this.link.page = null;
        this.icon = this.normalizeIconSettings(null);
        this.titleFont = this.normalizeFontSettings(null, 16);
        this.valueFont = this.normalizeFontSettings(null, 26);
    }

    private normalizeFontSettings(
        font: { family?: string; size?: number; colorMode?: 'scheme' | 'custom'; customColor?: string } | null,
        defaultSize: number,
    ): { family: string; size: number; colorMode: 'custom'; customColor: string } {
        return {
            family: font?.family ?? '',
            size: font?.size ?? defaultSize,
            colorMode: 'custom',
            customColor: font?.customColor ?? '#000000',
        };
    }

    private normalizeIconSettings(
        icon: { file?: string; appearance?: 'always' | 'limits'; colorMode?: 'scheme' | 'custom'; customColor?: string } | null,
    ): { file: string; appearance: 'always' | 'limits'; colorMode: 'custom'; customColor: string } {
        return {
            file: icon?.file ?? null,
            appearance: icon?.appearance ?? 'always',
            colorMode: 'custom',
            customColor: icon?.customColor ?? '#000000',
        };
    }
}
