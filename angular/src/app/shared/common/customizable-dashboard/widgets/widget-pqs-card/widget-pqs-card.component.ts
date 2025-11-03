import { Component, ElementRef, Injector, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { CreateOrEditCardConfigurationComponent } from './create-or-edit-card-configuration/create-or-edit-card-configuration.component';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import {
    CardWidgetConfigurationDto,
    CardWidgetConfigurationsServiceProxy,
    ColumnWidgetTable,
    CreateOrEditWidgetConfigurationDto,
    CustomWidgetTableData,
    DataUnitType,
    EventClass,
    FeederComponentInfo,
    RowWidgetTable,
    TableWidgetEvent,
    TableWidgetRequest,
    TableWidgetResponse,
    TenantDashboardServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ColorSchema, ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { ColumnType } from '@app/shared/enums/column-type';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import safeStringify from 'fast-safe-stringify';
import { Subject, Subscription, takeUntil, timer } from 'rxjs';
import { DateTime } from 'luxon';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { CardWidgetConfigurationService } from '@app/shared/services/widget-configurations/card-widget-configuration.service';

@Component({
    selector: 'app-widget-pqs-card',
    templateUrl: './widget-pqs-card.component.html',
    styleUrl: './widget-pqs-card.component.css',
})
export class WidgetPqsCardComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy {
    @ViewChild('createOrEditModal') createOrEditModal: CreateOrEditCardConfigurationComponent;
    @ViewChild('renameWidgetModal') renameModal: RenameWidgetModalComponent;

    cardWidgetConfiguration: CardWidgetConfigurationDto;
    cardWidgetRequest: TableWidgetRequest;

    parameter: WidgetParametersColumn;

    value: number;
    unit = '';
    title = '';
    valueFontSize = '2.2em';
    valueFontFamily = '';
    valueFontColor = '#000';
    titleFontSize = '1.2em';
    titleFontFamily = '';
    titleFontColor = '#000';
    iconColor: string = '#5b9bd5';
    isIconVisible: boolean = true;

    calculatedColorSchema: string | null;

    private stopStream$ = new Subject();
    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        elementReference: ElementRef,
        private cardWidgetConfigurationService: CardWidgetConfigurationService,
        private dateRangeService: DateRangeService,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _configurationVersionService: ConfigurationVersionService,
    ) {
        super(injector, elementReference, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSCard');
    }

    ngOnInit(): void {
        super.ngOnInit();
        if (this.isNew) {
            this.runDelayed(() => this.edit());
        }
    }

    onNameEdit(): void {
        this.renameModal.show(this.widgetConfigurationInDB?.name);
    }

    edit(): void {
        this.isEditModalInitialized = true;
        var sub = this._configurationVersionService.refreshVersion().subscribe();
        this.subs.push(sub);
        setTimeout(() => {
            this.createOrEditModal.show(this.widgetConfigurationInDB);
        }, 0);
    }

    onConfigurationChange(newConfig: CreateOrEditWidgetConfigurationDto): void {
        this.saveConfiguration(newConfig.id.toString());
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.refreshWidget();
    }

    refreshWidget(): void {
        if (this.widgetConfigurationInDB && this.widgetConfigurationInDB.configuration) {
            var sub = this.cardWidgetConfigurationService.getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.cardWidgetConfiguration = result.cardWidgetConfiguration;
                    if (this.cardWidgetConfiguration) {
                        let parameters = JSON.parse(this.cardWidgetConfiguration.parameters);
                        this.parameter = parameters.at(0);
                        this.fetch();
                    }
                });
            this.subs.push(sub);
        }
    }

    fetch() {
        let request = new TableWidgetRequest();

        request.widgetName = this.widgetConfigurationInDB?.name;
        request.userTimeZone = 1;
        request.rows = new RowWidgetTable({
            tags: null,
            feeders: this.getFormattedFeedersAndComponents(),
        });
        const parameters = JSON.parse(this.cardWidgetConfiguration.parameters) as WidgetParametersColumn[];

        if (parameters.some((p) => p)) {
            request.columnWidgetTables = parameters.map((column) => {
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
                    normalize: column.cardWidgetAdvancedSettings?.normalizeValue,
                    normalValue: column.cardWidgetAdvancedSettings?.normalizeNominalValue,
                    excludeFlagged: this.prepareExcludedFlagged(
                        column.cardWidgetAdvancedSettings?.excludeFlagged,
                        ArrayUtils.ensureArray(column.cardWidgetAdvancedSettings?.defaultFlagEvent),
                    ),
                    ignoreAligningFunction: false,
                    replaceAggregationWith: null,
                    baseData,
                    customData,
                    tableEvent,
                    parameterName: column.name,
                    isExcludeFlaggedData: column.cardWidgetAdvancedSettings?.excludeFlagged === ExcludeFlagged.DefaultEvents,
                    markers: null
                });
            });
        }

        this.cardWidgetRequest = request;

        if (this.cardWidgetConfiguration.refreshRate !== -1) {
            var subTimer = timer(0, this.cardWidgetConfiguration.refreshRate * 1000)
                .pipe(takeUntil(this.stopStream$))
                .subscribe(() => {
                    const range = this.prepareDataRange();
                    request.startDate = range[0].toUTC();
                    request.endDate = range[1].toUTC();
                    var sub = this._tenantDashboardService
                        .pQSCardWidgetData(request)
                        .subscribe((result) => this.processResponse(result));
                    this.subs.push(sub);
                });
            this.subs.push(subTimer);
        } else {
            const range = this.prepareDataRange();
            request.startDate = range[0].toUTC();
            request.endDate = range[1].toUTC();
            var sub = this._tenantDashboardService.pQSCardWidgetData(request).subscribe((result) => this.processResponse(result));
            this.subs.push(sub);
        }
    }

    private getFormattedFeedersAndComponents(): FeederComponentInfo[] {
        const parameters = JSON.parse(this.cardWidgetConfiguration.parameters) as WidgetParametersColumn[];

        if (parameters.some((p) => p)) {
            const components = parameters.at(0).componentsState;

            if (components) {
                let tableWidgetConfigurationComponents = components.components ?? [];
                let formattedFeeders = components.feeders?.map((f) => new FeederComponentInfo({
                    ...f,
                    compName: tableWidgetConfigurationComponents?.find(c => c.key === f.componentId)?.label ?? ''
                })) ?? [];
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

    onEditModelClose(isSaved) {
        if (!isSaved && !this.widgetConfigurationInDB?.configuration)
        {
            abp.event.trigger('app.dashboard.removeWidget', this.widgetConfigurationInDB.widgetGuid, 'Widgets_Tenant_PQSCard');
        }
    }

    private processResponse(response: TableWidgetResponse) {
        const responseItem = response.items[0];
        this.title = responseItem.parameterName;
        // if(this.parameter?.cardWidgetAdvancedSettings?.normalizeValue === NormalizeEnum.VALUE && this.parameter?.cardWidgetAdvancedSettings?.normalizeNominalValue){ 
        //     responseItem.calculated = normalize(responseItem.calculated, this.parameter.cardWidgetAdvancedSettings.normalizeNominalValue);
        // }
        this.formatNumber(responseItem.calculated, responseItem.dataUnitType);

        this.calculatedColorSchema = getColorSchema(responseItem.calculated, this.parameter.cardWidgetAdvancedSettings);

        if (this.calculatedColorSchema != null) {
            this.titleFontColor = this.calculatedColorSchema;
            this.valueFontColor = this.calculatedColorSchema;
            this.iconColor = this.calculatedColorSchema;
        }

        if (
            this.parameter.cardWidgetAdvancedSettings?.lowerLimit < responseItem.calculated 
            && this.parameter.cardWidgetAdvancedSettings?.upperLimit > responseItem.calculated
            && this.parameter.cardWidgetAdvancedSettings?.icon.appearance === 'limits'
        ) {
            this.isIconVisible = false;
        }

        this.titleFontSize = this.parameter.cardWidgetAdvancedSettings?.titleFont?.size
            ? `${this.parameter.cardWidgetAdvancedSettings?.titleFont?.size}px`
            : this.titleFontSize;
        this.titleFontColor =
            this.parameter.cardWidgetAdvancedSettings?.titleFont?.colorMode === 'custom'
                ? this.parameter.cardWidgetAdvancedSettings?.titleFont?.customColor
                : this.titleFontColor;
        this.titleFontFamily = this.parameter.cardWidgetAdvancedSettings?.titleFont?.family;

        this.valueFontSize = this.parameter.cardWidgetAdvancedSettings?.valueFont?.size
            ? `${this.parameter.cardWidgetAdvancedSettings?.valueFont?.size}px`
            : this.valueFontSize;
        this.valueFontColor =
            this.parameter.cardWidgetAdvancedSettings?.valueFont?.colorMode === 'custom'
                ? this.parameter.cardWidgetAdvancedSettings?.valueFont?.customColor
                : this.valueFontColor;
        this.valueFontFamily = this.parameter.cardWidgetAdvancedSettings?.valueFont?.family;
        this.iconColor =
            this.parameter.cardWidgetAdvancedSettings?.icon?.colorMode === 'custom'
                ? this.parameter.cardWidgetAdvancedSettings?.icon?.customColor
                : this.iconColor;
    }

    private prepareDataRange(): [DateTime, DateTime] {
        const state: DateRangeState = this.cardWidgetConfiguration?.dateRange
            ? DateRangeState.fromJSON(this.cardWidgetConfiguration.dateRange)
            : new DateRangeState({ rangeOption: DateRangeUnits.LAST_7_DAYS, startDate: null, endDate: null });

        let [startDate, endDate] = this.dateRangeService.getDateRangeFromState(state);

        if (!startDate || !endDate || startDate >= endDate) {
            [startDate, endDate] = this.dateRangeService.getDateRangeFromUnit(DateRangeUnits.LAST_7_DAYS);
        }

        return [startDate, endDate];
    }

    private roundValue(value: number): number {
        let roundTo = 2;
        if (this.parameter?.cardWidgetAdvancedSettings?.decimalPoints !== null
            && this.parameter?.cardWidgetAdvancedSettings?.decimalPoints !== undefined
        ) {
            roundTo = this.parameter.cardWidgetAdvancedSettings.decimalPoints;
        }
        return Number.parseFloat(value.toFixed(roundTo));
    }

    private formatNumber(value: number, dataUnitType: DataUnitType) {
        const token = dataUnitType?.id !== 41 && dataUnitType?.id !== 255 ? this.l(dataUnitType.tokenCode) : '';

        if (value === 0 || dataUnitType.id === 18 || dataUnitType.id === 19) { // 18 19 is Percentage
            this.value = this.roundValue(value);
            this.unit = `${token}`;
            return;
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

        this.value = this.roundValue(value);
        this.unit = `${suffix}${token}`;
    }

    ngOnDestroy() {
        this.stopStream$.next(null);
        this.stopStream$.complete();
        this.subs.forEach(sub => sub.unsubscribe());
    }
}

function getColorSchema(value: number | null | undefined, settings: CardWidgetAdvancedSettingsConfig): string | null {
    if (!settings) return null;

    const isNoData =
        value === null ||
        value === undefined ||
        (typeof value === 'string' && ['DB_OUT_OF_RANGE', 'NO_DATA', 'N/A', ''].includes((value as string).trim()));

    const {
        lowerLimit,
        upperLimit,
        okColor,
        noDataColor,
        showOkColor,
        showNoDataColor,
        colorScheme,
        gradientFromColor,
        gradientToColor,
        outOfLimitColor,
    } = settings;

    if (isNoData && showNoDataColor && noDataColor) {
        return noDataColor;
    }

    if (
        showOkColor &&
        lowerLimit != null &&
        upperLimit != null &&
        value != null &&
        value >= lowerLimit &&
        value <= upperLimit &&
        okColor
    ) {
        return okColor;
    }

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

// function normalize(value: number, normalizationValue: number): number {
//   if (normalizationValue === 0) {
//     return value;
//   }
//   return value / normalizationValue;
// }

function normalizeValue(value: number, min: number, max: number): number {
    if (max === min) return 1;
    return (value - min) / (max - min);
}

function interpolateColor(from: string, to: string, ratio: number): string {
    const f = hexToRgb(from);
    const t = hexToRgb(to);
    if (!f || !t) return from;

    const r = Math.round(lerp(f.r, t.r, ratio));
    const g = Math.round(lerp(f.g, t.g, ratio));
    const b = Math.round(lerp(f.b, t.b, ratio));

    const alpha = 0.6;
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function lerp(a: number, b: number, t: number): number {
    if (t < 0) return a;
    if (t > 1.5) return b;
    return a + (b - a) * Math.min(t, 1);
}

function hexToRgb(hex: string): { r: number; g: number; b: number } | null {
    const clean = hex.replace('#', '');
    const bigint = parseInt(clean, 16);
    if (clean.length === 6) {
        return {
            r: (bigint >> 16) & 255,
            g: (bigint >> 8) & 255,
            b: bigint & 255,
        };
    }
    return null;
}
