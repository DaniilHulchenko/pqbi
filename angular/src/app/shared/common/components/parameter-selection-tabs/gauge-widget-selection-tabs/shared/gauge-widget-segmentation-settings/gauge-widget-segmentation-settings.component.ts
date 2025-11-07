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
    selectedSegmentId: string | null = null;

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
            weight: this._segments.length ? this.weight! : this.TOTAL_WEIGHT,
        };

        const plannedSegments = [...this._segments, newSegment];
        const neighborId = this.resolveNeighborId(plannedSegments, newSegment.id);

        this.updateSegments(plannedSegments, {
            targetId: newSegment.id,
            desiredWeight: newSegment.weight!,
            neighborId,
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

        const neighborId = this.resolveNeighborId(updated, this.editingSegmentId);

        this.updateSegments(updated, {
            targetId: this.editingSegmentId,
            desiredWeight: this.weight!,
            neighborId,
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

        const removed = sorted[index];
        const remaining = sorted.filter((_, idx) => idx !== index);

        if (!remaining.length) {
            this.updateSegments([], { suppressHint: true });
            return;
        }

        const neighborIndex = index > 0 ? index - 1 : 0;
        const neighbor = remaining[neighborIndex];
        const neighborWeight = (neighbor.weight ?? 0) + (removed.weight ?? 0);

        remaining[neighborIndex] = {
            ...neighbor,
            weight: this.roundWeight(neighborWeight),
        };

        this.updateSegments(remaining, { suppressHint: true });
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
        options?: {
            targetId?: string;
            desiredWeight?: number;
            neighborId?: string | null;
            suppressHint?: boolean;
        },
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
        this.ensureSelectedSegment(options?.targetId);
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
        this.ensureSelectedSegment(this.selectedSegmentId);
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
        options?: {
            targetId?: string;
            desiredWeight?: number;
            neighborId?: string | null;
            suppressHint?: boolean;
        },
    ): { segments: Segment[]; message: string | null } {
        if (!segments.length) {
            return { segments: [], message: null };
        }

        const sorted = this.sortSegments(segments);

        if (sorted.length === 1) {
            sorted[0] = { ...sorted[0], weight: this.roundWeight(this.TOTAL_WEIGHT) };
            return { segments: sorted, message: null };
        }

        const targetId = options?.targetId;
        const desiredWeight = options?.desiredWeight;

        if (targetId && desiredWeight !== undefined) {
            const targetIndex = sorted.findIndex((segment) => segment.id === targetId);

            if (targetIndex !== -1) {
                const neighborIndex = this.findNeighborIndex(sorted, targetIndex, options?.neighborId ?? null);

                if (neighborIndex !== -1) {
                    return this.balanceWithNeighbor(sorted, targetIndex, neighborIndex, desiredWeight, options?.suppressHint);
                }
            }
        }

        return this.normalizeByTrailingSegment(sorted);
    }

    private balanceWithNeighbor(
        segments: Segment[],
        targetIndex: number,
        neighborIndex: number,
        desiredWeight: number,
        suppressHint?: boolean,
    ): { segments: Segment[]; message: string | null } {
        const otherSum = this.sumWeights(
            segments.filter((_, index) => index !== targetIndex && index !== neighborIndex),
        );
        const pairTotal = this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - otherSum));

        let actualWeight = Math.max(0, Math.min(desiredWeight, pairTotal));
        actualWeight = this.roundWeight(actualWeight);

        let hintMessage: string | null = null;

        if (!suppressHint && desiredWeight - actualWeight > this.EPSILON) {
            const formatted = this.formatWeight(actualWeight);
            hintMessage = `Недостатньо відсотків у попереднього сегмента. Доступно лише ${formatted}%. / Недостаточно процентов у предыдущего сегмента. Доступно только ${formatted}%.`;
        }

        const neighborWeight = this.roundWeight(Math.max(0, pairTotal - actualWeight));

        segments[targetIndex] = {
            ...segments[targetIndex],
            weight: actualWeight,
        };

        segments[neighborIndex] = {
            ...segments[neighborIndex],
            weight: neighborWeight,
        };

        return { segments, message: hintMessage };
    }

    private normalizeByTrailingSegment(segments: Segment[]): { segments: Segment[]; message: string | null } {
        const sorted = this.sortSegments(segments);
        const balancingIndex = sorted.length - 1;
        const fixedSum = this.sumWeights(sorted.slice(0, balancingIndex));
        sorted[balancingIndex] = {
            ...sorted[balancingIndex],
            weight: this.roundWeight(Math.max(0, this.TOTAL_WEIGHT - fixedSum)),
        };

        return { segments: sorted, message: null };
    }

    private findNeighborIndex(segments: Segment[], targetIndex: number, neighborId: string | null): number {
        if (neighborId) {
            const explicitIndex = segments.findIndex((segment) => segment.id === neighborId);
            if (explicitIndex !== -1 && explicitIndex !== targetIndex) {
                return explicitIndex;
            }
        }

        if (targetIndex > 0) {
            return targetIndex - 1;
        }

        if (targetIndex + 1 < segments.length) {
            return targetIndex + 1;
        }

        return -1;
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
        const targetId = this.selectedSegmentId;

        if (targetId) {
            const candidate = sorted.find((segment) => segment.id === targetId);
            if (candidate?.weight != null) {
                return this.roundWeight(candidate.weight);
            }
        }

        const last = sorted[sorted.length - 1];
        return this.roundWeight(last.weight ?? this.TOTAL_WEIGHT);
    }

    private resolveNeighborId(segments: Segment[], targetId: string): string | null {
        const sorted = this.sortSegments(segments);
        const targetIndex = sorted.findIndex((segment) => segment.id === targetId);

        if (targetIndex === -1) {
            return null;
        }

        if (targetIndex > 0) {
            return sorted[targetIndex - 1].id;
        }

        if (targetIndex + 1 < sorted.length) {
            return sorted[targetIndex + 1].id;
        }

        return null;
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
