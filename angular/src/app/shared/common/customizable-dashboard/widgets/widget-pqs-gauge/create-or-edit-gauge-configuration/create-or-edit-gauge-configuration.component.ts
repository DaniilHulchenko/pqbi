import { Component, EventEmitter, Injector, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { NgForm } from '@angular/forms';
import { CreateOrEditGaugeWidgetConfigurationDto, CreateOrEditWidgetConfigurationDto, CustomParametersServiceProxy, GaugeWidgetConfigurationsServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetConfigurationModalBaseComponent } from '../../widget-configuration-modal-base';
import { DxTabPanelComponent } from 'devextreme-angular';
import { AddBaseParameterEventCallBack, EditBaseParameterEventCallBack } from '@app/shared/interfaces/base-parameter-event-callbacks';
import { Guid } from 'guid-ts';
import { ColumnType } from '@app/shared/enums/column-type';
import { DxDataGridTypes } from 'devextreme-angular/ui/data-grid';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { finalize, Subscription } from 'rxjs';
import { ComponentsState } from '@app/shared/models/components-state';
import safeStringify from 'fast-safe-stringify';
import { AddGaugeCustomParameterEventCallBack, EditGaugeCustomParameterEventCallBack, GaugeWidgetCustomParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-custom-parameter-selection-tab/gauge-widget-custom-parameter-selection-tab.component';
import { GaugeWidgetLogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-logical-parameter-selection-tab/gauge-widget-logical-parameter-selection-tab.component';
import { GaugeWidgetChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-channel-parameter-selection-tab/gauge-widget-channel-parameter-selection-tab.component';
import { GaugeWidgetAdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-additional-parameter-selection-tab/gauge-widget-additional-parameter-selection-tab.component';
import { AddGaugeWidgetExceptionEventCallBack, EditGaugeWidgetExceptionEventCallBack, GaugeWidgetExceptionParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-exception-parameter-selection-tab/gauge-widget-exception-parameter-selection-tab.component';
import { AddGaugeWidgetEventParameterEventCallBack, EditGaugeWidgetEventParameterEventCallBack, GaugeWidgetEventParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/gauge-widget-selection-tabs/gauge-widget-event-parameter-selection-tab/gauge-widget-event-parameter-selection-tab.component';
import { GaugeStyle } from '@app/shared/interfaces/gauge-style';
import { GaugeWidgetStyleSelectorComponent } from '@app/shared/common/components/gauge-widget-style-selector/gauge-widget-style-selector.component';
import { WidgetRefreshSelectorComponent } from '@app/shared/common/components/widget-refresh-selector/widget-refresh-selector.component';
import { CustomParameterService } from '@app/shared/services/custom-parameter-service.service';
import { GaugeWidgetConfigurationService } from '@app/shared/services/widget-configurations/gauge-widget-configuration.service';
import { EditableTabComponentBaseComponent } from '@app/shared/common/components/parameter-selection-tabs/editable-tab-component-base';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateRangeSelectorComponent } from '@app/shared/common/components/date-range-selector/date-range-selector.component';

@Component({
    selector: 'createOrEditGaugeConfiguration',
    templateUrl: './create-or-edit-gauge-configuration.component.html',
    styleUrl: './create-or-edit-gauge-configuration.component.css',
})
export class CreateOrEditGaugeConfigurationComponent
    extends WidgetConfigurationModalBaseComponent
    implements OnInit, OnDestroy
{
    @ViewChild('gaugeWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab')
    customParameterSelectionTab: GaugeWidgetCustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab')
    logicalParameterSelectionTab: GaugeWidgetLogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab')
    channelParameterSelectionTab: GaugeWidgetChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab')
    additionalParameterSelectionTab: GaugeWidgetAdditionalParameterSelectionTabComponent;
    @ViewChild('exceptionParameterSelectionTab')
    exceptionParameterSelectionTab: GaugeWidgetExceptionParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab')
    eventParameterSelectionTab: GaugeWidgetEventParameterSelectionTabComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @ViewChild('styleSelector') styleSelector: GaugeWidgetStyleSelectorComponent;
    @ViewChild('refreshSelector') refreshSelector: WidgetRefreshSelectorComponent;
    @Output() onSave = new EventEmitter<CreateOrEditGaugeWidgetConfigurationDto>();
    @ViewChild('dateRangeSelector') dateRangeSelector: DateRangeSelectorComponent;
    @Output() onClose = new EventEmitter<boolean>();

    saving = false;
    isEditMode = false;

    selectedStyle: GaugeStyle | null = null;

    dateRangeSelectionState: DateRangeAndRefreshModelNew;

    refreshRateSelectionState: number = 0;
    parameter: WidgetParametersColumn;

    previousTabIndex: number | null = null;
    selectedTabIndex: number | null = 0;
    selectedTab: EditableTabComponentBaseComponent | null = null;
    tabs = [
        {
            ID: 1,
            name: 'Custom',
            template: 'customTemplate',
        },
        {
            ID: 2,
            name: 'Logical',
            template: 'logicalTemplate',
        },
        {
            ID: 3,
            name: 'Channel',
            template: 'channelTemplate',
        },
        {
            ID: 4,
            name: 'Additional',
            template: 'additionalTemplate',
        },
        {
            ID: 5,
            name: 'Exception',
            template: 'exceptionTemplate',
        },
        {
            ID: 6,
            name: 'Event',
            template: 'eventTemplate',
        },
    ];
    private gaugeWidgetConfiguration: CreateOrEditGaugeWidgetConfigurationDto =
        new CreateOrEditGaugeWidgetConfigurationDto();

    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        private gaugeWidgetServiceProxy: GaugeWidgetConfigurationsServiceProxy,
        private gaugeWidgetConfigurationService: GaugeWidgetConfigurationService,
        private _customParameterService: CustomParameterService,
    ) {
        super(injector);
    }

    get isParameterSelectionDisabled(): boolean {
        return this.parameter && !this.isEditMode;
    }

    ngOnInit(): void {}

    onDateRangeChanged(dateRange: DateRangeState) {
        switch (dateRange.rangeOption) {
            case DateRangeUnits.LAST_HOUR:
                this.refreshRateSelectionState = 5 * 60; // 5 minutes
                break;
            case DateRangeUnits.TODAY:
            case DateRangeUnits.LAST_24_HOURS:
                this.refreshRateSelectionState = 60 * 60; // 1 hour
                break;
            case DateRangeUnits.THIS_WEEK:
            case DateRangeUnits.LAST_7_DAYS:
                this.refreshRateSelectionState = 12 * 60 * 60; // 12 hours
                break;
            case DateRangeUnits.THIS_MONTH:
            case DateRangeUnits.LAST_30_DAYS:
                this.refreshRateSelectionState = 24 * 60 * 60; // 24 hours
                break;
            case DateRangeUnits.CUSTOM:
                this.refreshRateSelectionState = 0;
                setTimeout(() => {
                    this.refreshRateSelectionState = -1;
                    this.refreshSelector.isDisabled = true;
                }, 0);
                break;
        }
    }

    onDateRangeChangedNew() {
        this.refreshRateSelectionState = this.dateRangeSelectionState.refreshIntervalInSeconds;
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        const parameterName = this.applyCustomParameterName(event.parameter.name, event.gaugeWidgetAdvancedSettings);
        event.parameter.name = parameterName;
        this.parameter = {
            id: Guid.newGuid().toString(),
            componentsState: event.componentsState,
            name: parameterName,
            quantity: event.quantity,
            type: ColumnType.BaseParameter,
            data: safeStringify(event.parameter),
            gaugeWidgetAdvancedSettings: event.gaugeWidgetAdvancedSettings,
            resolution: 0,
            style: null,
        };
    }

    onAddCustomParameter(event: AddGaugeCustomParameterEventCallBack) {
        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            const parameterName = this.applyCustomParameterName(parameter.name, event.advancedSettings);
            this.parameter = {
                id: Guid.newGuid().toString(),
                componentsState: event.componentsState,
                name: parameterName,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                gaugeWidgetAdvancedSettings: event.advancedSettings,
                resolution: 0,
                style: null,
            };
            this.parameter.gaugeWidgetAdvancedSettings = event.advancedSettings;
        });
        this.subs.push(sub);
    }

    onAddException(event: AddGaugeWidgetExceptionEventCallBack) {
        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            const parameterName = this.applyCustomParameterName(parameter.name, event.gaugeWidgetAdvancedSettings);
            this.parameter = {
                id: Guid.newGuid().toString(),
                componentsState: null,
                name: parameterName,
                quantity: event.quantity,
                type: ColumnType.Exception,
                data: event.customParameterId,
                resolution: 0,
                gaugeWidgetAdvancedSettings: event.gaugeWidgetAdvancedSettings,
                style: null,
            };
        });
        this.subs.push(sub);
    }

    onAddEvent(event: AddGaugeWidgetEventParameterEventCallBack) {
        const phaseNames = event.phases.map((phase) => phase).join(', ');
        const formattedName = `${event.event.name} (${phaseNames}) ${event.parameter}`;
        const parameterName = this.applyCustomParameterName(formattedName, event.advancedSettings);

        const newItem = {
            id: Guid.newGuid().toString(),
            componentsState: event.componentState,
            name: parameterName,
            quantity: event.quantity,
            type: ColumnType.Event,
            resolution: 0,
            data: safeStringify({
                event: event.event,
                phases: event.phases,
                parameter: event.parameter,
                isPolyphase: event.polyphase,
                aggregationInSeconds: event.aggregation.aggregationValue,
            }),
            gaugeWidgetAdvancedSettings: event.advancedSettings,
            style: null,
        };

        this.parameter = newItem;
    }

    onEditCustomParameter(event: EditGaugeCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameter;

        if (!tableParameter || tableParameter.type !== ColumnType.CustomParameter) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.componentsState = event.componentsState;
            tableParameter.name = this.applyCustomParameterName(parameter.name, event.advancedSettings);
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.gaugeWidgetAdvancedSettings = event.advancedSettings;
        });
        this.subs.push(sub);
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameter;

        if (!tableParameter || tableParameter.type !== ColumnType.BaseParameter) {
            return;
        }

        tableParameter.componentsState = event.componentsState;
        tableParameter.name = this.applyCustomParameterName(event.parameter.name, event.gaugeWidgetAdvancedSettings);
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify(event.parameter);
        tableParameter.gaugeWidgetAdvancedSettings = event.gaugeWidgetAdvancedSettings;
    }

    onEditException(event: EditGaugeWidgetExceptionEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameter;

        if (!tableParameter || tableParameter.type !== ColumnType.Exception) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = this.applyCustomParameterName(parameter.name, event.gaugeWidgetAdvancedSettings);
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.gaugeWidgetAdvancedSettings = event.gaugeWidgetAdvancedSettings;
        });
        this.subs.push(sub);
    }

    onEditEvent(event: EditGaugeWidgetEventParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameter;
        if (!tableParameter || tableParameter.type !== ColumnType.Event) return;

        const phaseNames = event.phases.map((phase) => phase).join(', ');
        tableParameter.name = this.applyCustomParameterName(
            `${event.event.name} (${phaseNames}) ${event.parameter}`,
            event.advancedSettings,
        );
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify({
            event: event.event,
            phases: event.phases,
            parameter: event.parameter,
            isPolyphase: event.polyphase,
            aggregationInSeconds: event.aggregation.aggregationValue,
        });
        tableParameter.gaugeWidgetAdvancedSettings = event.advancedSettings;
    }

    onEditDeleteObject(event: string) {
        if (this.parameter) {
            this.parameter = null;
        }
    }

    removeParameter(event: DxDataGridTypes.RowRemovingEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.parameter = null;
        this.finishEdit();
    }

    updateParameter(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.handleParameter(event.data, 'edit');
        this.isEditMode = true;
    }

    private handleParameter(data: WidgetParametersColumn, action: 'edit' | 'duplicate') {
        const populateOrEdit = (tab: any) => {
            if (action === 'edit') {
                this.selectedTab = tab;
                tab.edit(data);
            } else {
                if (tab.isEdit) {
                    tab.isEdit = false;
                }
                tab.populateForm(data);
            }
        };

        switch (data.type) {
            case ColumnType.CustomParameter:
                this.tabPanel.selectedIndex = 0;
                populateOrEdit(this.customParameterSelectionTab);
                break;

            case ColumnType.BaseParameter:
                const parameter: Parameter = JSON.parse(data.data.toString());
                if (parameter?.type) {
                    switch (parameter.type) {
                        case BaseParameterType.Logical:
                            this.tabPanel.selectedIndex = 1;
                            populateOrEdit(this.logicalParameterSelectionTab);
                            break;
                        case BaseParameterType.Channel:
                            this.tabPanel.selectedIndex = 2;
                            populateOrEdit(this.channelParameterSelectionTab);
                            break;
                        case BaseParameterType.Additional:
                            this.tabPanel.selectedIndex = 3;
                            populateOrEdit(this.additionalParameterSelectionTab);
                            break;
                    }
                }
                break;

            case ColumnType.Exception:
                this.tabPanel.selectedIndex = 4;
                populateOrEdit(this.exceptionParameterSelectionTab);
                break;
            case ColumnType.Event:
                this.tabPanel.selectedIndex = 5;
                populateOrEdit(this.eventParameterSelectionTab);
                break;
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto) {
        this.open();
        if (configuration && configuration.configuration) {
            var sub = this.gaugeWidgetConfigurationService
                .getForEdit(+configuration.configuration)
                .subscribe((result) => {
                    this.gaugeWidgetConfiguration = result.gaugeWidgetConfiguration;
                    this.dateRangeSelectionState = DateRangeAndRefreshModelNew.createItem(result.gaugeWidgetConfiguration.dateRange);
                    this.parameter = JSON.parse(result.gaugeWidgetConfiguration.parameter);
                    this.refreshRateSelectionState = result.gaugeWidgetConfiguration.refreshRate;
                    this.selectedStyle = JSON.parse(result.gaugeWidgetConfiguration.style) as GaugeStyle;
                    setTimeout(() => {
                        this.handleParameter(this.parameter, 'edit');
                        this.isEditMode = true;
                    }, 1000);
                });
            this.subs.push(sub);
        } else {
            this.gaugeWidgetConfiguration = new CreateOrEditGaugeWidgetConfigurationDto();
            this.dateRangeSelectionState = null;
            this.parameter = null;
            this.refreshRateSelectionState = 0;
            this.selectedStyle = null;
        }
    }

    isFormValid(): boolean {
        return (
            (this.dateRangeSelector?.isValid() ?? false) &&
            this.parameter &&
            this.styleSelector.isValidState() &&
            (this.selectedTab ? this.selectedTab.isFormValid() : true)
        );
    }

    save() {
        this.saving = true;
        this.gaugeWidgetConfiguration.dateRange = safeStringify(this.dateRangeSelectionState);
        this.gaugeWidgetConfiguration.parameter = safeStringify(this.parameter);
        this.gaugeWidgetConfiguration.refreshRate = this.refreshRateSelectionState;
        this.gaugeWidgetConfiguration.style = safeStringify(this.selectedStyle);

        if (this.gaugeWidgetConfiguration.id) {
            this.gaugeWidgetServiceProxy
                .createOrEdit(this.gaugeWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.gaugeWidgetConfigurationService.update(this.gaugeWidgetConfiguration);
                    this.hide(true);
                    this.onSave.emit(this.gaugeWidgetConfiguration);
                });
        } else {
            this.gaugeWidgetServiceProxy
                .createAndGetId(this.gaugeWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.gaugeWidgetConfiguration.id = result;
                    this.gaugeWidgetConfigurationService.update(this.gaugeWidgetConfiguration);
                    this.hide(true);
                    this.onSave.emit(this.gaugeWidgetConfiguration);
                });
        }
    }

    onTabSelectionChanging(e: any) {
        if (this.isEditMode) {
            e.cancel = true;
            this.notify.info(this.l('CannotChangeTabTitle'));
            return;
        }
        this.previousTabIndex = e.component.option('selectedIndex');
    }

    onTabSelectionChanged(e: any) {
        const tabMapping: Record<number, any> = {
            0: this.customParameterSelectionTab,
            1: this.logicalParameterSelectionTab,
            2: this.channelParameterSelectionTab,
            3: this.additionalParameterSelectionTab,
        };

        const previousTab = tabMapping[this.previousTabIndex];
        const selectedTab = tabMapping[this.tabPanel.selectedIndex];

        if (previousTab && selectedTab && previousTab !== selectedTab) {
            selectedTab.populateComponentsFromTab(previousTab);
        }
    }

    getIconClass(tabID: number): string {
        const icons = {
            1: 'fa-cogs',
            2: 'fa-brain',
            3: 'fa-project-diagram',
            4: 'fa-plus',
            5: 'fa-exclamation-circle',
        };

        return icons[tabID] || 'fa-question-circle';
    }

    componentsToString(componentsState: ComponentsState): string {
        return componentsState?.components?.map((component) => component.label).join(', ') ?? '';
    }

    disableEditMode() {
        this.isEditMode = false;
    }

    hide(isSaved: boolean): void {
        this.finishEdit();
        this.close();
        this.onClose.emit(isSaved);
    }

    private finishEdit() {
        this.selectedTab = null;
        this.disableEditMode();
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.additionalParameterSelectionTab.finishEdit();
        this.exceptionParameterSelectionTab.finishEdit();
        this.eventParameterSelectionTab.finishEdit();
    }

    ngOnDestroy(): void {
        this.subs.forEach((sub) => sub.unsubscribe());
    }

    private applyCustomParameterName(
        currentName: string,
        settings?: GaugeWidgetAdvancedSettingsConfig,
    ): string {
        return settings?.parameterName?.trim() || currentName;
    }
}
