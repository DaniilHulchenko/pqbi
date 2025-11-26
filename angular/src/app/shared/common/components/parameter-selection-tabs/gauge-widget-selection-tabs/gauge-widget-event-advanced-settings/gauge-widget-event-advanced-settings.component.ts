import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';
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
import { NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { GaugeWidgetAdvancedSettingsConfig, Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { GaugeWidgetSegmentationSettingsComponent } from '../shared/gauge-widget-segmentation-settings/gauge-widget-segmentation-settings.component';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';


@Component({
    selector: 'gaugeWidgetEventAdvancedSettings',
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
        GaugeWidgetSegmentationSettingsComponent,
    ],
    templateUrl: './gauge-widget-event-advanced-settings.component.html',
    styleUrl: './gauge-widget-event-advanced-settings.component.css',
})
export class GaugeWidgetEventAdvancedSettingsComponent implements OnInit, OnChanges, OnDestroy {
    @Input() config: GaugeWidgetAdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<GaugeWidgetAdvancedSettingsConfig>();

    modalVisible = false;
    normalizationOptions: any[];
    normalizeTypes = NormalizeEnum;

    normalizeValue = NormalizeEnum.NO;
    normalizeNominalValue = 0;

    colorScheme = ColorSchema.None;
    outOfLimitColor = '';

    setLimits = Limit.None;
    lowerLimit = 0;
    upperLimit = 0;

    decimalPoints = 2;
    titleFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 20,
        colorMode: 'scheme',
        customColor: '#000000',
    };
    valueFont: { family: string; size: number; colorMode: 'scheme' | 'custom'; customColor: string } = {
        family: '',
        size: 20,
        colorMode: 'scheme',
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

    limitOptions = [
        { value: Limit.Fixed, text: 'lower limit / upper limit' },
        { value: Limit.PercentNominal, text: 'lower limit % from nominal / Upper limit % from nominal' },
        {
            value: Limit.PercentNormalization,
            text: 'lower limit from Normalization value / Upper limit from Normalization value',
        },
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

    colorSchemaTypes = ColorSchema;

    constructor(private dashboardPagesService: DashboardPagesService) {}

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
        this.subscription = this.dashboardPagesService.getPages().subscribe((pages) => {
            this.pqbiPages = pages.map((page) => ({ id: page.id, name: page.name }));
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.config && this.config) {
            const c = this.config;
            this.normalizeValue = c.normalizeValue;
            this.normalizeNominalValue = +c.normalizeNominalValue;
            this.setLimits = c.setLimits;
            this.lowerLimit = +c.lowerLimit;
            this.upperLimit = +c.upperLimit;
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.titleFont = c.titleFont;
            this.valueFont = c.valueFont;
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
        const config: GaugeWidgetAdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit,
            upperLimit: this.upperLimit,
            decimalPoints: this.decimalPoints,
            linkPage: this.link.page,
            titleFont: this.titleFont,
            valueFont: this.valueFont,
            segments: this.segments.map((segment) => ({ ...segment })),
            unit: { unitType: this.unitType, selectedUnit: this.selectedUnit },
            marker1: this.marker1,
            marker2: this.marker2,
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
        };
        this.configChange.emit(config);
        this.hide();
    }
    onSelectNormalize(value: NormalizeEnum) {
        this.normalizeValue = this.normalizeValue === value ? NormalizeEnum.NO : value;
    }
    onSelectLimitType(value: Limit) {
        this.setLimits = this.setLimits === value ? null : value;
    }

    get canSave(): boolean {
        return this.isSegmentsValid;
    }

    private reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.setLimits = Limit.None;
        this.lowerLimit = 0;
        this.upperLimit = 0;
        this.decimalPoints = 2;
        this.link.page = null;
        this.titleFont = { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' };
        this.valueFont = { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' };
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
}
