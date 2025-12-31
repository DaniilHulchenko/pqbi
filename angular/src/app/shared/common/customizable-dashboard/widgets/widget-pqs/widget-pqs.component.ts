import {
    Component,
    OnInit,
    Injector,
    ViewChild,
    ElementRef,
    OnDestroy,
    AfterViewInit,
    Output,
    EventEmitter,
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
} from '@shared/service-proxies/service-proxies';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { TreeDragDropService } from 'primeng/api';
import { Subject, Subscription, catchError, takeUntil, throwError, timer } from 'rxjs';
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


type LineLegendType = {
    name: string,
    id: string
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
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
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

    init222(trend: TrendResponse) {
        const data = trend.data;
        this.chartData = [];
        this.chartConfiguration.lineLegend = [];

        let map: Map<number, object> = new Map(); // number is datetime representation in UNIX sec

        for (const dataItem of data) {
            let legendLabel = dataItem.parameterName || this.parameterName222(dataItem);
            let dataId = Guid.newGuid().toString();

            this.chartConfiguration.lineLegend.push({
                name: legendLabel,
                id: dataId,
            });

            for (let i = 0; i < dataItem.data.length; i++) {
                if (map.has(trend.timeStamps[i])) {
                    let obj = map.get(trend.timeStamps[i]);
                    obj[dataId] = dataItem.data[i];
                }else {
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

        const feedersJoined = parameter.feeders.map(f => {
            const parts = [

                f.name ? ` ${f.name}` : f.componentId,
                f.id !== undefined ? `${f.id}` : '',
            ];
            return parts.join('').trim();
        }).join(',\n');

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

    reload(input: TrendCalcRequest) {
        this.showLoading();

        var sub = this._dashboardService.pQSTrendData(input)
            .pipe(
                catchError((error) => {
                    this.hideLoading();
                    return throwError(() => error);
                }),
            )
            .subscribe(result => {
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
                    this.init222(result);
                    this.updateLineColorFromChart();
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
    @ViewChild('headerColorPicker') headerColorPicker: ElementRef<HTMLInputElement>;
    @Output() widgetRefresh: EventEmitter<any> = new EventEmitter();

    errorMessage: string | null = null;

    lineChart: LineChart;

    isStepLine = false;
    isLinePoints = true;
    stopStream$ = new Subject();
    isActive = false;

    allComponents: any[];
    trendWidgetConfiguration: CreateOrEditTrendWidgetConfigurationDto;
    chartWidth: number;

    lineColor?: string;
    backgroundColor?: string;
    detectedLineColor?: string;
    readonly defaultLinePickerColor = '#0000ff';
    readonly defaultBackgroundColor = '#ffffff';
    isLineColorEnabled = false;
    isBackgroundColorEnabled = false;

    headerBackgroundColor?: string;
    chartHeight: number = 300; // default value

    protected _defaultWidgetName;
    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        private _tenantdashboardService: TenantDashboardServiceProxy,
        private _trendWidgetConfigurationService: TrendWidgetConfigurationService,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
        public elementRef: ElementRef,
        dateRangeService: DateRangeService,
        private _resolutionService: ResolutionService,
        private _configurationVersionService: ConfigurationVersionService,
        private _dateTimeService: DateTimeService,
    ) {
        super(injector, elementRef, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSTrend');
        this.lineChart = new LineChart(this._tenantdashboardService, this._pqsRestApiServiceProxy, (error) => {
            this.errorMessage = error;
        });
        this.lineChart.isInitialLoad = true;
    }

    ngOnInit() {
        super.ngOnInit();
        this.headerBackgroundColor = this.loadHeaderColorFromCookie();
        this.loadColorsFromCookie();
    }

    ngAfterViewInit() {
        this.runDelayed(() => {
            if (this.isNew) {
                this.onEditRequested(null);
            }
            this.chartWidth = this.chartComponent.instance.element().clientWidth;
        });
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
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this._trendWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.trendWidgetConfiguration = result.trendWidgetConfiguration;
                    this.loadColorsFromCookie();
                    this.headerBackgroundColor = this.loadHeaderColorFromCookie();
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

                        if (dateModel.rangeUnit === DateRangeType.Relative && resulutionValueInMs) {
                            timer(0, resulutionValueInMs)
                                .pipe(takeUntil(this.stopStream$))
                                .subscribe((result) => {
                                    let input = this.formatRequest();
                                    input.isRealTime = true;
                                    input.refreshRateInSeconds = resulutionValueInMs / 1000;
                                    if (input) {
                                        this.lineChart.reload(input);
                                    }
                                });
                        } else {
                            let input = this.formatRequest();
                            if (input) {
                                this.lineChart.reload(input);
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

     const normalizedValue = Number(value);

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
        this.isStepLine = !this.isStepLine;

        setTimeout(() => {
            this.isStepLine = !this.isStepLine;
        }, 0);
    }

    toggleLinePoints() {
        this.isStepLine = !this.isStepLine;

        setTimeout(() => {
            this.isStepLine = !this.isStepLine;
        }, 0);
    }

    onLineColorChange(color: string) {
        this.lineColor = color;
        this.isLineColorEnabled = true;
        this.saveColorsToCookie();
        this.updateDetectedLineColor(color);
    }

    onBackgroundColorChange(color: string) {
        this.backgroundColor = color;
        this.isBackgroundColorEnabled = true;
        this.saveColorsToCookie();
    }

    onLineColorToggle(enabled: boolean) {
        this.isLineColorEnabled = enabled;
        if (!enabled) {
            this.lineColor = undefined;
        } else if (!this.lineColor) {
            this.lineColor = this.detectedLineColor ?? this.defaultLinePickerColor;
        }
        this.saveColorsToCookie();
        this.updateDetectedLineColor(this.lineColor);
    }

    onBackgroundColorToggle(enabled: boolean) {
        this.isBackgroundColorEnabled = enabled;
        if (!enabled) {
            this.backgroundColor = undefined;
        } else if (!this.backgroundColor) {
            this.backgroundColor = this.defaultBackgroundColor;
        }
        this.saveColorsToCookie();
    }

    get lineLabelColor(): string {
        return this.lineColor ?? this.detectedLineColor ?? this.defaultLinePickerColor;
    }

    onChartRendered() {
        this.updateLineColorFromChart();
    }

    private updateLineColorFromChart(): void {
        if (this.lineColor) {
            this.updateDetectedLineColor(this.lineColor);
            return;
        }

        const series = this.chartComponent?.instance?.getAllSeries?.();
        if (series && series.length) {
            const seriesColor = series[0]?.getColor?.();
            this.updateDetectedLineColor(seriesColor);
        } else {
            this.updateDetectedLineColor();
        }
    }

    private updateDetectedLineColor(color?: string): void {
        this.detectedLineColor = color ?? this.detectedLineColor ?? this.defaultLinePickerColor;
    }

    private loadColorsFromCookie(): void {
        const key = this.getColorsCookieKey();
        if (!key) {
            this.isLineColorEnabled = false;
            this.isBackgroundColorEnabled = false;
            this.lineColor = undefined;
            this.backgroundColor = undefined;
            this.updateDetectedLineColor();
            return;
        }

        const cookies = document.cookie?.split(';').map(c => c.trim()) ?? [];
        const cookie = cookies.find(c => c.startsWith(`${key}=`));
        if (!cookie) {
            this.isLineColorEnabled = false;
            this.isBackgroundColorEnabled = false;
            this.lineColor = undefined;
            this.backgroundColor = undefined;
            this.updateDetectedLineColor();
            return;
        }

        try {
            const decoded = decodeURIComponent(cookie.substring(key.length + 1));
            const parsed = JSON.parse(decoded);
            this.isLineColorEnabled = !!parsed?.isLineEnabled;
            this.lineColor = parsed?.lineColor ?? undefined;
            this.isBackgroundColorEnabled = !!parsed?.isBackgroundEnabled;
            this.backgroundColor = parsed?.backgroundColor ?? undefined;
        } catch {
            this.isLineColorEnabled = false;
            this.isBackgroundColorEnabled = false;
            this.lineColor = undefined;
            this.backgroundColor = undefined;
        }

        this.updateDetectedLineColor(this.lineColor);
    }

    private saveColorsToCookie(): void {
        const key = this.getColorsCookieKey();
        if (!key) {
            return;
        }

        const payload = {
            isLineEnabled: this.isLineColorEnabled,
            lineColor: this.isLineColorEnabled ? this.lineColor : undefined,
            isBackgroundEnabled: this.isBackgroundColorEnabled,
            backgroundColor: this.isBackgroundColorEnabled ? this.backgroundColor : undefined,
        };

        const maxAge = 60 * 60 * 24 * 365; // 1 year
        document.cookie = `${key}=${encodeURIComponent(JSON.stringify(payload))};path=/;max-age=${maxAge}`;
    }

    private getColorsCookieKey(): string | null {
        return this.widgetConfigurationInDB?.widgetGuid
            ? `trend_colors_${this.widgetConfigurationInDB.widgetGuid}`
            : null;
    }

    onHeaderClick(): void {
        if (!this.editState || !this.headerColorPicker) {
            return;
        }

        this.headerColorPicker.nativeElement.click();
    }

    onHeaderColorChange(color: string): void {
        this.headerBackgroundColor = color;
        this.saveHeaderColorToCookie(color);
    }

    private getHeaderColorCookieKey(): string | null {
        return this.widgetConfigurationInDB?.widgetGuid
            ? `trend_header_color_${this.widgetConfigurationInDB.widgetGuid}`
            : null;
    }

    private loadHeaderColorFromCookie(): string | undefined {
        const key = this.getHeaderColorCookieKey();
        if (!key) {
            return undefined;
        }

        const cookies = document.cookie?.split(';').map(c => c.trim()) ?? [];
        for (const cookie of cookies) {
            if (cookie.startsWith(`${key}=`)) {
                const value = cookie.substring(key.length + 1);
                return decodeURIComponent(value);
            }
        }
        return undefined;
    }

    private saveHeaderColorToCookie(color: string): void {
        const key = this.getHeaderColorCookieKey();
        if (!key) {
            return;
        }

        const maxAge = 60 * 60 * 24 * 365; // 1 year
        document.cookie = `${key}=${encodeURIComponent(color)};path=/;max-age=${maxAge}`;
    }
}
