import { Component, EventEmitter, Injector, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { WidgetConfigurationModalBaseComponent } from '../../widget-configuration-modal-base';
import { CardWidgetConfigurationsServiceProxy, CardWidgetStyleType, CreateOrEditCardWidgetConfigurationDto, CreateOrEditWidgetConfigurationDto, CustomParametersServiceProxy } from '@shared/service-proxies/service-proxies';
import { NgForm } from '@angular/forms';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { DxTabPanelComponent } from 'devextreme-angular';
import { ComponentsState } from '@app/shared/models/components-state';
import { DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { ColumnType } from '@app/shared/enums/column-type';
import { AddBaseParameterEventCallBack, EditBaseParameterEventCallBack } from '@app/shared/interfaces/base-parameter-event-callbacks';
import { Guid } from 'guid-ts';
import safeStringify from 'fast-safe-stringify';
import { AddCustomParameterEventCallBack, CardWidgetCustomParameterSelectionTabComponent, EditCustomParameterEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-custom-parameter-selection-tab/card-widget-custom-parameter-selection-tab.component';
import { AddCardWidgetExceptionEventCallBack, CardWidgetExceptionParameterSelectionTabComponent, EditCardWidgetExceptionEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-exception-parameter-selection-tab/card-widget-exception-parameter-selection-tab.component';
import { CardWidgetAdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-additional-parameter-selection-tab/card-widget-additional-parameter-selection-tab.component';
import { CardWidgetChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-channel-parameter-selection-tab/card-widget-channel-parameter-selection-tab.component';
import { CardWidgetLogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-logical-parameter-selection-tab/card-widget-logical-parameter-selection-tab.component';
import { AddCardWidgetEventParameterEventCallBack, CardWidgetEventParameterSelectionTabComponent, EditCardWidgetEventParameterEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/card-widget-selection-tabs/card-widget-event-parameter-selection-tab/card-widget-event-parameter-selection-tab.component';
import { finalize, Subscription , of, switchMap, tap} from 'rxjs';
import { CustomParameterService } from '@app/shared/services/custom-parameter-service.service';
import { CardWidgetConfigurationService } from '@app/shared/services/widget-configurations/card-widget-configuration.service';
import { EditableTabComponentBaseComponent } from '@app/shared/common/components/parameter-selection-tabs/editable-tab-component-base';
import { DateRangeAndRefreshModelNew } from '@app/shared/models/date-range-and-refresh-model-new';
import { DateRangeSelectorComponent } from '@app/shared/common/components/date-range-selector/date-range-selector.component';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { CardIconService } from '@app/shared/services/card-icon.service';

@Component({
    selector: 'createOrEditCardConfiguration',
    templateUrl: './create-or-edit-card-configuration.component.html',
    styleUrl: './create-or-edit-card-configuration.component.css',
})
export class CreateOrEditCardConfigurationComponent
    extends WidgetConfigurationModalBaseComponent
    implements OnInit, OnDestroy
{
    @ViewChild('cardWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab')
    customParameterSelectionTab: CardWidgetCustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab')
    logicalParameterSelectionTab: CardWidgetLogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab')
    channelParameterSelectionTab: CardWidgetChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab')
    additionalParameterSelectionTab: CardWidgetAdditionalParameterSelectionTabComponent;
    @ViewChild('exceptionParameterSelectionTab')
    exceptionParameterSelectionTab: CardWidgetExceptionParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab')
    eventParameterSelectionTab: CardWidgetEventParameterSelectionTabComponent;
    @ViewChild('dateRangeSelector') dateRangeSelector: DateRangeSelectorComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @Output() onSave = new EventEmitter<CreateOrEditCardWidgetConfigurationDto>();
    @Output() onClose = new EventEmitter<boolean>();

    saving = false;
    isEditMode = false;

    selectedCardStyle: CardWidgetStyleType;

    dateRangeSelectionState: DateRangeAndRefreshModelNew;

    refreshRateSelectionState: number = 0;
    parameters: WidgetParametersColumn[] = [];

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
    private cardWidgetConfiguration: CreateOrEditCardWidgetConfigurationDto =
        new CreateOrEditCardWidgetConfigurationDto();

    private subs: Subscription[] = [];

    constructor(
        injector: Injector,
        private cardWidgetServiceProxy: CardWidgetConfigurationsServiceProxy,
        private cardWidgetConfigurationService: CardWidgetConfigurationService,
        private _customParameterService: CustomParameterService,
        private cardIconService: CardIconService,
    ) {
        super(injector);
    }

    get isParameterSelectionDisabled(): boolean {
        return this.parameters.length >= 1 && !this.isEditMode;
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
                this.refreshRateSelectionState = -1;
                break;
        }
    }

    onDateRangeChangedNew() {
        this.refreshRateSelectionState = this.dateRangeSelectionState.refreshIntervalInSeconds;
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        const parameterName = this.applyCustomParameterName(event.parameter.name, event.cardWidgetAdvancedSettings);
        event.parameter.name = parameterName;
        this.parameters = [
            {
                id: Guid.newGuid().toString(),
                componentsState: event.componentsState,
                name: parameterName,
                quantity: event.quantity,
                type: ColumnType.BaseParameter,
                data: safeStringify(event.parameter),
                cardWidgetAdvancedSettings: event.cardWidgetAdvancedSettings,
                resolution: 0,
                style: null,
            },
        ];
    }

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            const parameterName = this.applyCustomParameterName(parameter.name, event.advancedSettings);
            this.parameters = [
                {
                    id: Guid.newGuid().toString(),
                    componentsState: event.componentsState,
                    name: parameterName,
                    quantity: event.quantity,
                    type: ColumnType.CustomParameter,
                    data: event.customParameterId,
                    cardWidgetAdvancedSettings: event.advancedSettings,
                    resolution: 0,
                    style: null,
                },
            ];
        });
        this.subs.push(sub);
    }

    onAddException(event: AddCardWidgetExceptionEventCallBack) {
        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            const parameterName = this.applyCustomParameterName(parameter.name, event.cardWidgetAdvancedSettings);
            this.parameters = [
                {
                    id: Guid.newGuid().toString(),
                    componentsState: null,
                    name: parameterName,
                    quantity: event.quantity,
                    type: ColumnType.Exception,
                    data: event.customParameterId,
                    resolution: 0,
                    cardWidgetAdvancedSettings: event.cardWidgetAdvancedSettings,
                    style: null,
                },
            ];
        });
        this.subs.push(sub);
    }

    onAddEvent(event: AddCardWidgetEventParameterEventCallBack) {
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
            cardWidgetAdvancedSettings: event.advancedSettings,
            style: null,
        };

        this.parameters = [newItem];
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.componentsState = event.componentsState;
            tableParameter.name = this.applyCustomParameterName(parameter.name, event.advancedSettings);
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.cardWidgetAdvancedSettings = event.advancedSettings;
        });
        this.subs.push(sub);
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        tableParameter.componentsState = event.componentsState;
        tableParameter.name = this.applyCustomParameterName(event.parameter.name, event.cardWidgetAdvancedSettings);
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify(event.parameter);
        tableParameter.cardWidgetAdvancedSettings = event.cardWidgetAdvancedSettings;
    }

    onEditException(event: EditCardWidgetExceptionEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        var sub = this._customParameterService.getById(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = this.applyCustomParameterName(parameter.name, event.cardWidgetAdvancedSettings);
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.cardWidgetAdvancedSettings = event.cardWidgetAdvancedSettings;
        });
        this.subs.push(sub);
    }

    onEditEvent(event: EditCardWidgetEventParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);
        if (!tableParameter) return;

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
        tableParameter.cardWidgetAdvancedSettings = event.advancedSettings;
    }

    onEditDeleteObject(event: string) {
        const index = this.parameters.findIndex((p) => p.id === event);
        if (index !== -1) {
            this.parameters.splice(index, 1);
        }
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
            var sub = this.cardWidgetConfigurationService
                .getForEdit(+configuration.configuration)
                .subscribe((result) => {
                    this.cardWidgetConfiguration = result.cardWidgetConfiguration;
                    this.dateRangeSelectionState = DateRangeAndRefreshModelNew.createItem(result.cardWidgetConfiguration.dateRange);
                    this.refreshRateSelectionState = result.cardWidgetConfiguration.refreshRate;
                    this.selectedCardStyle = result.cardWidgetConfiguration.styleType;
                    this.parameters = JSON.parse(result.cardWidgetConfiguration.parameters);
                    setTimeout(() => {
                        this.handleParameter(this.parameters[0], 'edit');
                        this.isEditMode = true;
                    }, 1000);
                });
            this.subs.push(sub);
        } else {
            this.cardWidgetConfiguration = new CreateOrEditCardWidgetConfigurationDto();
            this.dateRangeSelectionState = null;
            this.parameters = [];
            this.refreshRateSelectionState = 0;
            this.selectedCardStyle = null;
        }
    }

    isFormValid(): boolean {
        return (
            (this.dateRangeSelector?.isValid() ?? false) &&
            this.parameters?.length > 0 &&
            this.selectedCardStyle != null &&
            (this.selectedTab ? this.selectedTab.isFormValid() : true)
        );
    }

    save() {
        this.saving = true;
        const defaultIconId = this.extractDefaultIconId(this.parameters);
        const sanitizedParameters = this.sanitizeParameters(this.parameters);
        this.parameters = sanitizedParameters;

        this.cardWidgetConfiguration.dateRange = safeStringify(this.dateRangeSelectionState);
        this.cardWidgetConfiguration.parameters = safeStringify(this.parameters);
        this.cardWidgetConfiguration.refreshRate = this.refreshRateSelectionState;
        this.cardWidgetConfiguration.styleType = this.selectedCardStyle;

        const saveRequest = this.cardWidgetConfiguration.id
            ? this.cardWidgetServiceProxy.createOrEdit(this.cardWidgetConfiguration)
            : this.cardWidgetServiceProxy
                  .createAndGetId(this.cardWidgetConfiguration)
                  .pipe(tap((result) => (this.cardWidgetConfiguration.id = result)));

        const sub = (defaultIconId ? this.cardIconService.setDefaultIcon(defaultIconId) : of(null))
            .pipe(
                switchMap(() => saveRequest),
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe(() => {
                this.cardWidgetConfigurationService.update(this.cardWidgetConfiguration);
                this.hide(true);
                this.onSave.emit(this.cardWidgetConfiguration);
            });
        this.subs.push(sub);
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

    finishEdit() {
        this.selectedTab = null;
        this.disableEditMode();
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.additionalParameterSelectionTab.finishEdit();
        this.exceptionParameterSelectionTab.finishEdit();
        this.eventParameterSelectionTab.finishEdit();
    }

    private sanitizeParameters(parameters: WidgetParametersColumn[]): WidgetParametersColumn[] {
        return parameters.map((parameter) => {
            const icon = parameter.cardWidgetAdvancedSettings?.icon;
            if (!icon) {
                return parameter;
            }

            const sanitizedIcon = {
                ...icon,
                file: null,
                setAsDefault: undefined,
            };

            return {
                ...parameter,
                cardWidgetAdvancedSettings: {
                    ...parameter.cardWidgetAdvancedSettings,
                    icon: sanitizedIcon,
                } as CardWidgetAdvancedSettingsConfig,
            };
        });
    }

    private extractDefaultIconId(parameters: WidgetParametersColumn[]): number | null {
        for (const parameter of parameters) {
            const icon = parameter.cardWidgetAdvancedSettings?.icon;
            if (icon?.setAsDefault && icon.id) {
                return icon.id;
            }
        }

        return null;
    }

    ngOnDestroy(): void {
        this.subs.forEach((sub) => sub.unsubscribe());
    }
    private applyCustomParameterName(
        currentName: string,
        settings?: CardWidgetAdvancedSettingsConfig,
    ): string {
        return settings?.parameterName?.trim() || currentName;
    }
}
