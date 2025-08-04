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
    ComponentDto,
    CreateOrEditTrendWidgetConfigurationDto,
    FeederComponentInfo,
    FeederDescriptionDto,
    TrendParameter,
    PQSRestApiServiceProxy,
    TenantDashboardServiceProxy,
    TrendCalcRequest,
    TrendWidgetConfigurationsServiceProxy,
    TrendCustomWidgetData,
    BaseData,
    EmailSettingsEditDto,
    CalculatedDataItem,
    TrendResponse,
} from '@shared/service-proxies/service-proxies';
import { DashboardChartBase } from '../dashboard-chart-base';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { TreeDragDropService } from 'primeng/api';
import { Subject, catchError, forkJoin, takeUntil, throwError, timer } from 'rxjs';
import { CreateOrEditTrendConfigurationComponent } from './create-or-edit-trend-configuration/create-or-edit-trend-configuration.component';
import { Guid } from 'guid-ts';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ResolutionService } from '@app/shared/services/resolution-service';
import { DxChartComponent, DxTextBoxComponent } from 'devextreme-angular';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DateTime } from 'luxon';
import { ResolutionUnits } from '@app/shared/enums/resolution-selection-units';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { ColumnType } from '@app/shared/enums/column-type';

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
        return {
            text: `${object.seriesName}<br/>${object.originalValue}`,
        };
    }

    reload(input: TrendCalcRequest) {
        this.showLoading();

        this._dashboardService.pQSTrendData(input)
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
                }
                this.hideLoading();
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
    @Output() widgetRefresh: EventEmitter<any> = new EventEmitter();

    errorMessage: string | null = null;

    lineChart: LineChart;

    isStepLine = false;
    stopStream$ = new Subject();
    isActive = false;

    allComponents: any[];
    trendWidgetConfiguration: CreateOrEditTrendWidgetConfigurationDto;
    chartWidth: number;

    protected _defaultWidgetName;

    constructor(
        injector: Injector,
        private _tenantdashboardService: TenantDashboardServiceProxy,
        private _trendWidgetConfigurationService: TrendWidgetConfigurationsServiceProxy,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
        elementReference: ElementRef,
        dateRangeService: DateRangeService,
        private _resolutionService: ResolutionService,
    ) {
        super(injector, elementReference, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSTrend');
        this.lineChart = new LineChart(this._tenantdashboardService, this._pqsRestApiServiceProxy, (error) => {
            this.errorMessage = error;
        });
    }

    ngOnInit() {
        super.ngOnInit();
    }

    ngAfterViewInit() {
        this.runDelayed(() => {
            if (this.isNew) {
                this.edit();
            }
            this.chartWidth = this.chartComponent.instance.element().clientWidth;
        });
    }

    edit() {
        this.createOrEditModal.show(this.widgetConfigurationInDB);
    }

    onNameEdit() {
        this.renameModal.show(this.widgetName);
    }

    refreshWidget(): void {
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            this._trendWidgetConfigurationService
                .getTrendWidgetConfigurationForView(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.trendWidgetConfiguration = result.trendWidgetConfiguration;
                    let rangeOption = JSON.parse(this.trendWidgetConfiguration.dateRange).rangeOption;
                    if (this.trendWidgetConfiguration) {
                        let isAutoResolution = this.trendWidgetConfiguration.resolution === ResolutionUnits.AUTO;
                        let resulutionValueInMs = 0;

                        if (isAutoResolution && (rangeOption.startsWith('Last') || rangeOption.startsWith('This') || rangeOption === 'Today')) {
                            resulutionValueInMs = this.calculateAutoResolution().toRefresh;
                        } else {
                            resulutionValueInMs = this._resolutionService.resolutionValueInMs(
                                this._resolutionService.parseStateFromString(
                                    this.trendWidgetConfiguration.resolution,
                                    true,
                                ),
                            );
                        }
                        if (resulutionValueInMs) {
                            timer(0, resulutionValueInMs)
                                .pipe(takeUntil(this.stopStream$))
                                .subscribe((result) => {
                                    /*let range: [DateTime, DateTime] = this._dateRangeService.getDateRangeFromState(
                                    this.trendWidgetConfiguration.dateRange,
                                );
                                this.trendWidgetConfiguration.dateR

                                this.pqsTrendConfigRequest.startDate = range[0];
                                this.pqsTableConfigRequest.endDate = range[1];*/
                                    let input = this.formatRequest();
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
        }
    }

    calculateAutoResolution(): { toServer: number; toRefresh: number } {
        /** TBD **/
        if (!this.trendWidgetConfiguration.dateRange) {
            throw new Error('No dateRange for calculating AUTO resolution');
        }
        let { startDate, endDate, rangeOption } = JSON.parse(this.trendWidgetConfiguration.dateRange);
        let dateRange: [DateTime, DateTime];
        let dateRangeInMs: number;
        if (rangeOption !== DateRangeUnits.CUSTOM) {
            dateRange = this._dateRangeService.getDateRangeFromUnit(rangeOption);
        } else if (startDate && endDate) {
            dateRange = [DateTime.fromISO(startDate), DateTime.fromISO(endDate)];
        }
        dateRangeInMs = dateRange[1].toMillis() - dateRange[0].toMillis();

        this.chartWidth = this.chartComponent?.instance.element().clientWidth ?? this.chartWidth;
        return { toServer: Math.floor(this.chartWidth * 0.75), toRefresh: Math.floor((dateRangeInMs / this.chartWidth) * 0.75) };
    }

    formatRequest(): TrendCalcRequest| null {
        try {
            let dateRange = this._dateRangeService.getDateRangeFromState(
                DateRangeState.fromJSON(this.trendWidgetConfiguration.dateRange),
            );

            const state = this._resolutionService.parseStateFromString(
                this.trendWidgetConfiguration.resolution,
                true,
            );

            let resolutionInSeconds = 0;
            const isAutoResolution = state.isAuto;

            if (isAutoResolution) {
                resolutionInSeconds = this.calculateAutoResolution().toServer;
            }
            else {
                resolutionInSeconds = this._resolutionService.resolutionValueInSeconds(state);
            }


            let parameters: WidgetParametersColumn[] = JSON.parse(this.trendWidgetConfiguration.parameters);
            let request: TrendCalcRequest = new TrendCalcRequest({
                isAutoResolution,
                resolutionInSeconds,
                userTimeZone: 1,
                widgetName: this.widgetName,
                startDate: dateRange[0],
                endDate: dateRange[1],
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
                                quantity: parameter.quantity
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
                                quantity: parameter.quantity
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
                        feeders: parameter.componentsState?.feeders?.map(
                            (feeder) => {
                                return new FeederComponentInfo({id: feeder.id, componentId: feeder.componentId, name: feeder.name, compName: parameter.componentsState?.components?.find(c => c.key === feeder.componentId)?.label});
                            }
                        ),
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
    }

    save(configuration: CreateOrEditTrendWidgetConfigurationDto) {
        this.saveConfiguration(configuration.id.toString());
    }

    toggleStepLine() {
        this.isStepLine = !this.isStepLine;

        setTimeout(() => {
            this.isStepLine = !this.isStepLine;
        }, 0);
    }
}
