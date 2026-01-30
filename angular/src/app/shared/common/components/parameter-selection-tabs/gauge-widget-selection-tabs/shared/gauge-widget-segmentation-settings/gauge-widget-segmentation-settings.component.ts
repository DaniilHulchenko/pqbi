import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DxTextBoxModule, DxNumberBoxModule, DxColorBoxModule, DxButtonModule, DxDataGridModule } from 'devextreme-angular';
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
        DxColorBoxModule,
        DxButtonModule,
        DxDataGridModule,
    ],
    templateUrl: './gauge-widget-segmentation-settings.component.html',
    styleUrls: ['./gauge-widget-segmentation-settings.component.css'],
})
export class GaugeWidgetSegmentationSettingsComponent {

    private readonly DEFAULT_COLOR = '#000000';
    readonly TOTAL_WEIGHT = 100;
    private readonly EPSILON = 0.001;
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
    selectedSegmentId: string | null = null;


    name: string = '';
    from: number | null = null;
    to: number | null = null;
    colorMode: 'custom' = 'custom';
    color: string | null = this.DEFAULT_COLOR;
    weight: number | null = null;

    segmentError: string | null = null;
    totalWeight = 0;
    segmentHint: string | null = null;
    boundaryErrorMessage: string | null = null;
    private invalidBoundarySegmentIds: Set<string> = new Set<string>();

    addSegment(): void {
        if (!this.validateSegmentForm()) {
            return;
        }
        const desiredWeight = this.weight!;


        const newSegment: Segment = {
            id: Guid.newGuid().toString(),
            name: this.name.trim(),
            from: this.from!,
            to: this.to!,
            colorMode: 'custom',
            color: this.color ?? this.DEFAULT_COLOR,
            weight: this.roundWeight(desiredWeight),
        };

        const plannedSegments = [...this._segments, newSegment];
 
        this.updateSegments(plannedSegments, {
            targetId: newSegment.id,
 
        });
        
        this.resetSegmentForm();
    }

    editSegment(data: Segment): void {
        this.isEditingSegment = true;
        this.editingSegmentId = data.id;
        this.selectedSegmentId = data.id;
        this.name = data.name;
        this.from = data.from;
        this.to = data.to;
        this.colorMode = 'custom';
        this.color = data.color ?? this.DEFAULT_COLOR;
        this.weight = data.weight ?? null;
        this.segmentError = null;
        this.segmentHint = null;
        this.segmentHint = null;
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
                      colorMode: 'custom' as const,
                      color: this.color ?? this.DEFAULT_COLOR,
                      weight: this.roundWeight(this.weight!),

                   }
                : segment,
        );

 
        this.updateSegments(updated, {
            targetId: this.editingSegmentId,
 
        });
        this.cancelEditedSegment(false);
    }

    cancelEditedSegment(clearHint: boolean = true): void {
        this.isEditingSegment = false;
        this.editingSegmentId = null;
        this.resetSegmentForm();
        if (clearHint) {
            this.segmentHint = null;
        }
        this.segmentError = null;
    }

    deleteSegment(index: number): void {
        const sorted = this.sortSegments(this._segments);

        if (index < 0 || index >= sorted.length) {
            return;
        }

         const remaining = sorted.filter((_, idx) => idx !== index);

        if (!remaining.length) {
            this.updateSegments([], { suppressHint: true });
            return;
        }

        const nextSelected = remaining[Math.min(index, remaining.length - 1)]?.id;

        this.updateSegments(remaining, { suppressHint: true, targetId: nextSelected ?? null });
        if (!this.isEditingSegment) {
            this.resetSegmentForm();
        }
    }

    validateBeforeSave(): boolean {
        if (!this._segments.length) {
            this.segmentError = 'At least one segment is required.';
            this.emitState({ emitSegments: false });
            return false;
        }

         if (this.hasAdjacencyIssues) {
            this.segmentError = this.boundaryErrorMessage;
            this.emitState({ emitSegments: false });
            return false;
        }


        if (!this.isTotalWeightValid) {
            this.segmentError = 'Weights must sum to 100%.';
            this.emitState({ emitSegments: false });
            return false;
        }

        return true;
    }

    private updateSegments(
        segments: Segment[],
        options?: {
            targetId?: string | null;
            suppressHint?: boolean;
        },
    ): void {
        const normalized = segments.map((segment) => ({
            ...segment,
            from: +segment.from,
            to: +segment.to,
            weight: segment.weight != null ? this.roundWeight(+segment.weight) : segment.weight,
        }));
        this._segments = normalized;
        this.segmentHint = null;
        this.recalculateSegmentsState();
        this.segmentError = null;
        this.emitState({ emitSegments: true });
        this.ensureSelectedSegment(options?.targetId);

        if (!this._segments.length && !this.isEditingSegment) {
            this.resetSegmentForm();
        }
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
        this.segmentHint = null;

    }

    private resetSegmentForm(): void {
        this.name = '';
        this.from = null;
        this.to = null;
        this.colorMode = 'custom';
        this.color = this.DEFAULT_COLOR;
        this.weight = this.getDefaultWeight();
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
            colorMode: 'custom' as const,
            color: segment.color ?? this.DEFAULT_COLOR,
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
        this._segments = this.sortSegments(this._segments);
        const total = this.calculateTotalWeight(this._segments);
        this.totalWeight = this.roundWeight(total);
        this.updateAdjacencyState();
        this.ensureSelectedSegment(this.selectedSegmentId);

    }

    private validateSegmentForm(ignoreId?: string): boolean {
        this.segmentError = null;

        if (!this.name || !this.name.trim() || this.from === null || this.to === null || this.weight === null) {
            this.segmentError = 'Please fill out all required segment fields.';
            return false;
        }

        if (!this.color) {
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
        return Math.abs(this.totalWeight - this.TOTAL_WEIGHT) < 0.01;
    }

    get isTotalWeightExceeded(): boolean {
        return this.totalWeight - this.TOTAL_WEIGHT > this.EPSILON;
    }

    get totalWeightOverflow(): number {
        if (!this.isTotalWeightExceeded) {
            return 0;
        }

        return this.roundWeight(this.totalWeight - this.TOTAL_WEIGHT);
    }

    get canSave(): boolean {
         return this._segments.length > 0 && this.isTotalWeightValid && !this.hasAdjacencyIssues;
    }

    get hasAdjacencyIssues(): boolean {
        return this.invalidBoundarySegmentIds.size > 0;
    }

    onRowPrepared(e: any): void {
        if (e.rowType !== 'data' || !e.data?.id) {
            return;
        }

        if (this.invalidBoundarySegmentIds.has(e.data.id)) {
            e.rowElement.classList.add('invalid-boundary');
        }
    }

    
    private sortSegments(segments: Segment[]): Segment[] {
        return [...segments].sort((a, b) => {
            if (a.from !== b.from) {
                return a.from - b.from;
            }

            if (a.to !== b.to) {
                return a.to - b.to;
            }

            return a.name.localeCompare(b.name);
        });
    }

    

    private roundWeight(value: number): number {
        return Math.round(value * 1000) / 1000;
    }
    

    private calculateTotalWeight(segments: Segment[]): number {
        return segments.reduce((sum, segment) => sum + (segment.weight ?? 0), 0);
    }

    private getDefaultWeight(): number {
        const remaining = Math.max(0, this.TOTAL_WEIGHT - this.calculateTotalWeight(this._segments));
        return this.roundWeight(remaining);
    }


    private formatBoundary(value: number): string {
        return `${Math.round(value * 1000) / 1000}`;
    }

    private updateAdjacencyState(): void {
        const invalidIds: string[] = [];
        let message: string | null = null;

        for (let index = 1; index < this._segments.length; index++) {
            const previous = this._segments[index - 1];
            const current = this._segments[index];
            const diff = current.from - previous.to;

            if (Math.abs(diff) <= this.EPSILON) {
                continue;
            }

            invalidIds.push(previous.id, current.id);

            if (!message) {
                const prevValue = this.formatBoundary(previous.to);
                const nextValue = this.formatBoundary(current.from);

                if (diff > this.EPSILON) {
                    message = `There are no clearings between the segments. Reverse the values ${prevValue} and ${nextValue}.`;
                } else {
                    message = `Segments cannot overlap. Check the values ${prevValue} and ${nextValue}.`;
                }
            }
        }

        this.invalidBoundarySegmentIds = new Set(invalidIds);
        this.boundaryErrorMessage = message;
    }


 
    private ensureSelectedSegment(preferredId: string | null | undefined): void {
        const candidate = preferredId ?? this.selectedSegmentId;

        if (candidate && this._segments.some((segment) => segment.id === candidate)) {
            this.selectedSegmentId = candidate;
            return;
        }

        this.selectedSegmentId = this._segments.length
            ? this._segments[this._segments.length - 1].id
            : null;
    }

    onFocusedRowChanged(event: any): void {
        const data = event?.row?.data;

        if (data?.id) {
            this.selectedSegmentId = data.id;
        }
    }
}