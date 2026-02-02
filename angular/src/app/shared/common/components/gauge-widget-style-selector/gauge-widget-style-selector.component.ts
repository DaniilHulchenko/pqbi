import { Component, forwardRef, Injector, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DxSelectBoxModule, DxNumberBoxModule, DxCheckBoxModule, DxTextBoxModule } from 'devextreme-angular';
import { GaugeStyle, GaugeStyleArcAngleEnum, GaugeStyleEnum, GaugeStyleOrientationEnum } from '@app/shared/interfaces/gauge-style';
import { AppComponentBase } from '@shared/common/app-component-base';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { UtilsModule } from '@shared/utils/utils.module';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'gaugeWidgetStyleSelector',
    standalone: true,
    imports: [LocalizePipe, CommonModule, DxSelectBoxModule, DxNumberBoxModule, DxCheckBoxModule, DxTextBoxModule, UtilsModule],
    templateUrl: './gauge-widget-style-selector.component.html',
    styleUrl: './gauge-widget-style-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => GaugeWidgetStyleSelectorComponent),
            multi: true,
        },
    ],
})
export class GaugeWidgetStyleSelectorComponent extends AppComponentBase implements ControlValueAccessor, OnInit {
    styleModel: GaugeStyle;

    styleOptions = [];

    orientationOptions = [];

    arcAngleOptions = [];

    onChange: any = () => {};
    onTouched: any = () => {};

    private isSilentChange = false;

    constructor(injector: Injector) {
        super(injector);
        this.setDefaultValue();
    }

    get isLinear(): boolean {
        return this.styleModel.style === GaugeStyleEnum.Linear;
    }

    get isCircular(): boolean {
        return this.styleModel.style === GaugeStyleEnum.Circle;
    }

    get isCustomArc(): boolean {
        return this.styleModel.arcAngle === GaugeStyleArcAngleEnum.Custom;
    }

    get isStyleValid(): boolean {
        return !!this.styleModel?.style;
    }

    get isGaugeOrientationValid(): boolean {
        return this.isLinear ? !!this.styleModel?.orientation : true;
    }

    get isInvertScaleValid(): boolean {
        return this.isLinear ? this.styleModel.isInvertScale !== null && this.styleModel.isInvertScale !== undefined : true;
    }

    get isTrackWidthValid(): boolean {
        return this.isLinear ? !!this.styleModel.trackWidth : true;
    }

    get isArcWidthValid(): boolean {
        return this.isCircular ? !!this.styleModel.arcWidth : true;
    }

    get isArcAngleValid(): boolean {
        return this.isCircular ? !!this.styleModel.arcAngle : true;
    }

    get isStartAngleValid(): boolean {
        return this.isCircular && this.isCustomArc
            ? this.styleModel.startAngle != null && this.styleModel.startAngle != undefined
            : true;
    }

    get isEndAngleValid(): boolean {
        return this.isCircular && this.isCustomArc
            ? this.styleModel.endAngle != null && this.styleModel.endAngle != undefined
            : true;
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.setStyleOptions();
        this.setOrientationOptions();
        this.setArcAngleOptions();
    }

    writeValue(newValue: GaugeStyle): void {
        if (newValue){
            this.isSilentChange = true;
            this.styleModel = newValue;
        } else {
            this.setDefaultValue();
        }
        
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    invokeOnChange() {
        this.onChange(this.styleModel);
    }

    onStyleChanged() {
        if (!this.isSilentChange) {
            this.styleModel.orientation = null;
            this.styleModel.arcWidth = null;
            this.styleModel.arcAngle = null;
            this.styleModel.startAngle = null;
            this.styleModel.endAngle = null;
            this.styleModel.trackWidth = null;
            this.styleModel.isInvertScale = false;
        } else {
            this.isSilentChange = false;
        }
        
        this.invokeOnChange();
    }

    onArcAngleChange() {
        if (this.styleModel.arcAngle !== GaugeStyleArcAngleEnum.Custom) {
            this.styleModel.startAngle = null;
            this.styleModel.endAngle = null;
        }
        this.invokeOnChange();
    }

    isValidState(): boolean {
        return this.isStyleValid && this.isGaugeOrientationValid && this.isInvertScaleValid && this.isTrackWidthValid && this.isArcWidthValid && this.isArcAngleValid && this.isStartAngleValid && this.isEndAngleValid;
    }

    getIconSrc(type: GaugeStyleArcAngleEnum): string {
        switch (type) {
            case GaugeStyleArcAngleEnum.TopHalf:
                return 'assets/common/images/icons/gauge_top_half.png';
            case GaugeStyleArcAngleEnum.BottomHalf:
                return 'assets/common/images/icons/gauge_bottom_half.png';
            case GaugeStyleArcAngleEnum.FullCircle:
                return 'assets/common/images/icons/gauge_full_circle.png';
            case GaugeStyleArcAngleEnum.LeftHalf:
                return 'assets/common/images/icons/gauge_left_half.png';
            case GaugeStyleArcAngleEnum.RightHalf:
                return 'assets/common/images/icons/gauge_right_half.png';
            case GaugeStyleArcAngleEnum.Quarter:
                return 'assets/common/images/icons/gauge_left_quarter.png';
            case GaugeStyleArcAngleEnum.ThreeQuarters:
                return 'assets/common/images/icons/gauge_3-4_quarters.png';
            default:
                return '';
        }
    }

    private setDefaultValue() {
        this.styleModel = {
            isInvertScale: false,
        } as GaugeStyle;
    }

    private setOrientationOptions() {
        this.orientationOptions = [
            { id: GaugeStyleOrientationEnum.Horizontal, text: this.l('Horizontal') },
            { id: GaugeStyleOrientationEnum.Vertical, text: this.l('Vertical') },
        ];
    }

    private setStyleOptions() {
        this.styleOptions = [
            { id: GaugeStyleEnum.Linear, text: this.l('Linear') },
            { id: GaugeStyleEnum.Circle, text: this.l('Circular') },
        ];
    }

    private setArcAngleOptions() {
        this.arcAngleOptions = [
            { id: GaugeStyleArcAngleEnum.TopHalf, text: this.l('TopHalf') },
            { id: GaugeStyleArcAngleEnum.FullCircle, text: this.l('FullCircle') },
            { id: GaugeStyleArcAngleEnum.BottomHalf, text: this.l('BottomHalf') },
            { id: GaugeStyleArcAngleEnum.LeftHalf, text: this.l('LeftHalf') },
            { id: GaugeStyleArcAngleEnum.RightHalf, text: this.l('RightHalf') },
            { id: GaugeStyleArcAngleEnum.ThreeQuarters, text: this.l('ThreeQuarters') },
            { id: GaugeStyleArcAngleEnum.Quarter, text: this.l('Quarter') },
            { id: GaugeStyleArcAngleEnum.Custom, text: this.l('Custom') },
        ];
    }
}
