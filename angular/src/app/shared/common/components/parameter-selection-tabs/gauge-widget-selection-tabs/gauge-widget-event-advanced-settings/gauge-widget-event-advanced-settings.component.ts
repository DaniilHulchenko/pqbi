import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
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
    DxDataGridModule,
} from 'devextreme-angular';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import { NormalizeEnum, EventClass } from '@shared/service-proxies/service-proxies';
import { GaugeWidgetAdvancedSettingsConfig, Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { Guid } from 'guid-ts';

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
        DxDataGridModule,
    ],
    templateUrl: './gauge-widget-event-advanced-settings.component.html',
    styleUrl: './gauge-widget-event-advanced-settings.component.css',
})
export class GaugeWidgetEventAdvancedSettingsComponent implements OnInit, OnChanges {
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

    segments: Segment[] = [];
    isEditingSegment = false;
    editingSegmentId: string | null = null;

    name: string = '';
    from: number | null = null;
    to: number | null = null;
    colorMode: 'scheme' | 'custom';
    color: string | null;

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

    pqbiPages = [];

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

    constructor() {}

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
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
            this.segments = c.segments || [];
            this.unitType = c.unit?.unitType || 'auto';
            this.selectedUnit = c.unit?.selectedUnit || '';
            this.marker1 = c.marker1 || null;
            this.marker2 = c.marker2 || null;
            this.colorScheme = c.colorScheme;
            this.outOfLimitColor = c.outOfLimitColor;
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

    show(settings: GaugeWidgetAdvancedSettingsConfig | null = null) {
        if (settings) {
            this.config = settings;
            this.ngOnChanges({ config: { currentValue: settings, previousValue: null, firstChange: true, isFirstChange: () => true } });
        }
        this.modalVisible = true;
    }

    hide() {
        this.reset();
        this.modalVisible = false;
    }

    addSegment() {
        if (!this.name || this.from === null || this.to === null) return;

        this.segments.push({
            id: Guid.newGuid().toString(),
            name: this.name,
            from: this.from,
            to: this.to,
            color: this.color,
            colorMode: this.colorMode,
        });

        this.name = '';
        this.from = null;
        this.to = null;
        this.color = null;
        this.colorMode = 'scheme';
    }

    editSegment(data: Segment) {
        this.isEditingSegment = true;
        this.editingSegmentId = data.id;
        this.name = data.name;
        this.from = data.from;
        this.to = data.to;
        this.color = data.color;
        this.colorMode = data.colorMode;
    }

    saveEditedSegment() {
        if (!this.name || this.from === null || this.to === null || !this.editingSegmentId) return;

        const segment = this.segments.find((s) => s.id === this.editingSegmentId);
        if (segment) {
            segment.name = this.name;
            segment.from = this.from;
            segment.to = this.to;
            segment.color = this.color;
            segment.colorMode = this.colorMode;
        }

        this.cancelEditedSegment();
    }

    cancelEditedSegment() {
        this.isEditingSegment = false;
        this.editingSegmentId = null;
        this.name = '';
        this.from = null;
        this.to = null;
        this.color = null;
        this.colorMode = 'scheme';
    }

    deleteSegment(index: number) {
        this.segments.splice(index, 1);
    }

    save() {
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
            segments: this.segments,
            unit: {unitType: this.unitType, selectedUnit: this.selectedUnit},
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
        this.unitType = 'auto';
        this.selectedUnit = '';
        this.marker1 = null;
        this.marker2 = null;
        this.colorScheme = ColorSchema.None;
        this.outOfLimitColor = '';
    }
}
