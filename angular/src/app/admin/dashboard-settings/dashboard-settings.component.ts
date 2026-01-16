import { Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DefaultValuesServiceProxy, GetDefaultValueForEditOutput, CreateOrEditDefaultValueDto } from '@shared/service-proxies/service-proxies';
import { DefaultValueKeys } from '@shared/DefaultValueKeys';
import { finalize } from 'rxjs/operators';
import { AdvancedParameter } from './shared/advanced-parameter-selector/advanced-parameter-selector.component';

@Component({
    templateUrl: './dashboard-settings.component.html',
    styleUrls: ['./dashboard-settings.component.less'],
    animations: [appModuleAnimation()],
})
export class DashboardSettingsComponent extends AppComponentBase implements OnInit {
    // Display Format
    decimalPlacesForNumbers: number = 2;
    decimalPlacesForPercentage: number = 2;

    // Default Colors - Common Tab
    v1nColor: string = '#800000';
    v2nColor: string = '#008000';
    v3nColor: string = '#000080';
    v12Color: string = '#804000';
    v23Color: string = '#00C000';
    v31Color: string = '#004080';
    frequencyColor: string = '#B124D5';
    auxColor: string = '#400000';
    totalColor: string = '#CA6919';
    vnColor: string = '#808080';

    // Default Colors Tabs
    colorTabs = [
        { ID: 1, name: 'Common', template: 'commonTemplate' },
        { ID: 2, name: 'Advanced', template: 'advancedTemplate' },
    ];

    // Advanced parameters
    advancedParameters: AdvancedParameter[] = [];

    // Original values from DB (for cancel functionality)
    private originalValues = {
        decimalPlacesForNumbers: 2,
        decimalPlacesForPercentage: 2,
        v1nColor: '',
        v2nColor: '',
        v3nColor: '',
        v12Color: '',
        v23Color: '',
        v31Color: '',
        frequencyColor: '',
        auxColor: '',
        totalColor: '',
        vnColor: '',
        advancedParameters: '[]',
    };

    // IDs from DB (for update functionality)
    private valueIds: { [key: string]: number | undefined } = {};

    saving = false;

    constructor(injector: Injector,
        private defaultValuesServiceProxy: DefaultValuesServiceProxy) {
        super(injector);
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.loadDefaultValues();
    }

    private loadDefaultValues(): void {
        const keys = [
            DefaultValueKeys.defaultNumberOfDecimalsSettingName,
            DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName,
            ...Object.values(DefaultValueKeys.defaultColors),
            DefaultValueKeys.advancedParameterColorsSettingName,
        ];

        this.defaultValuesServiceProxy.getDefaultValueByNames(keys).subscribe((result: GetDefaultValueForEditOutput[]) => {
            result.forEach((item: GetDefaultValueForEditOutput) => {
                const key = item.defaultValue?.name;
                const value = item.defaultValue?.value;
                const id = item.defaultValue?.id;

                if (!key) {
                    return;
                }

                // Save ID for update
                this.valueIds[key] = id;

                if (!value) {
                    return;
                }

                // Map settings
                if (key === DefaultValueKeys.defaultNumberOfDecimalsSettingName) {
                    const decimals = parseInt(value, 10);
                    if (!isNaN(decimals)) {
                        this.decimalPlacesForNumbers = decimals;
                        this.originalValues.decimalPlacesForNumbers = decimals;
                    }
                } else if (key === DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName) {
                    const decimals = parseInt(value, 10);
                    if (!isNaN(decimals)) {
                        this.decimalPlacesForPercentage = decimals;
                        this.originalValues.decimalPlacesForPercentage = decimals;
                    }
                }
                // Map color values
                else if (key === DefaultValueKeys.defaultColors.v1n) {
                    this.v1nColor = value;
                    this.originalValues.v1nColor = value;
                } else if (key === DefaultValueKeys.defaultColors.v2n) {
                    this.v2nColor = value;
                    this.originalValues.v2nColor = value;
                } else if (key === DefaultValueKeys.defaultColors.v3n) {
                    this.v3nColor = value;
                    this.originalValues.v3nColor = value;
                } else if (key === DefaultValueKeys.defaultColors.v12) {
                    this.v12Color = value;
                    this.originalValues.v12Color = value;
                } else if (key === DefaultValueKeys.defaultColors.v23) {
                    this.v23Color = value;
                    this.originalValues.v23Color = value;
                } else if (key === DefaultValueKeys.defaultColors.v31) {
                    this.v31Color = value;
                    this.originalValues.v31Color = value;
                } else if (key === DefaultValueKeys.defaultColors.frequency) {
                    this.frequencyColor = value;
                    this.originalValues.frequencyColor = value;
                } else if (key === DefaultValueKeys.defaultColors.aux) {
                    this.auxColor = value;
                    this.originalValues.auxColor = value;
                } else if (key === DefaultValueKeys.defaultColors.total) {
                    this.totalColor = value;
                    this.originalValues.totalColor = value;
                } else if (key === DefaultValueKeys.defaultColors.vn) {
                    this.vnColor = value;
                    this.originalValues.vnColor = value;
                } else if (key === DefaultValueKeys.advancedParameterColorsSettingName) {
                    try {
                        const parsed = JSON.parse(value || '[]');
                        if (Array.isArray(parsed)) {
                            this.advancedParameters = parsed;
                            this.originalValues.advancedParameters = value || '[]';
                        } else {
                            this.advancedParameters = [];
                            this.originalValues.advancedParameters = '[]';
                        }
                    } catch (e) {
                        // If parsing fails, keep empty array
                        this.advancedParameters = [];
                        this.originalValues.advancedParameters = '[]';
                    }
                }
            });
        });
    }

    onParameterDelete(event: any): void {
        const index = this.advancedParameters.findIndex(p => p.id === event.data.id);
        if (index !== -1) {
            this.advancedParameters.splice(index, 1);
        }
    }

    resetToDefaults(): void {
        this.v1nColor = '#800000';
        this.v2nColor = '#008000';
        this.v3nColor = '#000080';
        this.v12Color = '#804000';
        this.v23Color = '#00C000';
        this.v31Color = '#004080';
        this.frequencyColor = '#B124D5';
        this.auxColor = '#400000';
        this.totalColor = '#CA6919';
        this.vnColor = '#808080';
    }

    saveChanges(): void {
        this.saving = true;

        const valuesToSave: CreateOrEditDefaultValueDto[] = [
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultNumberOfDecimalsSettingName],
                name: DefaultValueKeys.defaultNumberOfDecimalsSettingName,
                value: this.decimalPlacesForNumbers.toString(),
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName],
                name: DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName,
                value: this.decimalPlacesForPercentage.toString(),
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v1n],
                name: DefaultValueKeys.defaultColors.v1n,
                value: this.v1nColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v2n],
                name: DefaultValueKeys.defaultColors.v2n,
                value: this.v2nColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v3n],
                name: DefaultValueKeys.defaultColors.v3n,
                value: this.v3nColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v12],
                name: DefaultValueKeys.defaultColors.v12,
                value: this.v12Color,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v23],
                name: DefaultValueKeys.defaultColors.v23,
                value: this.v23Color,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.v31],
                name: DefaultValueKeys.defaultColors.v31,
                value: this.v31Color,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.frequency],
                name: DefaultValueKeys.defaultColors.frequency,
                value: this.frequencyColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.aux],
                name: DefaultValueKeys.defaultColors.aux,
                value: this.auxColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.total],
                name: DefaultValueKeys.defaultColors.total,
                value: this.totalColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultColors.vn],
                name: DefaultValueKeys.defaultColors.vn,
                value: this.vnColor,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.advancedParameterColorsSettingName],
                name: DefaultValueKeys.advancedParameterColorsSettingName,
                value: JSON.stringify(this.advancedParameters),
            }),
        ];

        this.defaultValuesServiceProxy
            .createOrEditValues(valuesToSave)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe({
                next: () => {
                    this.originalValues.decimalPlacesForNumbers = this.decimalPlacesForNumbers;
                    this.originalValues.decimalPlacesForPercentage = this.decimalPlacesForPercentage;
                    this.originalValues.v1nColor = this.v1nColor;
                    this.originalValues.v2nColor = this.v2nColor;
                    this.originalValues.v3nColor = this.v3nColor;
                    this.originalValues.v12Color = this.v12Color;
                    this.originalValues.v23Color = this.v23Color;
                    this.originalValues.v31Color = this.v31Color;
                    this.originalValues.frequencyColor = this.frequencyColor;
                    this.originalValues.auxColor = this.auxColor;
                    this.originalValues.totalColor = this.totalColor;
                    this.originalValues.vnColor = this.vnColor;
                    this.originalValues.advancedParameters = JSON.stringify(this.advancedParameters);

                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultNumberOfDecimalsSettingName, this.decimalPlacesForNumbers.toString());
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultNumberOfDecimalsForPercentageSettingName, this.decimalPlacesForPercentage.toString());
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v1n, this.v1nColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v2n, this.v2nColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v3n, this.v3nColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v12, this.v12Color);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v23, this.v23Color);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.v31, this.v31Color);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.frequency, this.frequencyColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.aux, this.auxColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.total, this.totalColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultColors.vn, this.vnColor);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.advancedParameterColorsSettingName, JSON.stringify(this.advancedParameters));

                    this.notify.success(this.l('SettingsSavedSuccessfully'));
                },
                error: () => {
                    this.notify.error(this.l('ErrorOccurredWhileSavingSettings'));
                },
            });
    }

    cancel(): void {
        this.decimalPlacesForNumbers = this.originalValues.decimalPlacesForNumbers || this.decimalPlacesForNumbers;
        this.decimalPlacesForPercentage = this.originalValues.decimalPlacesForPercentage || this.decimalPlacesForPercentage;
        this.v1nColor = this.originalValues.v1nColor || this.v1nColor;
        this.v2nColor = this.originalValues.v2nColor || this.v2nColor;
        this.v3nColor = this.originalValues.v3nColor || this.v3nColor;
        this.v12Color = this.originalValues.v12Color || this.v12Color;
        this.v23Color = this.originalValues.v23Color || this.v23Color;
        this.v31Color = this.originalValues.v31Color || this.v31Color;
        this.frequencyColor = this.originalValues.frequencyColor || this.frequencyColor;
        this.auxColor = this.originalValues.auxColor || this.auxColor;
        this.totalColor = this.originalValues.totalColor || this.totalColor;
        this.vnColor = this.originalValues.vnColor || this.vnColor;
        
        try {
            const parsed = JSON.parse(this.originalValues.advancedParameters || '[]');
            if (Array.isArray(parsed)) {
                this.advancedParameters = parsed;
            } else {
                this.advancedParameters = [];
            }
        } catch (e) {
            this.advancedParameters = [];
        }

        this.notify.info(this.l('ChangesCancelled'));
    }

    onParameterAdded(parameter: AdvancedParameter): void {
        this.advancedParameters.push(parameter);
    }

    deleteParameter(parameter: AdvancedParameter): void {
        const index = this.advancedParameters.findIndex(p => p.id === parameter.id);
        if (index !== -1) {
            this.advancedParameters.splice(index, 1);
        }
    }
}
