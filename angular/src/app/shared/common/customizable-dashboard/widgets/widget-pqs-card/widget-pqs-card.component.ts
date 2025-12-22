import { Component, ElementRef, Injector, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WidgetComponentBaseComponent } from '../widget-component-base';
import { DateRangeService } from '@app/shared/services/date-range-service';
import { CreateOrEditCardConfigurationComponent } from './create-or-edit-card-configuration/create-or-edit-card-configuration.component';
import { RenameWidgetModalComponent } from '../../rename-widget-modal/rename-widget-modal.component';
import {
    CardWidgetConfigurationDto,
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
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ColorSchema, ExcludeFlagged } from '@app/shared/enums/advanced-settings-options';
import { ColumnType } from '@app/shared/enums/column-type';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import safeStringify from 'fast-safe-stringify';
import { of, Subject, Subscription, switchMap, takeUntil, timer } from 'rxjs';
import { DateTime } from 'luxon';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { ConfigurationVersionService } from '@app/shared/services/configuration-version-service.service';
import { CardWidgetConfigurationService } from '@app/shared/services/widget-configurations/card-widget-configuration.service';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { CardIconService } from '@app/shared/services/card-icon.service';


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
    widgetNameFontSize?: string;
    widgetDisplayName = '';
    private localWidgetNameFontSize?: number;
    iconColor: string = '#5b9bd5';
    isIconVisible: boolean = true;
    navigationPageId: string | null = null;
    canNavigateToPage = false;

    calculatedColorSchema: string | null;
    private defaultIconColor = '#5b9bd5';

    private stopStream$ = new Subject();
    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        elementReference: ElementRef,
        private cardWidgetConfigurationService: CardWidgetConfigurationService,
        private dateRangeService: DateRangeService,
        private _tenantDashboardService: TenantDashboardServiceProxy,
        private _configurationVersionService: ConfigurationVersionService,
        private dashboardPagesService: DashboardPagesService,

        private _dateTimeService: DateTimeService,
        private cardIconService: CardIconService,
    ) {
        super(injector, elementReference, dateRangeService);
        this._defaultWidgetName = this.l('WidgetPQSCard');
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.widgetDisplayName = this.widgetConfigurationInDB?.name ?? this._defaultWidgetName;
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
            var sub = this.cardWidgetConfigurationService
                .getForEdit(+this.widgetConfigurationInDB.configuration)
                .subscribe((result) => {
                    this.cardWidgetConfiguration = result.cardWidgetConfiguration;
                    this.widgetDisplayName = this.widgetConfigurationInDB?.name ?? this.widgetDisplayName;
                    if (this.cardWidgetConfiguration) {
                        let parameters = JSON.parse(this.cardWidgetConfiguration.parameters);
                        this.parameter = parameters.at(0);
                        this.calculatedColorSchema = null;
                        this.resetFontSettings();
                        this.applyFontSettings(this.parameter.cardWidgetAdvancedSettings, null);
                        this.loadIconForParameter(this.parameter);
                        this.setNavigationTarget();
                        this.fetch();
                    }
                });
            this.subs.push(sub);
        }
    }

    private loadIconForParameter(parameter: WidgetParametersColumn | null) {
        const iconSettings = parameter?.cardWidgetAdvancedSettings?.icon;
        if (!iconSettings) {
            return;
        }

        const icon$ = iconSettings.id
            ? this.cardIconService.getIconById(iconSettings.id)
            : this.cardIconService.getDefaultIconId().pipe(
                  switchMap((id) => (id ? this.cardIconService.getIconById(id) : of(null))),
              );

        const sub = icon$.subscribe((icon) => {
            if (parameter?.cardWidgetAdvancedSettings?.icon) {
                parameter.cardWidgetAdvancedSettings.icon.file = icon?.content ?? null;
                parameter.cardWidgetAdvancedSettings.icon.name = icon?.name ?? null;
            }
        });

        this.subs.push(sub);
    }
    
    fetch() {
        let request = new TableWidgetRequest();

        request.widgetName = this.widgetConfigurationInDB?.name;
        request.userTimeZone = this._dateTimeService.getUserTimeZoneName();
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
                    isExcludeFlaggedData:
                        column.cardWidgetAdvancedSettings?.excludeFlagged === ExcludeFlagged.DefaultEvents,
                    markers: null,
                });
            });
        }

        this.cardWidgetRequest = request;

        if (this.cardWidgetConfiguration.refreshRate !== -1) {
            var subTimer = timer(0, this.cardWidgetConfiguration.refreshRate * 1000)
                .pipe(takeUntil(this.stopStream$))
                .subscribe(() => {
                    const range = this.prepareDataRange();
                    request.startDate = range[0];
                    request.endDate = range[1];
                    request.isRealTime = true;
                    request.refreshRateInSeconds = this.cardWidgetConfiguration.refreshRate;
                    
                    var sub = this._tenantDashboardService
                        .pQSCardWidgetData(request)
                        .subscribe((result) => this.processResponse(result));
                    this.subs.push(sub);
                });
            this.subs.push(subTimer);
        } else {
            const range = this.prepareDataRange();
            request.startDate = range[0];
            request.endDate = range[1];
            request.isRealTime = false;
            request.refreshRateInSeconds = 0;
            var sub = this._tenantDashboardService
                .pQSCardWidgetData(request)
                .subscribe((result) => this.processResponse(result));
            this.subs.push(sub);
        }
    }

    private getFormattedFeedersAndComponents(): FeederComponentInfo[] {
        const parameters = JSON.parse(this.cardWidgetConfiguration.parameters) as WidgetParametersColumn[];

        if (parameters.some((p) => p)) {
            const components = parameters.at(0).componentsState;

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
        if (!isSaved && !this.widgetConfigurationInDB?.configuration) {
            abp.event.trigger(
                'app.dashboard.removeWidget',
                this.widgetConfigurationInDB.widgetGuid,
                'Widgets_Tenant_PQSCard',
            );
        }
    }

    private processResponse(response: TableWidgetResponse) {
        const responseItem = response.items[0];
        const customParameterName = this.parameter?.cardWidgetAdvancedSettings?.parameterName?.trim();
        const parameterName = this.parameter?.name;
        this.title = customParameterName || parameterName || responseItem.parameterName;
        // if(this.parameter?.cardWidgetAdvancedSettings?.normalizeValue === NormalizeEnum.VALUE && this.parameter?.cardWidgetAdvancedSettings?.normalizeNominalValue){
        //     responseItem.calculated = normalize(responseItem.calculated, this.parameter.cardWidgetAdvancedSettings.normalizeNominalValue);
        // }
        this.formatNumber(responseItem.calculated, responseItem.dataUnitType);

        this.calculatedColorSchema = getColorSchema(responseItem.calculated, this.parameter.cardWidgetAdvancedSettings);

        if (this.calculatedColorSchema != null) {
            this.iconColor = this.calculatedColorSchema;
        }

        if (
            this.parameter.cardWidgetAdvancedSettings?.lowerLimit < responseItem.calculated &&
            this.parameter.cardWidgetAdvancedSettings?.upperLimit > responseItem.calculated &&
            this.parameter.cardWidgetAdvancedSettings?.icon.appearance === 'limits'
        ) {
            this.isIconVisible = false;
        }

        this.applyFontSettings(this.parameter.cardWidgetAdvancedSettings, this.calculatedColorSchema);
        this.iconColor =
            this.parameter.cardWidgetAdvancedSettings?.icon?.colorMode === 'custom'
                ? this.parameter.cardWidgetAdvancedSettings?.icon?.customColor
                : this.iconColor;
    }

    protected override resolveWidgetNameFontSize(localSize?: number, defaultSize?: string): string | undefined {
        const effectiveLocalSize = localSize ?? this.localWidgetNameFontSize;

        if (effectiveLocalSize) {
            return `${effectiveLocalSize}px`;
        }

        if (this.globalWidgetNameFontSize) {
            return `${this.globalWidgetNameFontSize}px`;
        }

        return defaultSize;
    }

    private resetFontSettings(): void {
        this.titleFontSize = '1.2em';
        this.titleFontFamily = '';
        this.titleFontColor = '#000';
        this.valueFontSize = '2.2em';
        this.valueFontFamily = '';
        this.valueFontColor = '#000';
        this.localWidgetNameFontSize = undefined;
        this.widgetNameFontSize = this.resolveWidgetNameFontSize();
        this.iconColor = this.defaultIconColor;
    }

    private applyFontSettings(
        settings: CardWidgetAdvancedSettingsConfig | undefined,
        colorSchema: string | null,
    ): void {
        if (!settings) {
            return;
        }

        const titleFontSizeSetting = settings.titleFont?.size;
        this.localWidgetNameFontSize = titleFontSizeSetting ?? this.localWidgetNameFontSize;
        if (titleFontSizeSetting) {
            this.titleFontSize = `${titleFontSizeSetting}px`;
        }

        const resolvedSchema = colorSchema ?? this.calculatedColorSchema;

        this.titleFontColor =
            settings.titleFont?.colorMode === 'custom'
                ? settings.titleFont?.customColor
                : resolvedSchema ?? this.titleFontColor;
        this.titleFontFamily = settings.titleFont?.family || this.titleFontFamily;
        this.widgetNameFontSize = this.resolveWidgetNameFontSize(titleFontSizeSetting, this.widgetNameFontSize);

        if (settings.valueFont?.size) {
            this.valueFontSize = `${settings.valueFont.size}px`;
        }

        this.valueFontColor =
            settings.valueFont?.colorMode === 'custom'
                ? settings.valueFont?.customColor
                : resolvedSchema ?? this.valueFontColor;
        this.valueFontFamily = settings.valueFont?.family ?? this.valueFontFamily;
    }

    private prepareDataRange(): [DateTime, DateTime] {
        const state: DateRangeAndRefreshModelNew = this.cardWidgetConfiguration?.dateRange
            ? DateRangeAndRefreshModelNew.createItem(this.cardWidgetConfiguration.dateRange)
            : DateRangeAndRefreshModelNew.createItem('');
        var [startDate, endDate] = this._dateRangeService.getDateRangeFromNewState(state);

        if (!startDate || !endDate || startDate >= endDate) {
            [startDate, endDate] = this.dateRangeService.getDateRangeFromNewUnit(RefreshSelectionCustomUnits.Day, 30);
        }

        return [DateTime.fromJSDate(startDate), DateTime.fromJSDate(endDate)];
    }

    private roundValue(value: number): number {
        let roundTo = 2;
        if (
            this.parameter?.cardWidgetAdvancedSettings?.decimalPoints !== null &&
            this.parameter?.cardWidgetAdvancedSettings?.decimalPoints !== undefined
        ) {
            roundTo = this.parameter.cardWidgetAdvancedSettings.decimalPoints;
        }
        return Number.parseFloat(value.toFixed(roundTo));
    }

    private formatNumber(value: number, dataUnitType: DataUnitType) {
        const token = dataUnitType?.id !== 41 && dataUnitType?.id !== 255 ? this.l(dataUnitType.tokenCode) : '';

        if (value === 0 || dataUnitType.id === 18 || dataUnitType.id === 19) {
            // 18 19 is Percentage
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

    onNavigateToPage(): void {
        if (!this.navigationPageId) {
            return;
        }

        abp.event.trigger('app.dashboard.navigateToPage', this.navigationPageId);
    }

    override saveName(newName: string) {
        this.widgetDisplayName = newName;
        super.saveName(newName);
    }

    onWidgetClick(event: MouseEvent): void {
        if (this.editState || !this.canNavigateToPage) {
            return;
        }

        event.stopPropagation();
        this.onNavigateToPage();
    }

    private setNavigationTarget(): void {
        this.navigationPageId = this.parameter?.cardWidgetAdvancedSettings?.linkPage ?? null;
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
