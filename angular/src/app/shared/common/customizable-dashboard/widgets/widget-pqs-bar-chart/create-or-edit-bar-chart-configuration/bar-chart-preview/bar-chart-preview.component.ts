import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DxChartModule } from 'devextreme-angular';
import { BarChartType } from '@shared/service-proxies/service-proxies';
@Component({
    selector: 'bar-chart-preview',
    standalone: true,
    imports: [CommonModule, DxChartModule],
    templateUrl: './bar-chart-preview.component.html',
    styleUrl: './bar-chart-preview.component.css',
})
export class BarChartPreviewComponent {
    @Input() type: BarChartType = BarChartType.Plain;
    barChartType = BarChartType;

    plainArgumentField: 'componentName' | 'eventName' = 'componentName';

    plainData = [
        { componentName: 'Component A', eventCount: 10 },
        { componentName: 'Component B', eventCount: 7 },
    ];

    multiSeriesData = [
        { componentName: 'Component A', eventName: 'Event 1', eventCount: 10 },
        { componentName: 'Component A', eventName: 'Event 2', eventCount: 5 },
        { componentName: 'Component B', eventName: 'Event 1', eventCount: 7 },
        { componentName: 'Component B', eventName: 'Event 2', eventCount: 3 },
    ];
}
