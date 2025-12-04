import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    DxPopupModule,
    DxScrollViewModule,
    DxTextBoxModule,
    DxColorBoxModule,
    DxButtonModule,
    DxFileUploaderModule,
    DxNumberBoxModule,
    DxRadioGroupModule,
    DxSelectBoxModule,
} from 'devextreme-angular';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import {
    EventClass,
    EventClassDescription,
    NormalizeEnum,
} from '@shared/service-proxies/service-proxies';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';
import { uniqBy } from 'lodash-es';
import { GaugeWidgetAdvancedSettingsConfig, Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { EventService } from '@app/shared/services/event-service.service';
import { GaugeWidgetSegmentationSettingsComponent } from '../shared/gauge-widget-segmentation-settings/gauge-widget-segmentation-settings.component';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';





@Component({
    selector: 'gaugeWidgetParameterAdvancedSettings',
    standalone: true,
    imports: [
        DxPopupModule,
        DxScrollViewModule,
        DxTextBoxModule,
        DxColorBoxModule,
        DxButtonModule,
        DxFileUploaderModule,
        CommonModule,
        DxNumberBoxModule,
        DxRadioGroupModule,
        DxSelectBoxModule,
        MultiSelectModule,
        FormsModule,
        CheckboxModule,
        RadioButtonModule,
        GaugeWidgetSegmentationSettingsComponent
    ],
    templateUrl: './gauge-widget-parameter-advanced-settings.component.html',
    styleUrl: './gauge-widget-parameter-advanced-settings.component.css',
})
export class GaugeWidgetParameterAdvancedSettingsComponent implements OnInit, OnChanges, OnDestroy {
    @Output() advancedSettingsChanged = new EventEmitter<GaugeWidgetAdvancedSettingsConfig>();
    @Input() config: GaugeWidgetAdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<GaugeWidgetAdvancedSettingsConfig>();

    modalVisible = false;
    normalizationOptions: any[];
    normalizeTypes = NormalizeEnum;
    excludeFlaggedTypes = ExcludeFlagged;
    colorSchemaTypes = ColorSchema;
    flaggingEvents: EventClassDescription[] = [];

    normalizeValue = NormalizeEnum.NO;
    normalizeNominalValue = 0;
    excludeFlagged = ExcludeFlagged.None;
    selectedFlagEvents: EventClass[] = [];
    colorScheme = ColorSchema.None;
    outOfLimitColor = '';
    decimalPoints = 2;
    titleFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 20,
        colorMode: 'custom',
        customColor: '#000000',
    };
    valueFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 20,
        colorMode: 'custom',
        customColor: '#000000',
    };
    link = { page: null };
    unitType: 'auto' | 'selection' = 'auto';
    selectedUnit = '';
    marker1: string = 'None';
    marker2: string = 'None';

    @ViewChild('segmentation') segmentationComponent?: GaugeWidgetSegmentationSettingsComponent;

    segments: Segment[] = [];
    isSegmentsValid = false;
    totalWeight = 0;

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

    unitOptions = [
        { text: 'Auto-detect', value: 'auto' },
        { text: 'Use selection', value: 'selection' },
    ];

    unitSelectionOptions = [];

    markerOptions = [
        { text: 'None', value: null },
        { text: 'AVG', value: 'AVG' },
        { text: 'MIN', value: 'MIN' },
        { text: 'MAX', value: 'MAX' },
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
            this.excludeFlagged = c.excludeFlagged ?? ExcludeFlagged.None;
            this.selectedFlagEvents = c.defaultFlagEvent ?? [];
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.titleFont = this.normalizeFontSettings(c.titleFont, 20);
            this.valueFont = this.normalizeFontSettings(c.valueFont, 20);
            this.isSegmentsValid = false;
            this.totalWeight = 0;
            this.segments = c.segments ? c.segments.map((segment) => ({ ...segment })) : [];
            this.unitType = c.unit?.unitType || 'auto';
            this.selectedUnit = c.unit?.selectedUnit || '';
            this.marker1 = c.marker1 || null;
            this.marker2 = c.marker2 || null;
            this.colorScheme = c.colorScheme;
            this.outOfLimitColor = c.outOfLimitColor;
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

    show(settings: GaugeWidgetAdvancedSettingsConfig | null = null) {
        if (settings) {
            this.config = settings;
            this.ngOnChanges({ config: { currentValue: settings, previousValue: null, firstChange: true, isFirstChange: () => true } });
        }
        this.modalVisible = true;
    }

    hide() {
        this.modalVisible = false;
        this.reset();
    }

    onSegmentsChange(segments: Segment[]) {
        this.segments = segments.map((segment) => ({ ...segment }));
    }




    onSegmentsValidityChange(isValid: boolean) {
        this.isSegmentsValid = isValid;
    }

    onSegmentsTotalWeightChange(total: number) {
        this.totalWeight = total;
    }

    save() {
        if (!this.canSave) {
            this.segmentationComponent?.validateBeforeSave();
            return;
        }

        const lowerLimit = this.getSegmentsMin();
        const upperLimit = this.getSegmentsMax();

        if (lowerLimit == null || upperLimit == null) {
            this.segmentationComponent?.validateBeforeSave();
            return;
        }
        const config: GaugeWidgetAdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent: this.excludeFlagged === ExcludeFlagged.UserSelected ? this.selectedFlagEvents : [],
            setLimits: Limit.None,
            lowerLimit,
            upperLimit,
            limitFromNominal: false,
            limitFromNormalization: false,
            decimalPoints: this.decimalPoints,
            linkPage: this.link.page,
            titleFont: this.titleFont,
            valueFont: this.valueFont,
            segments: this.segments,
            unit: {unitType: this.unitType, selectedUnit: this.selectedUnit},
            marker1: this.marker1,
            marker2: this.marker2,
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
        };
        this.advancedSettingsChanged.emit(config);
        this.configChange.emit(config);
        this.hide();
    }
    onSelectNormalize(value: NormalizeEnum) {
        this.normalizeValue = this.normalizeValue === value ? NormalizeEnum.NO : value;
    }
    onSelectExcludeFlagged(value: ExcludeFlagged) {
        this.excludeFlagged = this.excludeFlagged === value ? ExcludeFlagged.None : value;
        if (this.excludeFlagged !== ExcludeFlagged.UserSelected) {
            this.selectedFlagEvents = [];
        }
    }

    get canSave(): boolean {
        return this.isSegmentsValid;
    }

    get minScale(): number | null {
        return this.getSegmentsMin();
    }

    get maxScale(): number | null {
        return this.getSegmentsMax();
    }

    private getSegmentsMin(): number | null {
        if (!this.segments.length) {
            return null;
        }
        return Math.min(...this.segments.map((segment) => segment.from));
    }

    private getSegmentsMax(): number | null {
        if (!this.segments.length) {
            return null;
        }
        return Math.max(...this.segments.map((segment) => segment.to));
    }



    reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        this.decimalPoints = 2;
        this.link.page = null;
        this.titleFont = this.normalizeFontSettings(null, 20);
        this.valueFont = this.normalizeFontSettings(null, 20);
        this.segments = [];
        this.isSegmentsValid = false;
        this.totalWeight = 0;
        this.unitType = 'auto';
        this.selectedUnit = '';
        this.marker1 = null;
        this.marker2 = null;
        this.colorScheme = ColorSchema.None;
        this.outOfLimitColor = '';
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
}
