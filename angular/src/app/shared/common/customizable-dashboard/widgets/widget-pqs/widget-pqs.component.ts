import {
    Component,
    OnInit,
    Injector,
    ViewChild,
    ViewChildren,
    QueryList,
    ElementRef,
    OnDestroy,
    AfterViewInit,
    Output,
    EventEmitter,
    ChangeDetectorRef,
} from '@angular/core';
import {
    CreateOrEditTrendWidgetConfigurationDto,
    FeederComponentInfo,
    TrendParameter,
    PQSRestApiServiceProxy,
    TenantDashboardServiceProxy,
    TrendCalcRequest,
    TrendCustomWidgetData,
    BaseData,
    CalculatedDataItem,
    DataUnitType,
    TrendResponse,
    IntervalSynchronized,
    TrendWidgetConfigurationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { TreeDragDropService } from 'primeng/api';
import { Subject, Subscription, catchError, takeUntil, throwError, timer, tap } from 'rxjs';
import { CreateOrEditTrendConfigurationComponent } from './create-or-edit-trend-configuration/create-or-edit-trend-configuration.component';
import { Guid } from 'guid-ts';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { DxChartComponent } from 'devextreme-angular';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DateTime } from 'luxon';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { ColumnType } from '@app/shared/enums/column-type';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { TrendWidgetConfigurationService } from '@app/shared/services/widget-configurations/trend-widget-configuration.service';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateRangeType } from '@app/shared/enums/date-range-type';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { CustomResolutionUnits } from '@app/shared/enums/custom-resolution-selection-units';
import { ThresholdSettingsModel } from '@app/shared/models/threshold-settings-model';


interface ParameterColorModel {
    color: string;
    dataUnitType?: DataUnitType;
}

type LineLegendType = {
    name: string,
    id: string,
    color?: string,
    parameterId?: string,
    isEditing?: boolean,
    customName?: string,
    axisName?: string,
    dataUnitType?: DataUnitType,
    isHidden?: boolean
};

enum PowerFactorDisplayMode {
    Load = 'Load',
    Source = 'Source'
}

interface PowerFactorResult {
    value: number;
    units: string;
}

interface CustomAxisLabel {
    value: number;
    text: string;
}

class PowerFactorUtils {
    private static readonly POWER_FACTOR_UNIT_ID = 9;
    // Default to Load mode - this should come from user settings
    private static displayMode: PowerFactorDisplayMode = PowerFactorDisplayMode.Load;

    static setDisplayMode(mode: PowerFactorDisplayMode): void {
        this.displayMode = mode;
    }

    static normalizeToCapInd(value: number, isRawData: boolean = false): { value: number; units: string; powerFactor: number } {
        if (isRawData) {
            return { value, units: '', powerFactor: value };
        }

        let powerFactor: number;
        let displayedValue: number;

        switch (this.displayMode) {
            case PowerFactorDisplayMode.Load:
                ({ powerFactor, displayedValue } = this.normalizeLoadToCapInd(value));
                break;
            case PowerFactorDisplayMode.Source:
                ({ powerFactor, displayedValue } = this.normalizeSourceToCapInd(value));
                break;
            default:
                powerFactor = value;
                displayedValue = value;
        }

        const units = this.getCapIndUnit(powerFactor);
        return { value: displayedValue, units, powerFactor };
    }

    static normalizeLoadToCapInd(value: number): { powerFactor: number; displayedValue: number } {
        const powerFactor = value;
        let displayedValue: number;

        // For IND (powerFactor >= 1): displayedValue = powerFactor (display the actual PF value)
        // For CAP (powerFactor < 1): displayedValue = 1 - powerFactor
        if (powerFactor >= 1) {
            displayedValue = powerFactor;
        } else {
            displayedValue = 1 - Math.abs(powerFactor);
        }

        displayedValue = Math.round(displayedValue * 1000000) / 1000000; // round to 6 decimals to clear noise
        return { powerFactor, displayedValue };
    }

    static normalizeSourceToCapInd(value: number): { powerFactor: number; displayedValue: number } {
        let powerFactor: number;
        let displayedValue: number;

        if (value < 0) {
            powerFactor = value + 2;
            displayedValue = 1 - powerFactor;
        } else {
            powerFactor = value - 2;
            displayedValue = powerFactor + 1;
        }

        displayedValue = Math.round(displayedValue * 1000000) / 1000000; // round to 6 decimals to clear noise
        return { powerFactor, displayedValue };
    }

    static calculateStep(min: number, max: number, numOfTicks: number = 8): number {
        const distance = Math.abs(max - min);
        const step = distance / (numOfTicks - 1);

        const stepLog = this.getLog10(step);
        const stepRoundValue = Math.pow(10, stepLog);

        const stepMainDigits = Math.round((step / stepRoundValue) * 100) / 100;

        let factor = 1;
        if (stepMainDigits < 1.5) {
            factor = 1;
        } else if (stepMainDigits < 3.5) {
            factor = 2;
        } else if (stepMainDigits < 7.5) {
            factor = 5;
        } else {
            factor = 10;
        }

        return factor * stepRoundValue;
    }

    private static getLog10(value: number): number {
        if (value === 0) return 0;
        const log = Math.floor(Math.log10(Math.abs(value)));
        return log;
    }

    static createCapIndLabels(min: number, step: number, maxPoint: number, precision: number = 2): CustomAxisLabel[] {
        const labels: CustomAxisLabel[] = [];
        let notRoundValueOnAxis = maxPoint;

        // Use a small tolerance for floating-point comparison
        const tolerance = 0.0000001;

        while (notRoundValueOnAxis >= min - tolerance) {
            let powerFactor: number;
            let displayedPoint: number;
            let unitsDescription: string;

            switch (this.displayMode) {
                case PowerFactorDisplayMode.Load:
                    powerFactor = notRoundValueOnAxis;
                    // For IND (powerFactor >= 1): displayedPoint = powerFactor (display actual PF value)
                    // For CAP (powerFactor < 1): displayedPoint = 1 - powerFactor
                    if (powerFactor >= 1) {
                        displayedPoint = powerFactor;
                    } else {
                        displayedPoint = 1 - Math.abs(powerFactor);
                    }
                    displayedPoint = Math.round(displayedPoint * 1000000) / 1000000;
                    break;

                case PowerFactorDisplayMode.Source:
                    if (notRoundValueOnAxis < 0) {
                        powerFactor = notRoundValueOnAxis + 2;
                        displayedPoint = 1 - powerFactor;
                    } else {
                        powerFactor = notRoundValueOnAxis - 2;
                        displayedPoint = powerFactor + 1;
                    }
                    displayedPoint = Math.round(displayedPoint * 1000000) / 1000000;
                    break;
            }

            // Adjust to the IEC standard
            if (powerFactor <= -1 || (powerFactor < 1 && powerFactor > 0)) {
                unitsDescription = 'CAP';
            } else {
                unitsDescription = 'IND';
            }

            const valueOnAxis = Math.round(notRoundValueOnAxis * 1000000) / 1000000;
            const formattedValue = `${Math.round(displayedPoint * Math.pow(10, precision)) / Math.pow(10, precision)} ${unitsDescription}`;

            labels.push({
                value: valueOnAxis,
                text: formattedValue
            });

            notRoundValueOnAxis -= step;
        }

        // Add explicit transition points if the range crosses them
        if (this.displayMode === PowerFactorDisplayMode.Load) {
            // Transition at PF=0 (between negative IND and positive CAP)
            // Displays as "1 IND" since displayedValue = 1 - |0| = 1
            if (min < 0 && maxPoint > 0) {
                const hasCloseLabel = labels.some(label => Math.abs(label.value - 0) < tolerance);
                if (!hasCloseLabel) {
                    labels.push({
                        value: 0.0,
                        text: `1 IND`
                    });
                }
            }

            // Transition at PF=1.0 (between CAP and positive IND)
            // Displays as "1 IND" since displayedValue = 1
            if (min < 1.0 && maxPoint > 1.0) {
                const hasCloseLabel = labels.some(label => Math.abs(label.value - 1.0) < tolerance);
                if (!hasCloseLabel) {
                    labels.push({
                        value: 1.0,
                        text: `1 IND`
                    });
                }
            }
        }

        // Sort labels by value in descending order (highest PF to lowest PF)
        labels.sort((a, b) => b.value - a.value);

        return labels;
    }

    static getPowerFactorDisplay(
        value: number,
        dataUnitType: DataUnitType | undefined
    ): PowerFactorResult {
        if (!dataUnitType || dataUnitType.id !== this.POWER_FACTOR_UNIT_ID) {
            return { value, units: '' };
        }

        // value is the original power factor (e.g., 0.88, -0.5)
        // Normalize it for display
        const normalized = this.normalizeToCapInd(value, false);

        return { value: normalized.value, units: normalized.units };
    }

    private static getCapIndUnit(powerFactor: number): string {
        // Adjust to the IEC standard
        if (powerFactor <= -1 || (powerFactor < 1 && powerFactor > 0)) {
            return 'CAP';
        } else {
            return 'IND';
        }
    }
}

type ValueAxisConfig = {
    name: string,
    position: 'left' | 'right',
    unitLabel: string,
    color: string,
    isPrimary: boolean,
    isPowerFactor?: boolean,
    customLabels?: CustomAxisLabel[],
    tickValues?: number[]
};

class LineChartConfiguration {
    lineLegend: LineLegendType[];
    color: string;
    overlappingMode: string;
    valueAxes: ValueAxisConfig[];
}

//#region LineChart
class LineChart extends DashboardChartBase {
    chartData: any[];
    errorMessage: string | null = null;
    //#region DevExtreme Chart
    chartConfiguration: LineChartConfiguration = {
        lineLegend: [],
        color: 'green',
        overlappingMode: 'hide',
        valueAxes: [],
    };
    //#endregion
    formatDateTime: (date: Date) => string = (date) => {
        const dateTime = DateTime.fromJSDate(date);
        return dateTime.toFormat('dd/MM/yyyy HH:mm');
    };
    numberOfDecimals: number = 2;
    numberOfDecimalsForPercentage: number = 2;

    constructor(
        private _dashboardService: TenantDashboardServiceProxy,
        private setErrorMessage: (error: string | null) => void,
        private localize: (key: string) => string,
        formatDateTimeFn?: (date: Date) => string,
        numberOfDecimals?: number,
        numberOfDecimalsForPercentage?: number,
    ) {
        super();
        if (formatDateTimeFn) {
            this.formatDateTime = formatDateTimeFn;
        }
        if (numberOfDecimals !== undefined) {
            this.numberOfDecimals = numberOfDecimals;
        }
        if (numberOfDecimalsForPercentage !== undefined) {
            this.numberOfDecimalsForPercentage = numberOfDecimalsForPercentage;
        }
    }

    init(trend: TrendResponse, parameters?: WidgetParametersColumn[], updateParameterColorMap?: (parameterName: string, dataUnitType: DataUnitType) => void, parameterColorMap?: Map<string, ParameterColorModel>) {
        const data = trend.data;
        this.chartData = [];
        this.chartConfiguration.lineLegend = [];
        this.chartConfiguration.valueAxes = [];

        let map: Map<number, object> = new Map(); // number is datetime representation in UNIX sec

        // Map to track axes by unit type (to share axes with same unit)
        const axisMap = new Map<string, string>(); // unitLabel -> axisName
        let axisCounter = 0;
        let primaryAxisSet = false;

        // Track power factor data for custom axis labels
        const powerFactorDataByAxis = new Map<string, number[]>(); // axisName -> original values

        for (let i = 0; i < data.length; i++) {
            const dataItem = data[i];
            let legendLabel = dataItem.parameterName || this.parameterName(dataItem);
            let dataId = Guid.newGuid().toString();
            const parameter = parameters?.[i];
            const styleData = parameter?.style ? JSON.parse(parameter.style) : {};
            let color = styleData?.color;

            if (!color && parameterColorMap && parameter) {
                // Try to find color from parameterColorMap using the parameter's name directly
                let colorModel = parameterColorMap.get(parameter.name);

                // If not found, try extracting from dataItem.parameterName
                if (!colorModel && dataItem.parameterName) {
                    const extractedName = dataItem.parameterName.slice(dataItem.parameterName.indexOf("-") + 1).trim();
                    colorModel = parameterColorMap.get(extractedName);
                }

                color = colorModel?.color;

                // Store the color back to the parameter's style so it persists
                if (color && parameter) {
                    const updatedStyle = { ...styleData, color: color };
                    parameter.style = JSON.stringify(updatedStyle);
                }
            }

            const customName = styleData?.customName;
            const dataUnitType = dataItem.dataUnitType;
            dataItem.dataUnitType = dataUnitType;
            const unitLabel = this.getUnitLabel(dataUnitType);
            const axisColor = color || this.chartConfiguration.color;
            const isPowerFactor = dataUnitType?.id === 9;

            // Update parameter color map with dataUnitType
            if (updateParameterColorMap && dataItem.parameterName) {
                updateParameterColorMap(dataItem.parameterName, dataUnitType);
            }

            // Determine which axis to use - reuse existing axis if same unit
            let axisName: string;
            const unitKey = unitLabel || 'no-unit';

            if (axisMap.has(unitKey)) {
                // Reuse existing axis for this unit
                axisName = axisMap.get(unitKey)!;
            } else {
                // Create new axis for this unit
                axisName = `axis-${axisCounter}`;
                axisMap.set(unitKey, axisName);

                // Determine position: alternate between left and right for different units
                const position = axisCounter % 2 === 0 ? 'left' : 'right';

                this.chartConfiguration.valueAxes.push({
                    name: axisName,
                    position: position,
                    unitLabel: isPowerFactor ? '' : unitLabel,  // Power factor axes don't need unit label since CAP/IND is shown in each tick
                    color: axisColor,
                    isPrimary: !primaryAxisSet,
                    isPowerFactor: isPowerFactor,
                });

                primaryAxisSet = true;
                axisCounter++;
            }

            this.chartConfiguration.lineLegend.push({
                name: legendLabel,
                id: dataId,
                color: color,
                parameterId: parameter?.id,
                customName: customName,
                isEditing: false,
                axisName: axisName,
                dataUnitType: dataUnitType,
            });

            // For power factor data, collect original values for axis label calculation
            if (isPowerFactor) {
                if (!powerFactorDataByAxis.has(axisName)) {
                    powerFactorDataByAxis.set(axisName, []);
                }
                const originalValues = powerFactorDataByAxis.get(axisName)!;
                dataItem.data.forEach(val => originalValues.push(val));
            }

            for (let j = 0; j < dataItem.data.length; j++) {
                // Store the data value as-is (no transformation)
                // The Y-axis labels will show the normalized CAP/IND format
                let dataValue = dataItem.data[j];

                if (map.has(trend.timeStamps[j])) {
                    let obj = map.get(trend.timeStamps[j]);
                    obj[dataId] = dataValue;
                } else {
                    let obj = new Object();
                    obj[dataId] = dataValue;
                    map.set(trend.timeStamps[j], obj);
                }
            }
        }

        // Generate custom axis labels for power factor axes
        powerFactorDataByAxis.forEach((originalValues, axisName) => {
            const axis = this.chartConfiguration.valueAxes.find(a => a.name === axisName);
            if (axis && originalValues.length > 0) {
                // Calculate min and max from original values
                const min = Math.min(...originalValues);
                const max = Math.max(...originalValues);

                // Calculate step
                const step = Math.round(PowerFactorUtils.calculateStep(min, max, 8) * 10000000) / 10000000;

                // Create custom labels
                const customLabels = PowerFactorUtils.createCapIndLabels(min, step, max, this.numberOfDecimals);

                axis.customLabels = customLabels;
                // Set tick values to match our custom labels so DevExtreme generates ticks at these exact positions
                axis.tickValues = customLabels.map(l => l.value);
            }
        });

        let arr = Array.from(map, ([key, value]) => {
            return {
                key: DateTime.fromSeconds(key).toJSDate(),
                ...value,
            };
        });

        this.chartData = arr;
        this.isInitialLoad = false;
    }

    parameterName(parameter: CalculatedDataItem): string {
        const feedersJoined = parameter.feeders
            .map((f) => {
                const parts = [f.name ? ` ${f.name}` : f.componentId, f.id !== undefined ? `${f.id}` : ''];
                return parts.join('').trim();
            })
            .join(',\n');

        let result = parameter.parameterName;
        if (parameter.parameterName) {
            return `${feedersJoined} ${result}`;
        }

        return feedersJoined;
    }

    customizeTooltip = (object: any) => {
        const parsed = parseFloat(object.originalValue);
        if (isNaN(parsed)) {
            return {
                text: `${object.seriesName}<br/>${object.originalValue}`,
            };
        }

        // Find the legend item by series name or by series tag (dataId)
        const legendItem = this.chartConfiguration.lineLegend.find(
            (item) => item.name === object.seriesName || item.id === object.series?.tag
        );

        // Check if dataUnitType is power factor (id 9)
        const isPowerFactor = legendItem?.dataUnitType && legendItem.dataUnitType.id === 9;

        if (isPowerFactor) {
            // parsed is the original power factor value (e.g., 0.88, -0.5)
            // Normalize it for display
            const normalized = PowerFactorUtils.normalizeToCapInd(parsed, false);
            const decimals = this.numberOfDecimals;
            const formattedValue = normalized.value.toFixed(decimals);
            const displayText = normalized.units ? `${formattedValue} ${normalized.units}` : formattedValue;
            return {
                text: `${object.seriesName}<br/>${displayText}`,
            };
        }

        // Check if dataUnitType is percentage (id 18 or 19)
        const isPercentage = legendItem?.dataUnitType &&
            (legendItem.dataUnitType.id === 18 || legendItem.dataUnitType.id === 19);

        const decimals = isPercentage ? this.numberOfDecimalsForPercentage : this.numberOfDecimals;
        const res = parsed.toFixed(decimals);

        return {
            text: `${object.seriesName}<br/>${res}`,
        };
    };

    customizeAxisLabel = (object: any) => {
        if (object.value && object.value instanceof Date) {
            return this.formatDateTime(object.value);
        }
        return object.valueText || '';
    };

    customizePowerFactorAxisLabel = (value: any) => {
        const numValue = typeof value === 'number' ? value : parseFloat(value.value || value);
        if (isNaN(numValue)) {
            return '';
        }

        // numValue is the original power factor value
        // Normalize it for display
        const normalized = PowerFactorUtils.normalizeToCapInd(numValue, false);
        const formattedValue = normalized.value.toFixed(2);

        return `${formattedValue} ${normalized.units}`;
    };

    private getUnitLabel(dataUnitType?: DataUnitType): string {
        if (!dataUnitType?.id) {
            return '';
        }

        return dataUnitType.id !== 41 && dataUnitType.id !== 255 && dataUnitType.tokenCode
            ? this.localize(dataUnitType.tokenCode)
            : '';
    }

    reload(input: TrendCalcRequest, parameters?: WidgetParametersColumn[], updateParameterColorMap?: (parameterName: string, dataUnitType: DataUnitType) => void, parameterColorMap?: Map<string, ParameterColorModel>) {
        this.showLoading();

        var sub = this._dashboardService
            .pQSTrendData(input)
            .pipe(
                catchError((error) => {
                    this.hideLoading();
                    return throwError(() => error);
                }),
            )
            .subscribe((result) => {
                if (!result.isSuccess) {
                    this.setErrorMessage(this.errorMessage);
                } else if (!result.data || result.data.length === 0) {
                    this.errorMessage = 'No Data for Selected Values';
                    this.setErrorMessage(this.errorMessage);
                } else {
                    this.errorMessage = null;
                    this.setErrorMessage(null);
                    this.init(result, parameters, updateParameterColorMap, parameterColorMap);
                }
                this.hideLoading();
                sub.unsubscribe();
            });
    }
}

@Component({
    selector: 'app-widget-pqs',
    templateUrl: './widget-pqs.component.html',
    styleUrls: ['./widget-pqs.component.css'],
    providers: [TreeDragDropService],
})
export class WidgetPQSComponent extends WidgetComponentBaseComponent implements OnInit, AfterViewInit, OnDestroy {
    @ViewChild(DxChartComponent, { static: false }) chartComponent!: DxChartComponent;
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditTrendConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;
    @ViewChildren('colorPicker') colorPickers!: QueryList<ElementRef<HTMLInputElement>>;
    @Output() widgetRefresh: EventEmitter<any> = new EventEmitter();

    errorMessage: string | null = null;

    lineChart: LineChart;

    isStepLine = false;
    isLinePoints = true;
    stopStream$ = new Subject();
    isActive = false;

    allComponents: any[];
    trendWidgetConfiguration: CreateOrEditTrendWidgetConfigurationDto;
    currentParameters: WidgetParametersColumn[] = [];
    chartWidth: number;
    thresholdSettings: ThresholdSettingsModel = new ThresholdSettingsModel();
    parameterColorMap: Map<string, ParameterColorModel> = new Map();

    chartHeight: number = 300; // default value
    headerBackgroundColor = '#ffffff';
    widgetBackgroundColor = '#ffffff';
    chartLineColor = '#3699ff';
    chartBackgroundColor = '#ffffff';
    headerTitleColor = '#000000';
    headerTitleBadgeColor = 'rgba(0, 0, 0, 0.08)';
    hoveredSeriesName: string | null = null;

    protected _defaultWidgetName;
    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        private _tenantdashboardService: TenantDashboardServiceProxy,
        private _trendWidgetConfigurationService: TrendWidgetConfigurationService,
        private _trendWidgetConfigurationServiceProxy: TrendWidgetConfigurationsServiceProxy,
        public elementRef: ElementRef,
        dateRangeService: DateRangeService,
        private _resolutionService: ResolutionService,
        private _configurationVersionService: ConfigurationVersionService,
        private cdr: ChangeDetectorRef,
        private _dateTimeService: DateTimeService,
    ) {
        super(injector, elementRef, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSTrend');
        this.lineChart = new LineChart(
            this._tenantdashboardService,
            (error) => {
                this.errorMessage = error;
            },
            (key) => this.l(key),
            (date: Date) => {
                return this._dateTimeService.formatDateTimeForDisplay(DateTime.fromJSDate(date));
            },
            this.defaultNumberOfDecimals ?? 2,
            this.defaultNumberOfDecimalsForPercentage ?? 2
        );
        this.lineChart.isInitialLoad = true;
    }

    ngOnInit() {
        super.ngOnInit();
        this.loadColorPreferences();
        
        // Update lineChart formatting functions after default values are loaded
        this.ensureDefaultValuesLoaded().subscribe(() => {
            if (this.lineChart) {
                this.lineChart.formatDateTime = (date: Date) => {
                    return this._dateTimeService.formatDateTimeForDisplay(DateTime.fromJSDate(date));
                };
                this.lineChart.numberOfDecimals = this.defaultNumberOfDecimals ?? 2;
                this.lineChart.numberOfDecimalsForPercentage = this.defaultNumberOfDecimalsForPercentage ?? 2;
            }
        });
    }

    ngAfterViewInit() {
        this.runDelayed(() => {
            if (this.isNew) {
                this.onEditRequested(null);
            }
            this.chartWidth = this.chartComponent.instance.element().clientWidth;

            // Remove white padding from gridster-item parent
            this.removeGridsterPadding();
        });
    }

    private removeGridsterPadding(): void {
        const hostElement = this.elementRef.nativeElement;
        const gridsterItem = hostElement.closest('gridster-item');

        if (gridsterItem) {
            (gridsterItem as HTMLElement).style.background = 'transparent';
            (gridsterItem as HTMLElement).style.padding = '0';
        }
    }

    customizePoint = (pointInfo: any) => {
        if (this.thresholdSettings.deviationColor && (pointInfo.value > this.thresholdSettings.upperValue
            || pointInfo.value < this.thresholdSettings.lowerValue)) {
            return { color: this.thresholdSettings.deviationColor, size: 7 };
        }
    };

    getCustomLabelFormatter(customLabels: CustomAxisLabel[]): (value: any) => string {
        return (value: any) => {
            const numValue = typeof value === 'number' ? value : parseFloat(value.value || value);
            if (isNaN(numValue)) {
                return '';
            }

            // Find the closest custom label
            const closest = customLabels.reduce((prev, curr) => {
                return Math.abs(curr.value - numValue) < Math.abs(prev.value - numValue) ? curr : prev;
            });

            // If the value is very close to a custom label value, use the custom text
            if (Math.abs(closest.value - numValue) < 0.0001) {
                return closest.text;
            }

            // Otherwise, use default power factor formatting
            return this.lineChart.customizePowerFactorAxisLabel(numValue);
        };
    }

    edit() {
        this.isEditModalInitialized = true;
        this._configurationVersionService.refreshVersion().subscribe(() => {
            setTimeout(() => {
                this.createOrEditModal.show(this.widgetConfigurationInDB);
            }, 0);
        });
    }

    onNameEdit() {
        this.renameModal.show(this.widgetConfigurationInDB?.name);
    }

    protected override onEditRequested(_payload: any): void {
        this.edit();
    }

    protected override onRenameRequested(_payload: any): void {
        this.onNameEdit();
    }


    refreshWidget(): void {
        const sub = this.ensureDefaultValuesLoaded().subscribe(() => {
            if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
                const configSub = this._trendWidgetConfigurationService
                    .getForEdit(+this.widgetConfigurationInDB.configuration)
                    .subscribe((result) => {
                        this.trendWidgetConfiguration = result.trendWidgetConfiguration;

                        if (this.trendWidgetConfiguration) {
                            let isAutoResolution = this.trendWidgetConfiguration.resolution === ResolutionUnits.AUTO;
                            let resulutionValueInMs = 0;

                            if (isAutoResolution) {
                                resulutionValueInMs = this.calculateAutoResolution().toRefresh;
                            } else {
                                resulutionValueInMs = this._resolutionService.resolutionValueInMs(
                                    this._resolutionService.parseStateFromString(
                                        this.trendWidgetConfiguration.resolution,
                                        true,
                                    ),
                                );
                            }
                            var dateModel = DateRangeAndRefreshModelNew.createItem(this.trendWidgetConfiguration.dateRange);
                            this.thresholdSettings = this.trendWidgetConfiguration.thresholdSettings
                                ? JSON.parse(this.trendWidgetConfiguration.thresholdSettings)
                                : new ThresholdSettingsModel();

                            this.currentParameters = JSON.parse(this.trendWidgetConfiguration.parameters);

                            // Build color map for parameters using advanced colors, phase colors and defaults
                            this.buildParameterColorMap();

                            if (dateModel.rangeUnit === DateRangeType.Relative && resulutionValueInMs) {
                                timer(0, resulutionValueInMs)
                                    .pipe(takeUntil(this.stopStream$))
                                    .subscribe((result) => {
                                        let input = this.formatRequest();
                                        input.isRealTime = true;
                                        input.refreshRateInSeconds = resulutionValueInMs / 1000;
                                        if (input) {
                                            this.lineChart.reload(input, this.currentParameters, (parameterName: string, dataUnitType: DataUnitType) => {
                                                const model = this.parameterColorMap.get(parameterName);
                                                if (model) {
                                                    model.dataUnitType = dataUnitType;
                                                }
                                            }, this.parameterColorMap);

                                            // After first load, save the updated parameters with colors
                                            if (result === 0) {
                                                this.saveParameterColors();
                                            }
                                        }
                                    });
                            } else {
                                let input = this.formatRequest();
                                if (input) {
                                    this.lineChart.reload(input, this.currentParameters, (parameterName: string, dataUnitType: DataUnitType) => {
                                        const model = this.parameterColorMap.get(parameterName);
                                        if (model) {
                                            model.dataUnitType = dataUnitType;
                                        }
                                    }, this.parameterColorMap);

                                    // Save the updated parameters with colors
                                    this.saveParameterColors();
                                }
                            }
                        }
                    });
                this.subs.push(configSub);
            }
        });
        this.subs.push(sub);
    }

    calculateAutoResolution(): { toServer: number; toRefresh: number } {
        /** TBD **/
        if (!this.trendWidgetConfiguration.dateRange) {
            throw new Error('No dateRange for calculating AUTO resolution');
        }
        let calculatedDateRange = this._dateRangeService.getDateRangeFromNewState(
                DateRangeAndRefreshModelNew.createItem(this.trendWidgetConfiguration.dateRange),
            );
        let dateRangeInMs: number;
        dateRangeInMs = DateTime.fromJSDate(calculatedDateRange[1]).toMillis() - DateTime.fromJSDate(calculatedDateRange[0]).toMillis();

        this.chartWidth = this.chartComponent?.instance.element().clientWidth ?? this.chartWidth;
        return {
            toServer: Math.floor(this.chartWidth * 0.75),
            toRefresh: Math.floor((dateRangeInMs / this.chartWidth) * 0.75),
        };
    }

    onEditModelClose(isSaved) {
        if (!isSaved && !this.widgetConfigurationInDB?.configuration) {
            abp.event.trigger('app.dashboard.removeWidget', this.widgetConfigurationInDB.widgetGuid, 'Widgets_Tenant_PQSTrend');
        }
    }

    formatRequest(): TrendCalcRequest | null {
        try {
            let dateRange = this._dateRangeService.getDateRangeFromNewState(
                DateRangeAndRefreshModelNew.createItem(this.trendWidgetConfiguration.dateRange),
            );

            const state = this._resolutionService.parseStateFromString(this.trendWidgetConfiguration.resolution, true);

            let resolutionInSeconds = 0;
            const isAutoResolution = state.isAuto;

            if (isAutoResolution) {
                resolutionInSeconds = this.calculateAutoResolution().toServer;
            } else {
                resolutionInSeconds = this._resolutionService.resolutionValueInSeconds(state);
            }

            let parameters: WidgetParametersColumn[] = JSON.parse(this.trendWidgetConfiguration.parameters);
            let request: TrendCalcRequest = new TrendCalcRequest({
                isAutoResolution,
                resolutionInSeconds,
                userTimeZone: this._dateTimeService.getUserTimeZoneName(),
                utcOffsetMinutes: this._dateTimeService.GetUtcOffsetMinutes(this.defaultUtcOffset),
                isMondayStartOfWeek: this._dateTimeService.IsMondayFirstDayOfWeek(this.defaultFirstDayOfWeek),
                widgetName: this.widgetConfigurationInDB?.name,
                startDate: DateTime.fromJSDate(dateRange[0]),
                endDate: DateTime.fromJSDate(dateRange[1]),
                refreshRateInSeconds: 0,
                isRealTime: false,
                selectedResolution: this.getSelectedIntervalResolution(
                    state.customResolutionUnit,
                    isAutoResolution,
                    state.customResolutionValue,
                ),

                // resolution:
                //     this.trendWidgetConfiguration.resolution === ResolutionUnits.AUTO
                //         ? `AUTO(${this.calculateAutoResolution().toServer})`
                //         : this._resolutionService.formatForRequest(
                //             this._resolutionService.parseStateFromString(
                //                 this.trendWidgetConfiguration.resolution,
                //                 true,
                //             ),
                //         ),
                parameters: parameters.map((parameter) => {
                    let customData: TrendCustomWidgetData = null;
                    let baseData: BaseData = null;

                    switch (parameter.type) {
                        case ColumnType.CustomParameter:
                            customData = TrendCustomWidgetData.fromJS({
                                id: Number(parameter.data),
                                ignoreAlignment: false,
                                quantity: parameter.quantity,
                            });

                            break;

                        case ColumnType.BaseParameter:
                            const parsed = JSON.parse(parameter.data as string);
                            baseData = BaseData.fromJS?.(parsed) ?? parsed;
                            baseData.type = parsed.type;

                            break;

                        case ColumnType.Exception:
                            customData = TrendCustomWidgetData.fromJS({
                                id: Number(parameter.data),
                                ignoreAlignment: false,
                                quantity: parameter.quantity,
                            });
                            break;

                        default:
                            console.warn('Unknown ColumnType:', parameter.type);
                    }
                    // customData.quantity = parameter.quantity;

                    const trendParameter = new TrendParameter({
                        customData,
                        baseData,
                        // data: parameter.data.toString(),
                        // quantity: parameter.quantity,
                        type: parameter.type,
                        feeders: parameter.componentsState?.feeders?.map((feeder) => {
                            return new FeederComponentInfo({
                                id: feeder.id,
                                componentId: feeder.componentId,
                                name: feeder.name,
                                compName: parameter.componentsState?.components?.find(
                                    (c) => c.key === feeder.componentId,
                                )?.label,
                            });
                        }),
                    });

                    return trendParameter;
                }),
            });

            return request;
        } catch (error: any) {
            this.lineChart.loading = false;
            console.log('Error', error);
            return null;
        }
    }

    onLegendMarkerClick(legendItem: LineLegendType, event: MouseEvent): void {
        event.stopPropagation();

        // Toggle visibility
        legendItem.isHidden = !legendItem.isHidden;

        // Find the series in the chart and toggle its visibility
        if (this.chartComponent?.instance) {
            const allSeries = this.chartComponent.instance.getAllSeries();
            const targetSeries = allSeries.find(s => s.name === legendItem.name);

            if (targetSeries) {
                if (legendItem.isHidden) {
                    targetSeries.hide();
                } else {
                    targetSeries.show();
                }
            }
        }
    }

    openLegendColorPicker(index: number, event: MouseEvent): void {
        event.stopPropagation();
        const colorPickersArray = this.colorPickers.toArray();
        if (colorPickersArray[index]) {
            colorPickersArray[index].nativeElement.click();
        }
    }

    onSeriesColorChange(legendItem: LineLegendType, color: string): void {
        legendItem.color = color;
        const axisName = legendItem.axisName;
        if (axisName) {
            const axis = this.lineChart.chartConfiguration.valueAxes.find((item) => item.name === axisName);
            if (axis) {
                axis.color = color;
            }
        }

        const parameters: WidgetParametersColumn[] = JSON.parse(this.trendWidgetConfiguration.parameters);

        const parameter = parameters.find(p => p.id === legendItem.parameterId);

        if (parameter) {
            const style = parameter.style ? JSON.parse(parameter.style) : {};
            style.color = color;
            parameter.style = JSON.stringify(style);

            this.trendWidgetConfiguration.parameters = JSON.stringify(parameters);

            this.currentParameters = parameters;

            this._trendWidgetConfigurationService.update(this.trendWidgetConfiguration);

            const sub = this._trendWidgetConfigurationServiceProxy
                .createOrEdit(this.trendWidgetConfiguration)
                .pipe(
                    tap(() => {
                        // After successful save, ensure cache is updated with latest data
                        this._trendWidgetConfigurationService.update(this.trendWidgetConfiguration);
                    })
                )
                .subscribe({
                    next: () => {
                    },
                    error: (error) => {
                    }
                });
            this.subs.push(sub);
        }

        setTimeout(() => {
            if (this.chartComponent?.instance) {
                this.chartComponent.instance.render();
            }
        }, 0);
    }

    onLegendTextClick(legendItem: LineLegendType, event: MouseEvent): void {
        event.stopPropagation();
        event.preventDefault();

        legendItem.isEditing = true;

        this.cdr.detectChanges();

        setTimeout(() => {
            const inputElement = this.elementRef.nativeElement.querySelector('.legend-text-input') as HTMLInputElement;
            if (inputElement) {
                inputElement.focus();
                inputElement.select();
            }
        }, 0);
    }

    onLegendNameChange(legendItem: LineLegendType, newName: string): void {
        legendItem.isEditing = false;

        if (!newName || newName.trim() === '') {
            return;
        }

        legendItem.customName = newName.trim();

        const parameters: WidgetParametersColumn[] = JSON.parse(this.trendWidgetConfiguration.parameters);
        const parameter = parameters.find(p => p.id === legendItem.parameterId);

        if (parameter) {
            const style = parameter.style ? JSON.parse(parameter.style) : {};
            style.customName = legendItem.customName;
            parameter.style = JSON.stringify(style);

            this.trendWidgetConfiguration.parameters = JSON.stringify(parameters);
            this.currentParameters = parameters;

            // Update cache and persist to database
            this._trendWidgetConfigurationService.update(this.trendWidgetConfiguration);

            const sub = this._trendWidgetConfigurationServiceProxy
                .createOrEdit(this.trendWidgetConfiguration)
                .pipe(
                    tap(() => {
                        this._trendWidgetConfigurationService.update(this.trendWidgetConfiguration);
                    })
                )
                .subscribe({
                    error: (error) => {
                    }
                });
            this.subs.push(sub);
        }
    }

    onLegendNameBlur(legendItem: LineLegendType, inputElement: HTMLInputElement): void {
        this.onLegendNameChange(legendItem, inputElement.value);
    }

    onLegendNameKeydown(legendItem: LineLegendType, event: KeyboardEvent, inputElement: HTMLInputElement): void {
        if (event.key === 'Enter') {
            event.preventDefault();
            this.onLegendNameChange(legendItem, inputElement.value);
        } else if (event.key === 'Escape') {
            event.preventDefault();
            legendItem.isEditing = false;
        }
    }

    onLegendItemHover(legendItem: LineLegendType, isHovering: boolean): void {
        // Don't show hover effect for hidden series
        if (legendItem.isHidden) {
            return;
        }

        if (!this.chartComponent?.instance) {
            return;
        }

        const chartInstance = this.chartComponent.instance;
        const allSeries = chartInstance.getAllSeries();

        if (isHovering) {
            // Find and hover the target series
            const targetSeries = allSeries.find(s => s.name === legendItem.name);

            if (targetSeries) {
                // Use DevExtreme's hover method
                targetSeries.hover();

                // Additionally manipulate the DOM for more control
                const chartElement = chartInstance.element();
                const svg = chartElement.querySelector('svg');

                if (svg) {
                    // Try multiple selectors to find series paths
                    let seriesPaths = svg.querySelectorAll('g[clip-path] path[stroke]');

                    if (seriesPaths.length === 0) {
                        seriesPaths = svg.querySelectorAll('path[stroke][fill="none"]');
                    }

                    if (seriesPaths.length === 0) {
                        seriesPaths = svg.querySelectorAll('path[stroke]');
                    }

                    // Filter to only line paths (exclude grid/axis)
                    const linePaths = Array.from(seriesPaths).filter((path: SVGPathElement) => {
                        const stroke = path.getAttribute('stroke');
                        const strokeWidth = path.getAttribute('stroke-width');
                        const fill = path.getAttribute('fill');

                        // Line paths typically have stroke, no fill or fill="none", and reasonable stroke-width
                        return stroke &&
                               stroke !== 'none' &&
                               (fill === 'none' || !fill) &&
                               (!strokeWidth || parseFloat(strokeWidth) >= 1);
                    });

                    linePaths.forEach((path: SVGPathElement, idx) => {
                        const series = allSeries[idx];

                        if (series && series.name === legendItem.name) {
                            path.style.opacity = '1';
                            path.style.strokeWidth = '4';
                            path.style.filter = 'drop-shadow(0 2px 4px rgba(0,0,0,0.3))';
                        } else {
                            path.style.opacity = '0.2';
                        }
                    });
                }
            }
        } else {
            // Clear hover from all series
            allSeries.forEach(series => {
                series.clearHover();
            });

            // Reset DOM styles
            const chartElement = chartInstance.element();
            const svg = chartElement.querySelector('svg');

            if (svg) {
                const allPaths = svg.querySelectorAll('path[stroke]');
                allPaths.forEach((path: SVGPathElement) => {
                    path.style.opacity = '';
                    path.style.strokeWidth = '';
                    path.style.filter = '';
                });
            }
        }
    }

    ngOnDestroy() {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.subs.forEach(sub => sub.unsubscribe());
    }

    private getSelectedIntervalResolution(
        unit: CustomResolutionUnits | undefined,
        isAutoResolution: boolean,
        value?: number,

    ): IntervalSynchronized {
        if (isAutoResolution) {
            return IntervalSynchronized.ISX;
        }

        if (!unit) {
            return IntervalSynchronized.IS1SEC;
        }

        switch (unit) {
            case CustomResolutionUnits.MS:
                return IntervalSynchronized.IS200MS;
            case CustomResolutionUnits.SEC:
                return IntervalSynchronized.IS1SEC;
            case CustomResolutionUnits.MIN:
                return IntervalSynchronized.IS1MIN;
            case CustomResolutionUnits.HOUR:
                return IntervalSynchronized.IS1HOUR;
            case CustomResolutionUnits.DAY:
                return IntervalSynchronized.IS1DAY;
            case CustomResolutionUnits.WEEK:
                return IntervalSynchronized.IS1WEEK;
            case CustomResolutionUnits.MONTH:
                return IntervalSynchronized.IS1MONTH;
            case CustomResolutionUnits.YEAR:
                return IntervalSynchronized.IS1YEAR;
            default:
                return IntervalSynchronized.IS1SEC;
        }
    }

    /**
     * Extracts group and phase from parameter, parsing data if needed
     * @returns Object with group and phase, or null if extraction fails
     */
    private extractParameterGroupAndPhase(parameter: WidgetParametersColumn): { group: string; phase: string } | null {
        let group: string | undefined;
        let phase: string | undefined;

        if (parameter.type === ColumnType.BaseParameter) {
            try {
                const parsed = JSON.parse(parameter.data as string);
                const baseData = BaseData.fromJS?.(parsed) ?? parsed;
                group = baseData.group;
                phase = baseData.phase;
            } catch (e) {
                console.warn('Failed to parse BaseParameter data:', e);
                return null;
            }
        } else {
            // For other parameter types, use direct properties
            group = parameter.group;
            phase = parameter.phase;
        }

        if (!group || !phase) {
            return null;
        }

        return { group, phase };
    }

    private buildParameterColorMap(): void {
        this.parameterColorMap = new Map<string, ParameterColorModel>();

        if (!this.currentParameters || this.currentParameters.length === 0) {
            return;
        }
        
        this.currentParameters.forEach((parameter) => {
            const parameterName = parameter.name;

            if (!parameterName) {
                return;
            }

            let color: string | undefined;

            // Try to find color in advanced parameters
            const advancedColor = this.findAdvancedColorForParameter(parameter);
            if (advancedColor) {
                color = advancedColor;
            } else {
                // Fallback to default phase colors
                const groupPhase = this.extractParameterGroupAndPhase(parameter);
                if (groupPhase && this.defaultColors) {
                    let phaseColor = null;
                    
                    switch(groupPhase.phase) {
                        case 'UV1N': {
                            phaseColor = this.defaultColors['v1n'];
                            break;
                        }
                        case 'UV2N': {
                            phaseColor = this.defaultColors['v2n'];
                            break;
                        }
                        case 'UV3N': {
                            phaseColor = this.defaultColors['v3n'];
                            break;
                        }
                        case 'UV12': {
                            phaseColor = this.defaultColors['v12'];
                            break;
                        }
                        case 'UV23': {
                            phaseColor = this.defaultColors['v23'];
                            break;
                        }
                        case 'UV31': {
                            phaseColor = this.defaultColors['v31'];
                            break;
                        }
                        case 'UVN': {
                            phaseColor = this.defaultColors['vn'];
                            break;
                        }
                        case 'UP123': {
                            phaseColor = this.defaultColors['total'];
                            break;
                        }
                        case 'IAUX': {
                            phaseColor = this.defaultColors['aux'];
                            break;
                        }
                        case 'UFREQUENCY': {
                            phaseColor = this.defaultColors['frequency'];
                            break;
                        }
                        default: null;
                    }

                    if (phaseColor) {
                        color = phaseColor;
                    }
                }
            }

            this.parameterColorMap.set(parameterName, {
                color: color,
            });
        });
    }

    private findAdvancedColorForParameter(parameter: WidgetParametersColumn): string | null {
        if (!this.advancedParameterColors || this.advancedParameterColors.length === 0) {
            return null;
        }

        const groupPhase = this.extractParameterGroupAndPhase(parameter);
        if (!groupPhase) {
            return null;
        }

        const quantity = parameter.quantity;

        const match = this.advancedParameterColors.find((ap: any) => {
            const sameGroup = ap.group === groupPhase.group;
            const samePhase = ap.phaseOrChannel === groupPhase.phase;
            const sameQuantity = ap.quantity === String(quantity);

            return sameGroup && samePhase && sameQuantity;
        });

        return match?.color ?? null;
    }

    private saveParameterColors(): void {
        if (!this.trendWidgetConfiguration || !this.currentParameters) {
            return;
        }

        // Update the configuration with current parameters (which now have colors)
        this.trendWidgetConfiguration.parameters = JSON.stringify(this.currentParameters);

        // Persist to database without triggering a refresh
        this._trendWidgetConfigurationService.update(this.trendWidgetConfiguration);
    }

    save(newConfig: CreateOrEditTrendWidgetConfigurationDto) {
        this.stopStream$.next(null);
        this.stopStream$.complete();

        // Update the configuration with current parameter colors from the chart
        if (this.currentParameters && this.currentParameters.length > 0) {
            newConfig.parameters = JSON.stringify(this.currentParameters);
        }

        if (newConfig.id.toString() !== this.widgetConfigurationInDB?.configuration) {
            this.saveConfiguration(newConfig.id.toString());
        } else {
            // Save the updated configuration to persist colors
            this._trendWidgetConfigurationService.update(newConfig);
            this.refreshWidget();
        }
    }

    toggleStepLine() {
        // Angular change detection will automatically update the chart
    }

    toggleLinePoints() {
        // Angular change detection will automatically update the chart
    }

    openColorPicker(picker: HTMLInputElement) {
        picker?.click();
    }


    onTitleClick(event: MouseEvent, picker: HTMLInputElement) {
        event.stopPropagation();
        this.openColorPicker(picker);
    }

    onHeaderColorChange(event: Event) {
        const color = (event.target as HTMLInputElement).value;
        this.applyHeaderColor(color);
    }

    onLineColorChange(event: Event) {
        const color = (event.target as HTMLInputElement).value;
        this.chartLineColor = color;
        this.lineChart.chartConfiguration.color = color;
        this.persistColorPreference('chartLineColor', color);
    }

    onBackgroundColorChange(event: Event) {
        const color = (event.target as HTMLInputElement).value;
        this.chartBackgroundColor = color;
        this.persistColorPreference('chartBackgroundColor', color);
    }

    private loadColorPreferences() {
        const header = abp.utils.getCookieValue(this.getColorCookieKey('headerBackgroundColor'));
        const widget = abp.utils.getCookieValue(this.getColorCookieKey('widgetBackgroundColor'));
        const line = abp.utils.getCookieValue(this.getColorCookieKey('chartLineColor'));
        const background = abp.utils.getCookieValue(this.getColorCookieKey('chartBackgroundColor'));

        if (header) {
            this.headerBackgroundColor = header;
        }

        if (widget) {
            this.widgetBackgroundColor = widget;
        } else {
            this.widgetBackgroundColor = this.headerBackgroundColor;
            this.persistColorPreference('widgetBackgroundColor', this.widgetBackgroundColor);
        }

        if (line) {
            this.chartLineColor = line;
        }
        this.lineChart.chartConfiguration.color = this.chartLineColor;
        if (background) {
            this.chartBackgroundColor = background;
        }

        this.updateHeaderTitleStyling();
    }

    private getColorCookieKey(suffix: string): string {
        const guid = this.elementRef.nativeElement.parentElement?.dataset?.guid ?? 'trend';
        return `trend_${guid}_${suffix}`;
    }

    private persistColorPreference(suffix: string, value: string) {
        abp.utils.setCookieValue(this.getColorCookieKey(suffix), value, this.getCookieExpiration());
    }

    private getCookieExpiration(): Date {
        const expireDate = new Date();
        expireDate.setFullYear(expireDate.getFullYear() + 1);
        return expireDate;
    }

    private applyHeaderColor(color: string) {
        this.headerBackgroundColor = color;
        this.persistColorPreference('headerBackgroundColor', color);
        this.updateHeaderTitleStyling();
    }

    private updateHeaderTitleStyling() {
        this.headerTitleColor = this.getContrastingTextColor(this.headerBackgroundColor);
        this.headerTitleBadgeColor = this.getBadgeColor(this.headerBackgroundColor);
    }

    private getContrastingTextColor(color: string): string {
        const { r, g, b } = this.hexToRgb(color);
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.6 ? '#1b1b1b' : '#ffffff';
    }

    private getBadgeColor(color: string): string {
        const { r, g, b } = this.hexToRgb(color);
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.6 ? 'rgba(0, 0, 0, 0.08)' : 'rgba(255, 255, 255, 0.18)';
    }

    private hexToRgb(hex: string): { r: number; g: number; b: number } {
        if (!hex) {
            return { r: 255, g: 255, b: 255 };
        }

        let normalized = hex.replace('#', '');

        if (normalized.length === 3) {
            normalized = normalized.split('').map((char) => char + char).join('');
        }

        const parsed = parseInt(normalized, 16);

        if (isNaN(parsed)) {
            return { r: 255, g: 255, b: 255 };
        }

        return {
            r: (parsed >> 16) & 255,
            g: (parsed >> 8) & 255,
            b: parsed & 255,
        };
    }
}
