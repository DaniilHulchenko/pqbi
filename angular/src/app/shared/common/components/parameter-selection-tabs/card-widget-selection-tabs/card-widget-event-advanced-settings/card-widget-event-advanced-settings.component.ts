import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { uniqBy } from 'lodash-es';
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
import {
    NormalizeEnum,
    EventClassDescription,
    EventClass,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AdvancedSettingsConfig } from '../../advanced-settings/advanced-settings.component';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { EventService } from '@app/shared/services/event-service.service';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'cardWidgetEventAdvancedSettings',
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
        DxFileUploaderModule,
    ],
    templateUrl: './card-widget-event-advanced-settings.component.html',
    styleUrl: './card-widget-event-advanced-settings.component.css',
})
export class CardWidgetEventAdvancedSettingsComponent implements OnInit, OnChanges, OnDestroy {
    @Input() isBaseParameter = false;
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

    decimalPoints = 2;
    titleFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 16,
        colorMode: 'scheme',
        customColor: '#000000',
    };
    valueFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 26,
        colorMode: 'scheme',
        customColor: '#000000',
    };
    icon: { file: string; appearance: 'always' | 'limits'; colorMode: 'scheme' | 'custom'; customColor: string } = {
        file: null,
        appearance: 'always',
        colorMode: 'scheme',
        customColor: '#000000',
    };
    link = { page: null };

    decimalPointOptions = [0, 1, 2, 3];

    fontFamilies = ['Arial', 'Verdana', 'Tahoma', 'Times New Roman', 'Courier New'];

    colorOptions = [
        { id: 'scheme', text: 'use color scheme' },
        { id: 'custom', text: 'set color' },
    ];

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
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.icon = c.icon;
            this.titleFont = c.titleFont;
            this.valueFont = c.valueFont;
        }
    }

    ngOnDestroy(): void {
        this.subscription?.unsubscribe();
    }

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
        this.reset();
        this.modalVisible = false;
    }

    onSelectReplaceAggregation() {}

    save() {
        const config: CardWidgetAdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent: this.excludeFlagged === ExcludeFlagged.UserSelected ? this.selectedFlagEvents : [],
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

    private reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        // this.setLimits = Limit.None;
        // this.lowerLimit = 0;
        // this.upperLimit = 0;
        // this.limitFromNominal = false;
        // this.limitFromNormalization = false;
        // this.colorScheme = ColorSchema.None;
        // this.outOfLimitColor = '';
        // this.gradientFromColor = '';
        // this.gradientToColor = '';
        // this.okColor = '';
        // this.noDataColor = '';
        // this.useOkColor = false;
        // this.useNoDataColor = false;
        // this.showOkColor = false;
        // this.showNoDataColor = false;
        this.decimalPoints = 2;
        this.link.page = null;
        this.icon = { file: null, appearance: 'always', colorMode: 'scheme', customColor: '#000000' };
        this.titleFont = { family: '', size: 16, colorMode: 'scheme', customColor: '#000000' };
        this.valueFont = { family: '', size: 26, colorMode: 'scheme', customColor: '#000000' };
    }
}
