import { Component, ElementRef, Injector, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import {
    ColumnWidgetTable,
    CreateOrEditWidgetConfigurationDto,
    CustomWidgetTableData,
    DataUnitType,
    EventClass,
    FeederComponentInfo,
    GaugeMarkerDto,
    GaugeWidgetConfigurationDto,
    MarkerKind,
    PQBIQuantityType,
    RowWidgetTable,
    TableWidgetEvent,
    TableWidgetRequest,
    TableWidgetResponse,
    TableWidgetResponseItem,
    TenantDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import { CreateOrEditGaugeConfigurationComponent } from './create-or-edit-gauge-configuration/create-or-edit-gauge-configuration.component';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { Subject, Subscription, takeUntil, timer } from 'rxjs';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DateTime } from 'luxon';
import { ColorSchema, ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { ColumnType } from '@app/shared/enums/column-type';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import safeStringify from 'fast-safe-stringify';
import { GaugeStyle, GaugeStyleArcAngleEnum, GaugeStyleEnum } from '@app/shared/interfaces/gauge-style';
import { GaugeWidgetAdvancedSettingsConfig, Segment } from '@app/shared/interfaces/gauge-widget-advanced-settings-config';
import { ChartsColor } from 'devextreme/common/charts';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { GaugeWidgetConfigurationService } from '@app/shared/services/widget-configurations/gauge-widget-configuration.service';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';



interface WeightedSegmentMeta extends Segment {
    startPosition: number;
    endPosition: number;
    weight: number;
}


interface WeightedSegmentMeta extends Segment {
    startPosition: number;
    endPosition: number;
    weight: number;
}


@Component({
    selector: 'app-widget-pqs-gauge',
    templateUrl: './widget-pqs-gauge.component.html',
    styleUrl: './widget-pqs-gauge.component.css',
})
export class WidgetPqsGaugeComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy {
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditGaugeConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;

    gaugeWidgetConfiguration: GaugeWidgetConfigurationDto;
    gaugeWidgetRequest: TableWidgetRequest;

    parameter: WidgetParametersColumn;
    style: GaugeStyle;

    lowerLimit: number;
    upperLimit: number;

    ranges?: {
        color?: ChartsColor | string;
        endValue?: number;
        startValue?: number;
    }[];

    subvalues?: number[];
    actualSubvalues?: number[];
    scaleCustomTicks?: number[];
    scaleLabelCustomizeText = (info: { value: number; valueText: string }) => this.getScaleLabelText(info);

    formattedValue: number;
    actualValue: number;
    displayValue: number;    unit = '';
    title = '';
    valueFontSize = 20;
    valueFontFamily = '';
    valueFontColor = '#000';
    titleFontSize = 20;
    titleFontFamily = '';
    titleFontColor = '#000';
    widgetNameFontSize?: string;

    calculatedColorSchema: string | null;
   
    navigationPageId: string | null = null;
    canNavigateToPage = false;

    private dataUnitType: DataUnitType;
    private stopStream$ = new Subject();

    private subs: Subscription[] = [];
    private weightedSegments: WeightedSegmentMeta[] = [];
    private hasWeightedScale = false;
    private actualLowerLimit: number;
    private actualUpperLimit: number;

    constructor(
        injector: Injector,
        elementReference: ElementRef,
        private gaugeWidgetConfigurationService: GaugeWidgetConfigurationService,
        private dateRangeService: DateRangeService,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _configurationVersionService: ConfigurationVersionService,
        private dashboardPagesService: DashboardPagesService,
        private _dateTimeService: DateTimeService,

    ) {
        super(injector, elementReference, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSGauge');
    }

    get formattedValueWithUnit(): string {
        return this.formattedValue != null ? `${this.formattedValue} ${this.unit}` : '';
    }

    get orientation(): string {
        return this.style && this.style.orientation === 1 ? 'horizontal' : 'vertical';
    }

    customizeTooltip = ({ value }) => {
        const numericValue = Number(value);
        const actualValue = this.hasWeightedScale ? this.mapAxisValueToActual(numericValue) : numericValue;
        const formatted = this.formatNumberWithReturn(actualValue, this.dataUnitType);
        return { text: `${formatted[0]} ${formatted[1]}`.trim() };
    };

    ngOnInit(): void {
        super.ngOnInit();
        this.subs.push(
            this.dashboardPagesService.getPages().subscribe(() => this.updateNavigationAvailability()),
        );
        if (this.isNew) {
            this.runDelayed(() => this.onEditRequested(null));
        }
    }

    onNameEdit(): void {
        this.renameModal.show(this.widgetConfigurationInDB?.name);
    }

    edit(): void {
        this.isEditModalInitialized = true;
        var sub = this._configurationVersionService.refreshVersion().subscribe(() => {
            setTimeout(() => {
                this.createOrEditModal.show(this.widgetConfigurationInDB);
            }, 0);
        });
        this.subs.push(sub);
    }

    protected override onEditRequested(_payload: any): void {
        this.edit();
    }

    protected override onRenameRequested(_payload: any): void {
        this.onNameEdit();
    }

    onConfigurationChange(newConfig: CreateOrEditWidgetConfigurationDto): void {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        
        if (newConfig.id.toString() !== this.widgetConfigurationInDB?.configuration) {
            this.saveConfiguration(newConfig.id.toString());
        } else {
            this.refreshWidget();
        }
    }

    refreshWidget(): void {
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this.gaugeWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.gaugeWidgetConfiguration = result.gaugeWidgetConfiguration;
                    if (this.gaugeWidgetConfiguration) {
                        this.parameter = JSON.parse(this.gaugeWidgetConfiguration.parameter);
                        this.style = JSON.parse(this.gaugeWidgetConfiguration.style) as GaugeStyle;
                        this.setNavigationTarget();
                        this.prepareStyle();
                        this.fetch();
                    }
                });
            this.subs.push(sub);
        }
    }

    fetch() {
        let request = new TableWidgetRequest();

        request.widgetName = this.widgetConfigurationInDB?.name;
        request.userTimeZone = this._dateTimeService.getUserTimeZoneName();
        request.rows = new RowWidgetTable({
            tags: null,
            feeders: this.getFormattedFeedersAndComponents(),
        });
        const parameter = JSON.parse(this.gaugeWidgetConfiguration.parameter) as WidgetParametersColumn;

        if (parameter) {
            request.columnWidgetTables = [parameter].map((column) => {
                let baseData: string = null;
                let customData: CustomWidgetTableData = null;
                let tableEvent: TableWidgetEvent = null;

                switch (column.type) {
                    case ColumnType.Exception:
                    case ColumnType.CustomParameter:
                        customData = new CustomWidgetTableData({
                            id: Number.parseInt(column.data.toString()),
                            ignoreAlignment: false,
                            quantity: column.quantity,
                        });
                        break;

                    case ColumnType.BaseParameter:
                        baseData = this.prepareParameterForRequest(column.type, column.data);
                        break;

                    case ColumnType.Event:
                        tableEvent = this.createTableWidgetEvent(column.data.toString(), column.quantity);
                        break;

                    default:
                        // optionally handle unknown types
                        console.warn('Unknown column type:', column.type);
                        break;
                }

                return new ColumnWidgetTable({
                    parameterType: column.type,
                    normalize: column.gaugeWidgetAdvancedSettings?.normalizeValue,
                    normalValue: column.gaugeWidgetAdvancedSettings?.normalizeNominalValue,
                    excludeFlagged: this.prepareExcludedFlagged(
                        column.gaugeWidgetAdvancedSettings?.excludeFlagged,
                        ArrayUtils.ensureArray(column.gaugeWidgetAdvancedSettings?.defaultFlagEvent),
                    ),
                    ignoreAligningFunction: false,
                    replaceAggregationWith: null,
                    baseData,
                    customData,
                    tableEvent,
                    parameterName: column.name,
                    isExcludeFlaggedData:
                        column.gaugeWidgetAdvancedSettings?.excludeFlagged === ExcludeFlagged.DefaultEvents,
                    markers: this.prepareMarkers(column),
                });
            });
        }

        this.gaugeWidgetRequest = request;

        if (this.gaugeWidgetConfiguration.refreshRate !== -1) {
            var sub = timer(0, this.gaugeWidgetConfiguration.refreshRate * 1000)
                .pipe(takeUntil(this.stopStream$))
                .subscribe(() => {
                    const range = this.prepareDataRange();
                    request.startDate = range[0];
                    request.endDate = range[1];
                    request.isRealTime = true;
                    request.refreshRateInSeconds = this.gaugeWidgetConfiguration.refreshRate;
                    var subGetData = this._tenantDashboardService
                        .pQSGaugeWidgetData(request)
                        .subscribe((result) => this.processResponse(result));
                    this.subs.push(subGetData);
                });
            this.subs.push(sub);
        } else {
            const range = this.prepareDataRange();
            request.startDate = range[0];
            request.endDate = range[1];
            request.isRealTime = false;
            request.refreshRateInSeconds = 0;
                    
            var sub = this._tenantDashboardService
                .pQSGaugeWidgetData(request)
                .subscribe((result) => this.processResponse(result));
            this.subs.push(sub);
        }
    }

    onEditModelClose(isSaved) {
        if (!isSaved && !this.widgetConfigurationInDB?.configuration) {
            abp.event.trigger(
                'app.dashboard.removeWidget',
                this.widgetConfigurationInDB.widgetGuid,
                'Widgets_Tenant_PQSGauge',
            );
        }
    }

    private getFormattedFeedersAndComponents(): FeederComponentInfo[] {
        const parameter = JSON.parse(this.gaugeWidgetConfiguration.parameter) as WidgetParametersColumn;

        if (parameter) {
            const components = parameter.componentsState;

            if (components) {
                let tableWidgetConfigurationComponents = components.components ?? [];
                let formattedFeeders =
                    components.feeders?.map(
                        (f) =>
                            new FeederComponentInfo({
                                ...f,
                                compName:
                                    tableWidgetConfigurationComponents?.find((c) => c.key === f.componentId)?.label ??
                                    '',
                            }),
                    ) ?? [];
                let formattedComponents = tableWidgetConfigurationComponents
                    .filter((c) => !formattedFeeders.some((f) => f.componentId === c.key))
                    .map(
                        (c) => new FeederComponentInfo({ componentId: c.key, id: null, name: null, compName: c.label }),
                    );

                return [...formattedFeeders, ...formattedComponents];
            }
        }

        return [];
    }

    createTableWidgetEvent(json: string, quantity: string): TableWidgetEvent {
        const event = JSON.parse(json);
        const tableWidgetEvent = new TableWidgetEvent({
            phases: event.phases,
            eventId: event.event.confID,
            eventClass: event.event.eventClass,
            isShared: event.event.isShared,
            parameter: event.parameter,
            isPolyphase: event.isPolyphase,
            aggregationInSeconds: event.aggregationInSeconds,
            quantity,
        });

        return tableWidgetEvent;
    }

    private prepareMarkers(column: WidgetParametersColumn): GaugeMarkerDto[] {
        const markers: GaugeMarkerDto[] = [];

        function mapMarker(marker: string): PQBIQuantityType {
            switch (marker) {
                case 'AVG':
                    return PQBIQuantityType.Avg;
                case 'MIN':
                    return PQBIQuantityType.Min;
                case 'MAX':
                    return PQBIQuantityType.Max;
                default:
                    return null;
            }
        }

        if (column.gaugeWidgetAdvancedSettings?.marker1) {
            markers.push(
                new GaugeMarkerDto({
                    kind: MarkerKind.Quantity,
                    key: '',
                    operation: mapMarker(column.gaugeWidgetAdvancedSettings.marker1),
                    percentOfNominal: null,
                }),
            );
        }
        if (column.gaugeWidgetAdvancedSettings?.marker2) {
            markers.push(
                new GaugeMarkerDto({
                    kind: MarkerKind.Nominal,
                    key: '',
                    operation: mapMarker(column.gaugeWidgetAdvancedSettings.marker2),
                    percentOfNominal: null,
                }),
            );
        }

        return markers;
    }

    private prepareParameterForRequest(type: ColumnType, data: string | number): string {
        switch (type) {
            case ColumnType.BaseParameter:
                return data.toString();
            case ColumnType.Event:
                const event = JSON.parse(data.toString());
                return safeStringify({
                    eventId: event.event.eventClass,
                    phases: event.phases,
                    parameter: event.parameter,
                    isPolyphase: event.isPolyphase,
                    aggregationInSeconds: event.aggregationInSeconds,
                });
            default:
                return data.toString();
        }
    }

    prepareExcludedFlagged(excludeFlagged: ExcludeFlagged, selectedEvents: EventClass[]): EventClass[] {
        switch (excludeFlagged) {
            case ExcludeFlagged.None:
                return [];
            case ExcludeFlagged.DefaultEvents:
                return [];
            case ExcludeFlagged.UserSelected:
                return selectedEvents;
            default:
                return [];
        }
    }

    private processResponse(response: TableWidgetResponse) {
        const responseItem = response.items[0];
        this.title = responseItem.parameterName;
        // if (
        //     this.parameter?.cardWidgetAdvancedSettings?.normalizeValue === NormalizeEnum.VALUE &&
        //     this.parameter?.cardWidgetAdvancedSettings?.normalizeNominalValue
        // ) {
        //     responseItem.calculated = normalize(
        //         responseItem.calculated,
        //         this.parameter.cardWidgetAdvancedSettings.normalizeNominalValue,
        //     );
        // }
        this.actualValue = responseItem.calculated;
        this.dataUnitType = responseItem.dataUnitType;
        this.formatNumber(responseItem.calculated, responseItem.dataUnitType);
        this.setLimits(responseItem);
        this.setSubvalues(responseItem);

        this.calculatedColorSchema = getColorSchema(
            responseItem.calculated,
            this.parameter.gaugeWidgetAdvancedSettings,
        );

        this.setSegments();

        if (this.calculatedColorSchema != null) {
            this.titleFontColor = this.calculatedColorSchema;
            this.valueFontColor = this.calculatedColorSchema;
        }

        this.titleFontSize = this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.size
            ? this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.size
            : this.titleFontSize;
        this.titleFontColor =
            this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.colorMode === 'custom'
                ? this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.customColor
                : this.titleFontColor;
        this.titleFontFamily = this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.family;
        this.widgetNameFontSize = this.resolveWidgetNameFontSize(
            this.parameter.gaugeWidgetAdvancedSettings?.titleFont?.size,
        );

        this.valueFontSize = this.parameter.gaugeWidgetAdvancedSettings?.valueFont?.size
            ? this.parameter.gaugeWidgetAdvancedSettings?.valueFont?.size
            : this.valueFontSize;
        this.valueFontColor =
            this.parameter.gaugeWidgetAdvancedSettings?.valueFont?.colorMode === 'custom'
                ? this.parameter.gaugeWidgetAdvancedSettings?.valueFont?.customColor
                : this.titleFontColor;
        this.valueFontFamily = this.parameter.gaugeWidgetAdvancedSettings?.valueFont?.family;
    }

    private prepareStyle() {
        if (this.style && this.style.style === GaugeStyleEnum.Circle) {
            if (!this.style.startAngle && !this.style.endAngle) {
                switch (this.style.arcAngle) {
                    case GaugeStyleArcAngleEnum.TopHalf:
                        this.style.startAngle = 180;
                        this.style.endAngle = 0;
                        break;
                    case GaugeStyleArcAngleEnum.FullCircle:
                        this.style.startAngle = 270;
                        this.style.endAngle = -90;
                        break;
                    case GaugeStyleArcAngleEnum.BottomHalf:
                        this.style.startAngle = 0;
                        this.style.endAngle = -180;
                        break;
                    case GaugeStyleArcAngleEnum.LeftHalf:
                        this.style.startAngle = -90;
                        this.style.endAngle = 90;
                        break;
                    case GaugeStyleArcAngleEnum.RightHalf:
                        this.style.startAngle = 90;
                        this.style.endAngle = -90;
                        break;
                    case GaugeStyleArcAngleEnum.ThreeQuarters:
                        this.style.startAngle = -90;
                        this.style.endAngle = 0;
                        break;
                    case GaugeStyleArcAngleEnum.Quarter:
                        this.style.startAngle = 225;
                        this.style.endAngle = 135;
                        break;
                }
            }
        }
    }

    private prepareDataRange(): [DateTime, DateTime] {
        const state: DateRangeAndRefreshModelNew = this.gaugeWidgetConfiguration?.dateRange
            ? DateRangeAndRefreshModelNew.createItem(this.gaugeWidgetConfiguration.dateRange)
            : DateRangeAndRefreshModelNew.createItem('');
        var [startDate, endDate] = this._dateRangeService.getDateRangeFromNewState(state);

        if (!startDate || !endDate || startDate >= endDate) {
            [startDate, endDate] = this.dateRangeService.getDateRangeFromNewUnit(RefreshSelectionCustomUnits.Day, 30);
        }

        return [DateTime.fromJSDate(startDate), DateTime.fromJSDate(endDate)];
    }

    private setLimits(item: TableWidgetResponseItem) {
        let lower: number;
        let upper: number;
        if (
            this.parameter?.gaugeWidgetAdvancedSettings?.lowerLimit !== null &&
            this.parameter?.gaugeWidgetAdvancedSettings?.upperLimit !== null &&
            this.parameter?.gaugeWidgetAdvancedSettings?.lowerLimit !== undefined &&
            this.parameter?.gaugeWidgetAdvancedSettings?.upperLimit !== undefined &&
            this.parameter?.gaugeWidgetAdvancedSettings?.lowerLimit !==
                this.parameter?.gaugeWidgetAdvancedSettings?.upperLimit
        ) {
            lower = this.parameter.gaugeWidgetAdvancedSettings.lowerLimit;
            upper = this.parameter.gaugeWidgetAdvancedSettings.upperLimit;
        } else {
            if (
                this.parameter?.gaugeWidgetAdvancedSettings?.marker1 !== null &&
                this.parameter?.gaugeWidgetAdvancedSettings?.marker2 !== null &&
                this.parameter?.gaugeWidgetAdvancedSettings?.marker1 !== undefined &&
                this.parameter?.gaugeWidgetAdvancedSettings?.marker2 !== undefined
            ) {
                const values = item.gaugeMarkerResultList?.map((m) => m.value).filter((v) => v != null) ?? [];
                const min = Math.min(...values);
                const max = Math.max(...values);
                lower = min - 10;
                upper = max + 10;
            } else if (item.calculated !== 0) {
                lower = item.calculated * 0.8;
                upper = item.calculated * 1.2;
            } else {
                lower = -10;
                upper = 10;
            }
        }

        this.actualLowerLimit = Math.min(lower, upper);
        this.actualUpperLimit = Math.max(lower, upper);
        this.lowerLimit = lower;
        this.upperLimit = upper;

        if (this.style.style === GaugeStyleEnum.Linear && this.style.isInvertScale) {
            const temp = this.lowerLimit;
            this.lowerLimit = this.upperLimit;
            this.upperLimit = temp;
        }
    }

    private setSubvalues(item: TableWidgetResponseItem) {
        this.actualSubvalues = item.gaugeMarkerResultList?.map((m) => m.value).filter((v) => v != null);
        this.subvalues = this.actualSubvalues ? [...this.actualSubvalues] : undefined;    }

    private setSegments() {
         const segments = this.parameter?.gaugeWidgetAdvancedSettings?.segments ?? [];

        this.weightedSegments = this.buildWeightedSegments(segments);
        this.hasWeightedScale = this.weightedSegments.length > 0;

        if (!this.hasWeightedScale) {
            this.ranges = segments.map((s) => ({
                startValue: s.from,
                endValue: s.to,
                color: s.colorMode === 'custom' ? s.color : this.calculatedColorSchema,
            }));
            this.displayValue = this.actualValue;
            this.subvalues = this.actualSubvalues ? [...this.actualSubvalues] : undefined;
            this.scaleCustomTicks = undefined;
            this.lowerLimit = this.actualLowerLimit;
            this.upperLimit = this.actualUpperLimit;
            return;
        }

        this.ranges = this.weightedSegments.map((segment) => ({
            startValue: this.toAxisCoordinate(segment.startPosition),
            endValue: this.toAxisCoordinate(segment.endPosition),
            color: segment.colorMode === 'custom' ? segment.color : this.calculatedColorSchema,
        }));
        const tickPositions = Array.from(
            new Set(this.weightedSegments.flatMap((segment) => [segment.startPosition, segment.endPosition])),
        ).sort((a, b) => a - b);

        this.scaleCustomTicks = tickPositions.map((position) => this.toAxisCoordinate(position));

        this.displayValue = this.mapActualToAxisValue(this.actualValue);
        this.subvalues = this.actualSubvalues?.map((value) => this.mapActualToAxisValue(value));

        this.lowerLimit = this.toAxisCoordinate(0);
        this.upperLimit = this.toAxisCoordinate(100);
    }

    private buildWeightedSegments(segments: Segment[]): WeightedSegmentMeta[] {
        if (!segments?.length) {
            return [];
        }

        const prepared = segments
            .map((segment) => ({
                ...segment,
                from: +segment.from,
                to: +segment.to,
                weight: segment.weight != null ? +segment.weight : undefined,
            }))
            .sort((a, b) => a.from - b.from);

        let weights = prepared.map((segment) => segment.weight ?? 0);

        if (prepared.some((segment) => segment.weight == null)) {
            const totalSpan = prepared.reduce((sum, segment) => sum + Math.max(segment.to - segment.from, 0), 0);

            if (totalSpan > 0) {
                weights = prepared.map((segment) => {
                    const span = Math.max(segment.to - segment.from, 0);
                    return span === 0 ? 0 : (span / totalSpan) * 100;
                });
            } else {
                const equalWeight = 100 / prepared.length;
                weights = prepared.map(() => equalWeight);
            }
        }

        const totalWeight = weights.reduce((sum, weight) => sum + weight, 0);
        if (!totalWeight) {
            return [];
        }

        const factor = 100 / totalWeight;
        let cursor = 0;

        return prepared.map((segment, index) => {
            const normalizedWeight = weights[index] * factor;
            const startPosition = cursor;
            cursor += normalizedWeight;
            const endPosition = index === prepared.length - 1 ? 100 : cursor;
            return {
                ...segment,
                weight: normalizedWeight,
                startPosition,
                endPosition,
            };
        });
    }

    private mapActualToAxisValue(value: number | null | undefined): number {
        if (value === null || value === undefined) {
            return value as any;
        }
        if (!this.hasWeightedScale) {
            return value;
        }
        const normalized = this.mapActualToNormalized(value);
        return this.toAxisCoordinate(normalized);
    }

    private mapAxisValueToActual(axisValue: number | null | undefined): number {
        if (axisValue === null || axisValue === undefined || !this.hasWeightedScale) {
            return axisValue as any;
        }
        const normalized = this.fromAxisCoordinate(axisValue);
        return this.mapNormalizedToActual(normalized);
    }

    private mapActualToNormalized(value: number): number {
        if (!this.weightedSegments.length) {
            return value;
        }

        const first = this.weightedSegments[0];
        const last = this.weightedSegments[this.weightedSegments.length - 1];

        if (value <= first.from) {
            return this.interpolateSegmentValue(first, value);
        }

        if (value >= last.to) {
            return this.interpolateSegmentValue(last, value);
        }

        for (const segment of this.weightedSegments) {
            if (value >= segment.from && value <= segment.to) {
                return this.interpolateSegmentValue(segment, value);
            }
        }

        const previous = [...this.weightedSegments].reverse().find((segment) => value > segment.to);
        if (previous) {
            return previous.endPosition;
        }

        const next = this.weightedSegments.find((segment) => value < segment.from);
        if (next) {
            return next.startPosition;
        }

        return value;
    }

    private mapNormalizedToActual(position: number): number {
        if (!this.weightedSegments.length) {
            return position;
        }

        const first = this.weightedSegments[0];
        const last = this.weightedSegments[this.weightedSegments.length - 1];

        if (position <= first.startPosition) {
            return first.from;
        }

        if (position >= last.endPosition) {
            return last.to;
        }

        for (const segment of this.weightedSegments) {
            if (position >= segment.startPosition && position <= segment.endPosition) {
                return this.interpolatePosition(segment, position);
            }
        }

        return position;
    }

    private interpolateSegmentValue(segment: WeightedSegmentMeta, value: number): number {
        if (segment.to === segment.from) {
            return segment.endPosition;
        }
        const ratio = (value - segment.from) / (segment.to - segment.from);
        return segment.startPosition + this.clamp(ratio, 0, 1) * (segment.endPosition - segment.startPosition);
    }

    private interpolatePosition(segment: WeightedSegmentMeta, position: number): number {
        if (segment.endPosition === segment.startPosition) {
            return segment.to;
        }
        const ratio = (position - segment.startPosition) / (segment.endPosition - segment.startPosition);
        return segment.from + this.clamp(ratio, 0, 1) * (segment.to - segment.from);
    }

    private toAxisCoordinate(position: number): number {
        return this.isInvertedScale ? 100 - position : position;
    }

    private fromAxisCoordinate(position: number): number {
        return this.isInvertedScale ? 100 - position : position;
    }

    private get isInvertedScale(): boolean {
        return this.style?.style === GaugeStyleEnum.Linear && this.style.isInvertScale;
    }

    private clamp(value: number, min: number, max: number): number {
        return Math.min(Math.max(value, min), max);
    }

    private getScaleLabelText(info: { value: number; valueText: string }): string {
        if (!this.hasWeightedScale) {
            return info.valueText;
        }

        const actualValue = this.mapAxisValueToActual(info.value);
        if (actualValue === null || actualValue === undefined || Number.isNaN(actualValue)) {
            return '';
        }

        const decimalPoints = this.parameter?.gaugeWidgetAdvancedSettings?.decimalPoints ?? 2;
        return actualValue.toFixed(decimalPoints);
    }

    private roundValue(value: number): number {
        let roundTo = 2;
        if (
            this.parameter?.gaugeWidgetAdvancedSettings?.decimalPoints !== null &&
            this.parameter?.gaugeWidgetAdvancedSettings?.decimalPoints !== undefined
        ) {
            roundTo = this.parameter.gaugeWidgetAdvancedSettings.decimalPoints;
        }
        return Number.parseFloat(value.toFixed(roundTo));
    }

    private formatNumber(value: number, dataUnitType: DataUnitType) {
        const res = this.formatNumberWithReturn(value, dataUnitType);

        this.formattedValue = res[0];
        this.unit = res[1];
    }

    private formatNumberWithReturn(value: number, dataUnitType: DataUnitType): [number, string] {
        const token = dataUnitType?.id !== 41 && dataUnitType?.id !== 255 ? this.l(dataUnitType.tokenCode) : '';

        if (value === 0 || dataUnitType.id === 18 || dataUnitType.id === 19) {
            // 18 19 is Percentage
            const roundedValue = this.roundValue(value);
            return [roundedValue, `${token}`];
        }

        let absValue = Math.abs(value);
        let suffix = '';

        if (absValue >= 1) {
            if (absValue >= 1e3 && absValue < 1e6) {
                value /= 1e3;
                suffix = 'K';
            } else if (absValue >= 1e6 && absValue < 1e9) {
                value /= 1e6;
                suffix = 'M';
            } else if (absValue >= 1e9 && absValue < 1e12) {
                value /= 1e9;
                suffix = 'G';
            }
        } else {
            let strValue = value.toExponential();
            let leadingZeros = Math.abs(parseInt(strValue.split('e')[1]));

            if (leadingZeros >= 1 && leadingZeros <= 3) {
                value *= 1e3;
                suffix = 'm';
            } else if (leadingZeros >= 4 && leadingZeros <= 6) {
                value *= 1e6;
                suffix = 'μ';
            } else if (leadingZeros >= 7 && leadingZeros <= 9) {
                value *= 1e9;
                suffix = 'n';
            }
        }

        const roundedValue = this.roundValue(value);
        const unit = `${suffix}${token}`;
        return [roundedValue, unit];
    }

    onNavigateToPage(): void {
        if (!this.navigationPageId) {
            return;
        }

        abp.event.trigger('app.dashboard.navigateToPage', this.navigationPageId);
    }

    private setNavigationTarget(): void {
        this.navigationPageId = this.parameter?.gaugeWidgetAdvancedSettings?.linkPage ?? null;
        this.updateNavigationAvailability();
    }

    private updateNavigationAvailability(): void {
        this.canNavigateToPage = !!this.dashboardPagesService.findPage(this.navigationPageId);
    }
    ngOnDestroy() {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.subs.forEach((sub) => sub.unsubscribe());
    }
}

// function normalize(value: number, normalizationValue: number): number {
//   if (normalizationValue === 0) {
//     return value;
//   }
//   return value / normalizationValue;
// }

function getColorSchema(value: number | null | undefined, settings: GaugeWidgetAdvancedSettingsConfig): string | null {
    if (!settings) return null;

    const isNoData =
        value === null ||
        value === undefined ||
        (typeof value === 'string' && ['DB_OUT_OF_RANGE', 'NO_DATA', 'N/A', ''].includes((value as string).trim()));

    const {
        lowerLimit,
        upperLimit,
        colorScheme,
        outOfLimitColor,
    } = settings;

    if (colorScheme === ColorSchema.None) {
        return null;
    }

    if (
        colorScheme === ColorSchema.OutOfLimit &&
        outOfLimitColor &&
        lowerLimit != null &&
        upperLimit != null &&
        (value < lowerLimit || value > upperLimit)
    ) {
        return outOfLimitColor;
    }

    return null;
}
