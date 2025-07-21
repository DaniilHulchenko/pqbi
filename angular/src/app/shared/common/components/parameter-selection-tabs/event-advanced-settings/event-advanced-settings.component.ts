import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@node_modules/@angular/common';
import { DxButtonComponent, DxButtonModule, DxColorBoxModule, DxNumberBoxModule, DxPopupModule, DxRadioGroupModule, DxScrollViewModule, DxSelectBoxModule } from '@node_modules/devextreme-angular';
import { AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';

@Component({
    selector: 'event-advanced-settings',
    standalone: true,
    imports: [CommonModule, DxNumberBoxModule, DxColorBoxModule, DxButtonModule, DxRadioGroupModule, DxPopupModule, DxScrollViewModule, DxSelectBoxModule],
    templateUrl: './event-advanced-settings.component.html',
    styleUrl: './event-advanced-settings.component.css'
})
export class EventAdvancedSettingsComponent implements OnInit, OnChanges {
    @Input() initialConfig: AdvancedSettingsConfig | null = null;
    @Output() settingsChanged = new EventEmitter<AdvancedSettingsConfig>();
    modalVisible = false;

    normalizeValue = 'none';
    normalizeNominalValue = 0;

    setLimits = 'none';
    lowerLimit = 0;
    upperLimit = 0;

    colorScheme = 'none';
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';

    aggregationFuncOptions = [
        { value: 'AVG', text: 'Average' },
        { value: 'MAX', text: 'Maximum' },
        { value: 'MIN', text: 'Minimum' }
    ];
    customAggregationFunc = '';

    ngOnInit() {}

    ngOnChanges(changes: SimpleChanges) {
        if (changes.initialConfig && this.initialConfig) {
            const c = this.initialConfig;
            this.normalizeValue = c.normalizeValue ?? 'none';
            (this.normalizeNominalValue= +c.normalizeNominalValue) ?? 0;
            this.setLimits = c.setLimits ?? 'none';
            (this.lowerLimit = +c.lowerLimit) ?? 0;
            (this.upperLimit = +c.upperLimit) ?? 0;
            this.colorScheme = c.colorScheme ?? 'none';
            this.outOfLimitColor = c.outOfLimitColor ?? '';
            this.gradientFromColor = c.gradientFromColor ?? '';
            this.gradientToColor = c.gradientToColor ?? '';
            this.okColor = c.okColor ?? '';
            this.noDataColor = c.noDataColor ?? '';
            this.customAggregationFunc = c.customAggregationFunc ?? '';
        }
    }

    show() {
        this.modalVisible = true;
    }
    hide() {
        this.modalVisible = false;
    }

    save() {
        const cfg: AdvancedSettingsConfig = {
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue.toString(),
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit.toString(),
            upperLimit: this.upperLimit.toString(),
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
            gradientFromColor: this.gradientFromColor,
            gradientToColor: this.gradientToColor,
            okColor: this.okColor,
            noDataColor: this.noDataColor,
            customAggregationFunc: this.customAggregationFunc
        };
        this.settingsChanged.emit(cfg);
        this.hide();
    }
}
