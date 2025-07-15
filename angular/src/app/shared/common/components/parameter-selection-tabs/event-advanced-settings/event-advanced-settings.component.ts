import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@node_modules/@angular/common';
import { DxButtonComponent, DxButtonModule, DxColorBoxModule, DxNumberBoxModule, DxPopupModule, DxRadioGroupModule, DxScrollViewModule, DxSelectBoxModule } from '@node_modules/devextreme-angular';
import { AdvancedSettingsConfig } from '../advanced-settings/advanced-settings.component';
import { NormalizeEnum } from '@shared/service-proxies/service-proxies';
import { ColorSchema, Limit } from '@app/shared/enums/advanced-settings-options';
import { CheckboxModule } from 'primeng/checkbox';
import { FormsModule } from '@angular/forms';


@Component({
    selector: 'event-advanced-settings',
    standalone: true,
    imports: [CommonModule, DxNumberBoxModule, DxColorBoxModule, DxButtonModule, DxRadioGroupModule, DxPopupModule, DxScrollViewModule, DxSelectBoxModule, CheckboxModule, FormsModule],
    templateUrl: './event-advanced-settings.component.html',
    styleUrl: './event-advanced-settings.component.css'
})
export class EventAdvancedSettingsComponent implements OnInit, OnChanges {
    @Input() initialConfig: AdvancedSettingsConfig | null = null;
    @Output() settingsChanged = new EventEmitter<AdvancedSettingsConfig>();
    modalVisible = false;

    normalizeTypes = NormalizeEnum;
    colorSchemaTypes = ColorSchema;
    limitTypes = Limit;

    normalizeValue:NormalizeEnum = NormalizeEnum.NO;
    normalizeNominalValue = 0;

    setLimits = Limit.None;
    lowerLimit = 0;
    upperLimit = 0;

    colorScheme = ColorSchema.None;
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';

    normalizationOptions = [
        // { value: NormalizeType.NO, text: 'No normalization' },
        { value: NormalizeEnum.VALUE, text: 'Normalize by…' },
    ];

    limitOptions = [
        // { value: Limit.None, text: 'No selection - default' },
        { value: Limit.Fixed, text: 'Yes – with fixed values' },
    ];

    colorSchemaOptions = [
        // { value: ColorSchema.None, text: 'No selection - default' },
        { value: ColorSchema.OutOfLimit, text: 'Out-of-limit color' },
        { value: ColorSchema.Gradient, text: 'Two-color color scheme' },
    ];

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
            this.normalizeValue = c.normalizeValue ??  NormalizeEnum.NO;
            (this.normalizeNominalValue= +c.normalizeNominalValue) ?? 0;
            this.setLimits = c.setLimits ?? Limit.None;
            (this.lowerLimit = +c.lowerLimit) ?? 0;
            (this.upperLimit = +c.upperLimit) ?? 0;
            this.colorScheme = c.colorScheme ?? ColorSchema.None;
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
            normalizeNominalValue: this.normalizeNominalValue,
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit,
            upperLimit: this.upperLimit,
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
    onSelectNormalize(value: NormalizeEnum) {
        this.normalizeValue = this.normalizeValue === value ? NormalizeEnum.NO : value;
    }

    onSelectLimitType(value: Limit) {
        this.setLimits = this.setLimits === value ? null : value;
    }

    onSelectColorScheme(value: ColorSchema) {
        this.colorScheme = this.colorScheme === value ? null : value;
    }

}
