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
    private readonly TOTAL_WEIGHT = 100;
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

    name: string = '';
    from: number | null = null;
    to: number | null = null;
    colorMode: 'scheme' | 'custom' = 'scheme';
    color: string | null = null;
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

        const newSegment: Segment = {
            id: Guid.newGuid().toString(),
            name: this.name.trim(),
            from: this.from!,
            to: this.to!,
            colorMode: this.colorMode,
            color: this.colorMode === 'custom' ? this.color : null,
            weight: this.weight!,
        };

        this.updateSegments([...this._segments, newSegment], {
            targetId: newSegment.id,
            desiredWeight: newSegment.weight!,
        });
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
                      colorMode: this.colorMode,
                      color: this.colorMode === 'custom' ? this.color : null,
                      weight: this.weight!,
                  }
                : segment,
        );

        this.updateSegments(updated, {
            targetId: this.editingSegmentId,
            desiredWeight: this.weight!,
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
        const updated = this._segments.filter((_, idx) => idx !== index);
        this.updateSegments(updated, { suppressHint: true });
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
            this.segmentError = 'Weights must sum to 100%. / Сума ваг повинна дорівнювати 100%.';
            this.emitState({ emitSegments: false });
            return false;
        }

        return true;
    }

    private updateSegments(
        segments: Segment[],
        options?: { targetId?: string; desiredWeight?: number; suppressHint?: boolean },
    ): void {
        const normalized = segments.map((segment) => ({
            ...segment,
            from: +segment.from,
            to: +segment.to,
            weight: segment.weight != null ? +segment.weight : segment.weight,
        }));

        const { segments: balanced, message } = this.balanceWeights(normalized, options);

        this._segments = balanced;
        this.segmentHint = options?.suppressHint ? null : message;
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
        this.segmentHint = null;
    }

    private resetSegmentForm(): void {
        this.name = '';
        this.from = null;
        this.to = null;
        this.colorMode = 'scheme';
        this.color = null;
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

        const { segments: balanced } = this.balanceWeights(prepared, { suppressHint: true });

        return balanced;
    }

    private recalculateSegmentsState(): void {
        this._segments = this.sortSegments(this._segments);
        const total = this._segments.reduce((sum, segment) => sum + (segment.weight ?? 0), 0);
        this.totalWeight = this.roundWeight(total);
        this.updateAdjacencyState();
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

    private balanceWeights(
        segments: Segment[],
        options?: { targetId?: string; desiredWeight?: number; suppressHint?: boolean },
    ): { segments: Segment[]; message: string | null } {
        if (!segments.length) {
            return { segments: [], message: null };
        }

        const sorted = this.sortSegments(segments);
        const balancingIndex = sorted.length - 1;
        const targetId = options?.targetId;
        const desiredWeight = options?.desiredWeight;
        let hintMessage: string | null = null;

        if (!targetId || desiredWeight === undefined) {
            const fixedSum = this.sumWeights(sorted.slice(0, balancingIndex));
            sorted[balancingIndex] = {
                ...sorted[balancingIndex],
                weight: this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - fixedSum)),
            };

            return { segments: sorted, message: null };
        }

        const targetIndex = sorted.findIndex((segment) => segment.id === targetId);

        if (targetIndex === -1) {
            return this.balanceWeights(sorted, { suppressHint: options?.suppressHint });
        }

        if (sorted.length === 1) {
            sorted[0] = { ...sorted[0], weight: this.roundWeight(this.TOTAL_WEIGHT) };
            return { segments: sorted, message: null };
        }

        if (targetIndex === balancingIndex) {
            const fixedSum = this.sumWeights(sorted.slice(0, balancingIndex));
            sorted[targetIndex] = {
                ...sorted[targetIndex],
                weight: this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - fixedSum)),
            };

            return { segments: sorted, message: null };
        }

        const fixedSum = sorted.reduce((sum, segment, index) => {
            if (index === targetIndex || index === balancingIndex) {
                return sum;
            }

            return sum + (segment.weight ?? 0);
        }, 0);

        const maxWeight = Math.max(0, this.TOTAL_WEIGHT - fixedSum);
        let actualWeight = Math.max(0, Math.min(desiredWeight, maxWeight));
        actualWeight = this.roundWeight(actualWeight);

        if (!options?.suppressHint && desiredWeight - actualWeight > this.EPSILON) {
            const formatted = this.formatWeight(actualWeight);
            hintMessage = `Доступно лише ${formatted}% для дотримання суми 100%. / Доступно только ${formatted}% для соблюдения суммы 100%.`;
        }

        const balancingWeight = this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - (fixedSum + actualWeight)));

        sorted[targetIndex] = {
            ...sorted[targetIndex],
            weight: actualWeight,
        };

        sorted[balancingIndex] = {
            ...sorted[balancingIndex],
            weight: balancingWeight,
        };

        return { segments: sorted, message: hintMessage };
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

    private sumWeights(segments: Segment[]): number {
        return segments.reduce((sum, segment) => sum + (segment.weight ?? 0), 0);
    }

    private roundWeight(value: number): number {
        return Math.round(value * 1000) / 1000;
    }

    private formatWeight(value: number): string {
        const rounded = Math.round(value * 100) / 100;
        return `${rounded}`;
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
                    message = `Між сегментами не може бути прогалин. Перевірте значення ${prevValue} і ${nextValue}. / Между сегментами не должно быть разрывов. Проверьте значения ${prevValue} и ${nextValue}.`;
                } else {
                    message = `Сегменти не можуть перекриватися. Перевірте значення ${prevValue} і ${nextValue}. / Сегменты не должны перекрываться. Проверьте значения ${prevValue} и ${nextValue}.`;
                }
            }
        }

        this.invalidBoundarySegmentIds = new Set(invalidIds);
        this.boundaryErrorMessage = message;
    }

    private getDefaultWeight(): number | null {
        if (!this._segments.length) {
            return this.TOTAL_WEIGHT;
        }

        if (this.isEditingSegment && this.editingSegmentId) {
            const segment = this._segments.find((item) => item.id === this.editingSegmentId);
            return segment?.weight ?? null;
        }

        const sorted = this.sortSegments(this._segments);
        if (sorted.length === 1) {
            return sorted[0].weight ?? this.TOTAL_WEIGHT;
        }

        const balancingIndex = sorted.length - 1;
        const fixedSum = this.sumWeights(sorted.slice(0, balancingIndex));
        return this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - fixedSum));
    }
}
