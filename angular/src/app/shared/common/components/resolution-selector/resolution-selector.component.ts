import { ChangeDetectionStrategy, Component, forwardRef, Injector, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormBuilder, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { ResolutionState } from '@app/shared/models/resolution-state';
import { DxSelectBoxModule, DxNumberBoxModule, DxTooltipModule, DxTextBoxModule } from 'devextreme-angular';
import { CustomResolutionUnitsComparer } from '@app/shared/services/custom-resolution-units-comparer';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { ChangeDetectorRef } from '@angular/core';
import { UtilsModule } from '../../../../../shared/utils/utils.module';
import { AppSharedModule } from '../../../app-shared.module';
import { AppComponentBase } from '@shared/common/app-component-base';

@Component({
    selector: 'resolutionSelector',
    standalone: true,
    imports: [CommonModule, FormsModule, DxSelectBoxModule, DxNumberBoxModule, DxTooltipModule, DxTextBoxModule, UtilsModule, AppSharedModule],
    templateUrl: './resolution-selector.component.html',
    styleUrl: './resolution-selector.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ResolutionSelectorComponent),
            multi: true,
        },
    ],
})
export class ResolutionSelectorComponent extends AppComponentBase implements ControlValueAccessor, OnInit {
    @Input() customDisabled?: boolean | undefined;
    @Input() onlyCustomAllowed?: boolean | undefined;
    @Input() tooltip?: {resolution: string; unit: string; argument: string };
    @Input() isVertical = false;
    resolutionState: ResolutionState = new ResolutionState({
        resolutionUnit: null,
        customResolutionUnit: undefined,
        customResolutionValue: undefined,
    });

    allResolutionOptions = ResolutionUnits;
    allowedResolutionOptions: Array<{ label: string; value: ResolutionUnits }>;
    allCustomResolutionOptions = CustomResolutionUnits;
    allowedCustomResolutionOptions: Array<{ label: string; value: CustomResolutionUnits }>;

    minCustomArgument = 1;
    maxCustomArgument = 99999;
    isTouched = false;
    tooltipId = `tooltip-${Math.random().toString(36).substr(2, 9)}`;

    private _minResolution: ResolutionState | undefined;
    private readonly _defaultMinResolution = new ResolutionState({
        resolutionUnit: ResolutionUnits.CUSTOM,
        customResolutionUnit: CustomResolutionUnits.MS,
        customResolutionValue: 1,
    });



    constructor(
        private fb: FormBuilder,
        private _customResolutionUnitsComparer: CustomResolutionUnitsComparer,
        private _resolutionService: ResolutionService,
        private cdr: ChangeDetectorRef,
        public injector: Injector,
    ) {
        super(injector);
    }

    @Input() set minResolution(minState: ResolutionState | undefined) {
        if (minState) {
            this._minResolution = minState;
            this.setAllowedQuickSelectResolutionUnits();
            this.setAllowedCustomResolutionUnits(this.resolutionState);
            this.setBoundsForCustomResolutionArgument(this.resolutionState.customResolutionUnit);
        }
    }

    ngOnInit(): void {
        this.changeResolutionOptions(this.resolutionState);
    }

    changeResolutionOptions(state: ResolutionState) {
        if (this.onlyCustomAllowed) {
            this.setAllowedCustomResolutionUnits(state);
            this.setBoundsForCustomResolutionArgument(state.customResolutionUnit);
        } else {
            if (state.resolutionUnit === ResolutionUnits.CUSTOM) {
                this.setAllowedCustomResolutionUnits(state);
                this.setBoundsForCustomResolutionArgument(state.customResolutionUnit);
            }
            this.setAllowedQuickSelectResolutionUnits();
        }
    }

    getUIResolutionRepresentation(unit: ResolutionUnits) {
        return unit ? this._resolutionService.getUIRepresentationForResolution(unit) : '';
    }

    onFocus() {
        this.isTouched = true;
        this.cdr.detectChanges();
    }
    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(state: ResolutionState): void {
        if (state) {
            this.changeResolutionOptions(state);
            this.resolutionState = state;
        }
    }

    isResolutionValid(): boolean {
        return !!this.resolutionState.resolutionUnit;
    }

    isCustomResolutionArgumentValid(): boolean {
        if (this.resolutionState.resolutionUnit !== this.allResolutionOptions.CUSTOM) {
            return true;
        }
        return (
            this.resolutionState.customResolutionValue !== undefined &&
            this.resolutionState.customResolutionValue >= this.minCustomArgument &&
            this.resolutionState.customResolutionValue <= this.maxCustomArgument
        );
    }

    isCustomResolutionUnitValid(): boolean {
        if (this.resolutionState.resolutionUnit !== this.allResolutionOptions.CUSTOM) {
            return true;
        }
        return !!this.resolutionState.customResolutionUnit;
    }

    isValid(): boolean {
        return this.isResolutionValid() && this.isCustomResolutionArgumentValid() && this.isCustomResolutionUnitValid();
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    updateResolutionModel() {
        this.onChange(this.resolutionState);
        this.onTouched();
    }

    onResolutionUnitChange(unit: ResolutionUnits) {
        if (this.resolutionState.resolutionUnit === ResolutionUnits.CUSTOM && unit !== ResolutionUnits.CUSTOM) {
            this.resolutionState.customResolutionValue = null;
            this.resolutionState.customResolutionUnit = null;
        }

        this.resolutionState.resolutionUnit = unit;

        if (unit === ResolutionUnits.CUSTOM) {
            this.setAllowedCustomResolutionUnits(this.resolutionState);
        }
        this.updateResolutionModel();
    }

    onCustomResolutionUnitChange() {
        this.updateResolutionModel();
        this.setBoundsForCustomResolutionArgument(this.resolutionState.customResolutionUnit);
    }

    private setBoundsForCustomResolutionArgument(unit: CustomResolutionUnits) {
        let bounds = this.getCustomArgumentBounds(unit);
        this.minCustomArgument =
            unit === this._minResolution.customResolutionUnit
                ? Math.max(this._minResolution.customResolutionValue, bounds[0])
                : bounds[0];
        this.maxCustomArgument = bounds[1];
    }

    private setAllowedQuickSelectResolutionUnits() {
        if (this._minResolution) {
            let allowedQuickSelect: ResolutionUnits[] = this.customDisabled
                ? [ResolutionUnits.AUTO]
                : [ResolutionUnits.CUSTOM, ResolutionUnits.AUTO];

            let resolutionUnitsWithoutCustom = Object.values(this.allResolutionOptions)
                .filter(
                    (unit) =>
                        unit !== this.allResolutionOptions.CUSTOM &&
                        unit !== this.allResolutionOptions.AUTO
                );

            for (let unit of resolutionUnitsWithoutCustom) {
                let unitModel = this._resolutionService.parseStateFromString(unit, true);

                const isGreaterOrEqual = this._customResolutionUnitsComparer.greaterOrEqual(unitModel.customResolutionUnit, this._minResolution.customResolutionUnit);

                if (isGreaterOrEqual) {
                    allowedQuickSelect.push(unit);
                }
            }

            this.allowedResolutionOptions = allowedQuickSelect.map(unit => ({
                label: this.l(unit),
                value: unit
            }));

        } else {
            this.minResolution = this._defaultMinResolution;
        }
    }

    private setAllowedCustomResolutionUnits(state: ResolutionState) {
        if (this._minResolution) {
            this.allowedCustomResolutionOptions = Object.values(this.allCustomResolutionOptions)
            .filter((unit: CustomResolutionUnits) =>
                this._customResolutionUnitsComparer.greaterOrEqual(unit, this._minResolution.customResolutionUnit)
            )
            .map((unit: CustomResolutionUnits) => ({
                label: this._resolutionService.getDisplayNameForCustomResolution(unit),
                value: unit
            }));

            if (!this.allowedCustomResolutionOptions.some(opt => opt.value === state.customResolutionUnit)) {
                state.customResolutionUnit = this._minResolution.customResolutionUnit;
                if (this._minResolution.customResolutionValue > state.customResolutionValue) {
                    state.customResolutionValue = this._minResolution.customResolutionValue;
                }
            }

            if (
                state.customResolutionUnit === this._minResolution.customResolutionUnit &&
                this._minResolution.customResolutionValue > state.customResolutionValue
            ) {
                state.customResolutionValue = this._minResolution.customResolutionValue;
            }

        } else {
            this.minResolution = this._defaultMinResolution;
        }
    }

    private getCustomArgumentBounds(unit: CustomResolutionUnits): [number, number] {
        switch (CustomResolutionUnits[unit]) {
            case CustomResolutionUnits.MS:
                return [1, 999];
            case CustomResolutionUnits.SEC:
                return [1, 59];
            case CustomResolutionUnits.MIN:
                return [1, 59];
            case CustomResolutionUnits.HOUR:
                return [1, 23];
            case CustomResolutionUnits.DAY:
                return [1, 6];
            case CustomResolutionUnits.WEEK:
                return [1, 999];
            default:
                return [0, 0];
        }
    }

    // eslint-disable-next-line @typescript-eslint/member-ordering
    protected readonly document = document;
}
