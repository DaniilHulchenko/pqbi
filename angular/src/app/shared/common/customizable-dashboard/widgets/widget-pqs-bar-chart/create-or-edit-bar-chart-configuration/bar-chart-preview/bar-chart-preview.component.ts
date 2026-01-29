import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DxChartModule } from 'devextreme-angular';
import { BarChartType } from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'bar-chart-preview',
    //standalone: true
,
    imports: [CommonModule, DxChartModule],
    templateUrl: './bar-chart-preview.component.html',
    styleUrl: './bar-chart-preview.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarChartPreviewComponent {
    @Input() type: BarChartType = BarChartType.Plain;
    @Input() data: any[] | null = null;
    barChartType = BarChartType;
    readonly fixedPoint2 = { type: 'fixedPoint', precision: 2 };



    customizeTooltip = ({ valueText, seriesName }) => ({
        text: seriesName ? `${seriesName}: ${valueText}` : `${valueText}`,
    });

    get plainData() {
        return (
            this.data ?? [
                { category: 'Component A', value: this.random() },
                { category: 'Component B', value: this.random() },
            ]
        );
    }
        get multiSeriesData() {
        return (
            this.data ?? [
                { category: 'Component A', seriesName: 'Event 1', value: this.random() },
                { category: 'Component A', seriesName: 'Event 2', value: this.random() },
                { category: 'Component B', seriesName: 'Event 1', value: this.random() },
                { category: 'Component B', seriesName: 'Event 2', value: this.random() },
            ]
        );
    }

    private random(): number {
        return Math.floor(Math.random() * 100);
    }
}
