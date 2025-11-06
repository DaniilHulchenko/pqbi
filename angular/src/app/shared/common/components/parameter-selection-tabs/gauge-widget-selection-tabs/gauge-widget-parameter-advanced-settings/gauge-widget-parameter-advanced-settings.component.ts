import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    DxPopupModule,
    DxScrollViewModule,
    DxRadioGroupModule,
    DxTextBoxModule,
    DxColorBoxModule,
    DxButtonModule,
    DxFileUploaderModule,
    DxNumberBoxModule,
    DxSelectBoxModule,
    DxDataGridModule,
} from 'devextreme-angular';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import {
    EventClass,
    EventClassDescription,
    NormalizeEnum,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ColorSchema, ExcludeFlagged, Limit } from '@app/shared/enums/advanced-settings-options';
import { uniqBy } from 'lodash-es';
import { GaugeWidgetAdvancedSettingsConfig, Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { Guid } from 'guid-ts';
import { EventService } from '@app/shared/services/event-service.service';

@Component({
    selector: 'gaugeWidgetParameterAdvancedSettings',
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
        DxDataGridModule
    ],
    templateUrl: './gauge-widget-parameter-advanced-settings.component.html',
    styleUrl: './gauge-widget-parameter-advanced-settings.component.css',
})
export class GaugeWidgetParameterAdvancedSettingsComponent implements OnInit, OnChanges {
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
    colorMode: 'scheme' | 'custom' = 'scheme';
    color: string | null = null;
    weight: number | null = null;

    totalWeight = 0;
    segmentError: string | null = null;

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

    constructor(private _eventService: EventService) {}

    ngOnInit() {
        this.normalizationOptions = this.getNormalizationOptions();
        this._eventService.pqsEvents().subscribe((evts) => {
            this.flaggingEvents = evts;
            this.flaggingEvents = uniqBy(this.flaggingEvents, (x) => x.eventClass);
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.config && this.config) {
            const c = this.config;
            this.normalizeValue = c.normalizeValue;
            this.normalizeNominalValue = +c.normalizeNominalValue;
            this.excludeFlagged = c.excludeFlagged ?? ExcludeFlagged.None;
            this.selectedFlagEvents = c.defaultFlagEvent ?? [];
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.titleFont = c.titleFont;
            this.valueFont = c.valueFont;
            this.segments = this.prepareSegments(c.segments || []);
            this.recalculateSegmentsState();
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
        this.cancelEditedSegment();
        this.modalVisible = false;
        this.reset();
    }

    addSegment() {
        if (!this.validateSegmentForm()) {
            return;
        }

        const newSegment: Segment = {
            id: Guid.newGuid().toString(),
            name: this.name.trim(),
            from: this.from!,
            to: this.to!,
            colorMode: this.colorMode,
            color: this.colorMode === 'custom' ? this.color : null,
            weight: this.weight!,
        };

        this.segments = [...this.segments, newSegment];
        this.onSegmentsChanged();
        this.resetSegmentForm();
    }

    editSegment(data: Segment) {
        this.isEditingSegment = true;
        this.editingSegmentId = data.id;
        this.name = data.name;
        this.from = data.from;
        this.to = data.to;
        this.color = data.color;
        this.colorMode = data.colorMode;
        this.weight = data.weight ?? null;
        this.segmentError = null;
    }

    saveEditedSegment() {
        if (!this.editingSegmentId) {
            return;
        }

        if (!this.validateSegmentForm(this.editingSegmentId)) {
            return;
        }

        this.segments = this.segments.map((segment) =>
            segment.id === this.editingSegmentId
                ? {
                      ...segment,
                      name: this.name.trim(),
                      from: this.from!,
                      to: this.to!,
                      colorMode: this.colorMode,
                      color: this.colorMode === 'custom' ? this.color : null,
                      weight: this.weight!,
                  }
                : segment,
        );

        this.onSegmentsChanged();
        this.cancelEditedSegment();
    }

    cancelEditedSegment() {
        this.isEditingSegment = false;
        this.editingSegmentId = null;
        this.resetSegmentForm();
        this.segmentError = null;
    }

    deleteSegment(index: number) {
        this.segments = this.segments.filter((_, idx) => idx !== index);
        this.onSegmentsChanged();
    }

    save() {
        if (!this.canSave) {
            this.segmentError = this.segmentError || (!this.segments.length ? 'At least one segment is required.' : null);
            return;
        }

        const lowerLimit = this.getSegmentsMin();
        const upperLimit = this.getSegmentsMax();

        if (lowerLimit == null || upperLimit == null) {
            this.segmentError = 'At least one segment is required.';
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
            segments: this.segments.map((segment) => ({ ...segment })),
            unit: { unitType: this.unitType, selectedUnit: this.selectedUnit },
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
        return this.segments.length > 0 && this.isTotalWeightValid;
    }

    get isTotalWeightValid(): boolean {
        return Math.abs(this.totalWeight - 100) < 0.01;
    }

    get minScale(): number | null {
        return this.getSegmentsMin();
    }

    get maxScale(): number | null {
        return this.getSegmentsMax();
    }

    private onSegmentsChanged(): void {
        this.segments = this.segments.map((segment) => ({
            ...segment,
            from: +segment.from,
            to: +segment.to,
            weight: segment.weight != null ? +segment.weight : segment.weight,
        }));
        this.recalculateSegmentsState();
        this.segmentError = null;
    }

    private prepareSegments(segments: Segment[]): Segment[] {
        if (!segments?.length) {
            return [];
        }

        const prepared = segments.map((segment) => ({
            ...segment,
            from: +segment.from,
            to: +segment.to,
            weight: segment.weight != null ? +segment.weight : segment.weight,
        }));

        const hasMissingWeights = prepared.some((segment) => segment.weight == null);

        if (hasMissingWeights) {
            const totalSpan = prepared.reduce((sum, segment) => sum + Math.max(segment.to - segment.from, 0), 0);

            if (totalSpan > 0) {
                prepared.forEach((segment) => {
                    if (segment.weight == null) {
                        const span = Math.max(segment.to - segment.from, 0);
                        segment.weight = span === 0 ? 0 : (span / totalSpan) * 100;
                    }
                });
            } else {
                const equalWeight = 100 / prepared.length;
                prepared.forEach((segment) => {
                    if (segment.weight == null) {
                        segment.weight = equalWeight;
                    }
                });
            }
        }

        return prepared;
    }

    private recalculateSegmentsState(): void {
        this.segments = [...this.segments].sort((a, b) => a.from - b.from);
        const total = this.segments.reduce((sum, segment) => sum + (segment.weight ?? 0), 0);
        this.totalWeight = Math.round(total * 1000) / 1000;
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

    private validateSegmentForm(ignoreId?: string): boolean {
        this.segmentError = null;

        if (!this.name || !this.name.trim() || this.from === null || this.to === null || this.weight === null) {
            this.segmentError = 'Please fill out all required segment fields.';
            return false;
        }

        if (this.colorMode === 'custom' && !this.color) {
            this.segmentError = 'Color is required for custom segments.';
            return false;
        }

        if (this.from >= this.to) {
            this.segmentError = 'Start value must be less than end value.';
            return false;
        }

        if (this.weight <= 0) {
            this.segmentError = 'Weight must be greater than 0.';
            return false;
        }

        if (this.hasOverlap(this.from, this.to, ignoreId)) {
            this.segmentError = 'Segments cannot overlap.';
            return false;
        }

        return true;
    }

    private hasOverlap(from: number, to: number, ignoreId?: string): boolean {
        return this.segments.some((segment) => {
            if (segment.id === ignoreId) {
                return false;
            }

            return from < segment.to && to > segment.from;
        });
    }

    private resetSegmentForm(): void {
        this.name = '';
        this.from = null;
        this.to = null;
        this.colorMode = 'scheme';
        this.color = null;
        this.weight = null;
    }

    reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        this.decimalPoints = 2;
        this.link.page = null;
        this.titleFont = { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' };
        this.valueFont = { family: '', size: 20, colorMode: 'scheme', customColor: '#000000' };
        this.segments = [];
        this.totalWeight = 0;
        this.unitType = 'auto';
        this.selectedUnit = '';
        this.marker1 = null;
        this.marker2 = null;
        this.colorScheme = ColorSchema.None;
        this.outOfLimitColor = '';
        this.resetSegmentForm();
        this.segmentError = null;
    }
}
