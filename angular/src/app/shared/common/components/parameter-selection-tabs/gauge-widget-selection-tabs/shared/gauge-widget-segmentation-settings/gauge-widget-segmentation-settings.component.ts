import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    DxTextBoxModule,
    DxNumberBoxModule,
    DxRadioGroupModule,
    DxColorBoxModule,
    DxButtonModule,
    DxDataGridModule,
} from 'devextreme-angular';
import { Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { Guid } from 'guid-ts';

@Component({
    selector: 'gaugeWidgetSegmentationSettings',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        DxTextBoxModule,
        DxNumberBoxModule,
        DxRadioGroupModule,
        DxColorBoxModule,
        DxButtonModule,
        DxDataGridModule,
    ],
    templateUrl: './gauge-widget-segmentation-settings.component.html',
    styleUrls: ['./gauge-widget-segmentation-settings.component.css'],
})
export class GaugeWidgetSegmentationSettingsComponent {
    private _segments: Segment[] = [];

    @Input()
    set segments(value: Segment[] | null) {
        const next = this.prepareSegments(value ?? []);
        this._segments = next;
        this.resetEditingState();
        this.recalculateSegmentsState();
        this.emitState({ emitSegments: false });
    }

    get segments(): Segment[] {
        return this._segments;
    }

    @Output() segmentsChange = new EventEmitter<Segment[]>();
    @Output() validityChange = new EventEmitter<boolean>();
    @Output() totalWeightChange = new EventEmitter<number>();

    isEditingSegment = false;
    editingSegmentId: string | null = null;

    name: string = '';
    from: number | null = null;
    to: number | null = null;
    colorMode: 'scheme' | 'custom' = 'scheme';
    color: string | null = null;
    weight: number | null = null;

    segmentError: string | null = null;
    totalWeight = 0;

    addSegment(): void {
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

        this.updateSegments([...this._segments, newSegment]);
        this.resetSegmentForm();
    }

    editSegment(data: Segment): void {
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

    saveEditedSegment(): void {
        if (!this.editingSegmentId) {
            return;
        }

        if (!this.validateSegmentForm(this.editingSegmentId)) {
            return;
        }

        const updated = this._segments.map((segment) =>
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

        this.updateSegments(updated);
        this.cancelEditedSegment();
    }

    cancelEditedSegment(): void {
        this.isEditingSegment = false;
        this.editingSegmentId = null;
        this.resetSegmentForm();
        this.segmentError = null;
    }

    deleteSegment(index: number): void {
        const updated = this._segments.filter((_, idx) => idx !== index);
        this.updateSegments(updated);
    }

    validateBeforeSave(): boolean {
        if (!this._segments.length) {
            this.segmentError = 'At least one segment is required.';
            this.emitState({ emitSegments: false });
            return false;
        }

        if (!this.isTotalWeightValid) {
            this.segmentError = 'Weights must sum to 100%. / Сума ваг повинна дорівнювати 100%.';
            this.emitState({ emitSegments: false });
            return false;
        }

        return true;
    }

    private updateSegments(segments: Segment[]): void {
        this._segments = segments.map((segment) => ({
            ...segment,
            from: +segment.from,
            to: +segment.to,
            weight: segment.weight != null ? +segment.weight : segment.weight,
        }));
        this.recalculateSegmentsState();
        this.segmentError = null;
        this.emitState({ emitSegments: true });
    }

    private emitState({ emitSegments }: { emitSegments: boolean }): void {
        if (emitSegments) {
            this.segmentsChange.emit(this._segments.map((segment) => ({ ...segment })));
        }

        this.totalWeightChange.emit(this.totalWeight);
        this.validityChange.emit(this.canSave);
    }

    private resetEditingState(): void {
        this.isEditingSegment = false;
        this.editingSegmentId = null;
        this.resetSegmentForm();
        this.segmentError = null;
    }

    private resetSegmentForm(): void {
        this.name = '';
        this.from = null;
        this.to = null;
        this.colorMode = 'scheme';
        this.color = null;
        this.weight = null;
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
        this._segments = [...this._segments].sort((a, b) => a.from - b.from);
        const total = this._segments.reduce((sum, segment) => sum + (segment.weight ?? 0), 0);
        this.totalWeight = Math.round(total * 1000) / 1000;
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
        return this._segments.some((segment) => {
            if (segment.id === ignoreId) {
                return false;
            }

            return from < segment.to && to > segment.from;
        });
    }

    get isTotalWeightValid(): boolean {
        return Math.abs(this.totalWeight - 100) < 0.01;
    }

    get canSave(): boolean {
        return this._segments.length > 0 && this.isTotalWeightValid;
    }
}
