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


type LineLegendType = {
    name: string,
    id: string,
    color?: string,
    parameterId?: string,
    isEditing?: boolean,
    customName?: string
};

class LineChartConfiguration {
    lineLegend: LineLegendType[];
    color: string;
    overlappingMode: string;
}

// class LineChartConfiguration {
//     lineLegend: any[];
//     color: string;
//     overlappingMode: string;
// }

//#region LineChart
class LineChart extends DashboardChartBase {
    chartData: any[];
    errorMessage: string | null = null;
    //#region DevExtreme Chart
    chartConfiguration: LineChartConfiguration = {
        lineLegend: [],
        color: 'green',
        overlappingMode: 'hide',
    };
    //#endregion
    constructor(
        private _dashboardService: TenantDashboardServiceProxy,
        private setErrorMessage: (error: string | null) => void,
    ) {
        super();
    }

    // init(data: GraphParametersComponentDtoV3[]) {
    //     this.chartData = [];
    //     this.chartConfiguration.lineLegend = [];
    //     let map: Map<number, object> = new Map(); // number is datetime representation in UNIX sec
    //     for (let compData of data) {
    //         let legendLabel = this.parameterName(compData);
    //         let dataId = Guid.newGuid().toString();
    //         this.chartConfiguration.lineLegend.push({
    //             name: legendLabel,
    //             id: dataId,
    //         });

    //         compData.data?.forEach((axisValue: AxisValue) => {
    //             let obj = new Object();
    //             if (map.has(axisValue.timeStempInSeconds)) {
    //                 obj = map.get(axisValue.timeStempInSeconds);
    //             }
    //             obj[dataId] = axisValue.value;
    //             map.set(axisValue.timeStempInSeconds, obj);
    //         });
    //     }

    //     let arr = Array.from(map, ([key, value]) => {
    //         return {
    //             key: DateTime.fromSeconds(key).toJSDate(),
    //             ...value,
    //         };
    //     });

    //     this.chartData = arr;
    // }

    init222(trend: TrendResponse, parameters?: WidgetParametersColumn[]) {
        const data = trend.data;
        this.chartData = [];
        this.chartConfiguration.lineLegend = [];

        let map: Map<number, object> = new Map(); // number is datetime representation in UNIX sec

        for (let i = 0; i < data.length; i++) {
            const dataItem = data[i];
            let legendLabel = dataItem.parameterName || this.parameterName222(dataItem);
            let dataId = Guid.newGuid().toString();
            const parameter = parameters?.[i];
            const styleData = parameter?.style ? JSON.parse(parameter.style) : {};
            const color = styleData?.color;
            const customName = styleData?.customName;

            this.chartConfiguration.lineLegend.push({
                name: legendLabel,
                id: dataId,
                color: color,
                parameterId: parameter?.id,
                customName: customName,
                isEditing: false
            });

            for (let i = 0; i < dataItem.data.length; i++) {
                if (map.has(trend.timeStamps[i])) {
                    let obj = map.get(trend.timeStamps[i]);
                    obj[dataId] = dataItem.data[i];
                } else {
                    let obj = new Object();
                    obj[dataId] = dataItem.data[i];
                    map.set(trend.timeStamps[i], obj);
                }
            }
        }

        let arr = Array.from(map, ([key, value]) => {
            return {
                key: DateTime.fromSeconds(key).toJSDate(),
                ...value,
            };
        });

        this.chartData = arr;
        this.isInitialLoad = false;
    }

    parameterName222(parameter: CalculatedDataItem): string {
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

    // parameterName(parameter: GraphParametersComponentDtoV3): string {
    //     let result = parameter.customParameterName || parameter.parameterNames.join(',\n');
    //     const feeders = parameter.feeders;
    //     var firstFeeder = feeders[0];
    //     if (firstFeeder.componentId) {
    //         let component = this._components?.find((c) => c.componentId === firstFeeder.componentId);
    //         let feeder: FeederDescriptionDto;
    //         if (component && firstFeeder.id) {
    //             feeder = component.feeders?.find((f) => f.id === firstFeeder.id);
    //         }
    //         result = (component?.componentName ?? '') + ' ' + (feeder?.name ?? '') + ' ' + result;
    //     }
    //     return result;
    // }

    customizeTooltip(object: any) {
        const parsed = parseFloat(object.originalValue);
        const res = isNaN(parsed) ? object.originalValue : parsed.toFixed(2);
        return {
            text: `${object.seriesName}<br/>${res}`,
        };
    }

    reload(input: TrendCalcRequest, parameters?: WidgetParametersColumn[]) {
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
                    // this.errorMessage = result2.reason || 'No Data Available';
                    this.setErrorMessage(this.errorMessage);
                } else if (!result.data || result.data.length === 0) {
                    this.errorMessage = 'No Data for Selected Values';
                    this.setErrorMessage(this.errorMessage);
                } else {
                    this.errorMessage = null;
                    this.setErrorMessage(null);
                    // this.init(result2.data);
                    this.init222(result, parameters);
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

    chartHeight: number = 300; // default value
    headerBackgroundColor = '#ffffff';
    widgetBackgroundColor = '#ffffff';
    chartLineColor = '#3699ff';
    chartBackgroundColor = '#ffffff';
    headerTitleColor = '#000000';
    headerTitleBadgeColor = 'rgba(0, 0, 0, 0.08)';

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
        this.lineChart = new LineChart(this._tenantdashboardService, (error) => {
            this.errorMessage = error;
        });
        this.lineChart.isInitialLoad = true;
    }

    ngOnInit() {
        this.loadColorPreferences();
        super.ngOnInit();
    }

    ngAfterViewInit() {
        this.runDelayed(() => {
            if (this.isNew) {
                this.onEditRequested(null);
            }
            this.chartWidth = this.chartComponent.instance.element().clientWidth;
        });
    }

    customizePoint = (pointInfo: any) => {
        if (this.thresholdSettings.deviationColor && (pointInfo.value > this.thresholdSettings.upperValue
            || pointInfo.value < this.thresholdSettings.lowerValue)) {
            return { color: this.thresholdSettings.deviationColor, size: 7 };
        }
    };

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
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this._trendWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.trendWidgetConfiguration = result.trendWidgetConfiguration;

                    let rangeOption = JSON.parse(this.trendWidgetConfiguration.dateRange).rangeOption;
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
                        this.thresholdSettings = this.trendWidgetConfiguration.thresholdSettings ? JSON.parse(this.trendWidgetConfiguration.thresholdSettings) : new ThresholdSettingsModel();

                        this.currentParameters = JSON.parse(this.trendWidgetConfiguration.parameters);

                        if (dateModel.rangeUnit === DateRangeType.Relative && resulutionValueInMs) {
                            timer(0, resulutionValueInMs)
                                .pipe(takeUntil(this.stopStream$))
                                .subscribe((result) => {
                                    let input = this.formatRequest();
                                    input.isRealTime = true;
                                    input.refreshRateInSeconds = resulutionValueInMs / 1000;
                                    if (input) {
                                        this.lineChart.reload(input, this.currentParameters);
                                    }
                                });
                        } else {
                            let input = this.formatRequest();
                            if (input) {
                                this.lineChart.reload(input, this.currentParameters);
                            }
                        }
                    }
                });
            this.subs.push(sub);
        }
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

    openLegendColorPicker(index: number): void {
        const colorPickersArray = this.colorPickers.toArray();
        if (colorPickersArray[index]) {
            colorPickersArray[index].nativeElement.click();
        }
    }

    onSeriesColorChange(legendItem: LineLegendType, color: string): void {
        legendItem.color = color;

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
                        console.error('onSeriesColorChange - Error saving color configuration:', error);
                    }
                });
            this.subs.push(sub);
        } else {
            console.warn('onSeriesColorChange - Parameter not found for legendItem:', legendItem);
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
            } else {
                console.error('Input element not found after change detection');
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
                        console.error('Error saving legend name:', error);
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

    save(newConfig: CreateOrEditTrendWidgetConfigurationDto) {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        
        if (newConfig.id.toString() !== this.widgetConfigurationInDB?.configuration) {
            this.saveConfiguration(newConfig.id.toString());
        } else {
            this.refreshWidget();
        }
    }

    toggleStepLine() {
        this.refreshChartAppearance();
    }

    toggleLinePoints() {
        this.refreshChartAppearance();
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
        this.persistColorPreference('chartLineColor', color);
        this.refreshChartAppearance();
    }

    onBackgroundColorChange(event: Event) {
        const color = (event.target as HTMLInputElement).value;
        this.chartBackgroundColor = color;
        this.persistColorPreference('chartBackgroundColor', color);
        this.refreshChartAppearance();
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

    private refreshChartAppearance() {
        if (this.chartComponent?.instance) {
            this.chartComponent.instance.refresh();
        }
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
