import { Component, EventEmitter, HostBinding, Injector, Output, ViewChild, ViewEncapsulation } from '@angular/core';
import { NgForm } from '@angular/forms';
import {
    CreateOrEditTableWidgetConfigurationDto,
    CreateOrEditWidgetConfigurationDto,
    CustomParametersServiceProxy,
    TableWidgetConfigurationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import safeStringify from 'fast-safe-stringify';
import { finalize } from 'rxjs';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ComponentsState } from '@app/shared/models/components-state';
import {
    AddCustomParameterEventCallBack,
    CustomParameterSelectionTabComponent,
    EditCustomParameterEventCallBack,
} from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { ColumnType } from '@app/shared/enums/column-type';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import {
    AddBaseParameterEventCallBack,
    EditBaseParameterEventCallBack,
} from '@app/shared/interfaces/base-parameter-event-callbacks';
import {
    AddExceptionEventCallBack,
    EditExceptionEventCallBack,
    ExceptionParameterSelectionTabComponent,
} from '@app/shared/common/components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';
import {
    AddEventParameterEventCallBack,
    EditEventParameterEventCallBack,
    EventParameterSelectionTabComponent,
} from '@app/shared/common/components/parameter-selection-tabs/event-parameter-selection-tab/event-parameter-selection-tab.component';
import { DateRangeUnits } from '@app/shared/enums/date-range-selection-units';
import { DxDataGridComponent, DxDataGridTypes } from '@node_modules/devextreme-angular/ui/data-grid';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { Guid } from 'guid-ts';
import { LogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import { ChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import { DxScrollViewComponent, DxTabPanelComponent } from '@node_modules/devextreme-angular';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TableDesignOptions } from './table-design-options/table-design-options.component';
import { AdvancedSettingsConfig } from '@app/shared/common/components/parameter-selection-tabs/advanced-settings/advanced-settings.component';
import { AdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';

@Component({
    selector: 'createOrEditTableConfiguration',
    templateUrl: './create-or-edit-table-configuration.component.html',
    styleUrl: './create-or-edit-table-configuration.component.css',
    encapsulation: ViewEncapsulation.None,
})
export class CreateOrEditTableConfigurationComponent extends AppComponentBase {

    @ViewChild('tableWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab') customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab') logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab') channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab') additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    // @ViewChild('exceptionParameterSelectionTab') exceptionParameterSelectionTab: ExceptionParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab') eventParameterSelectionTab: EventParameterSelectionTabComponent;
    @ViewChild('tabPanel') tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave = new EventEmitter<CreateOrEditTableWidgetConfigurationDto>();

    saving = false;
    expandTags = true;

    dateRangeSelectionState: DateRangeState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null });
    componentsState: ComponentsState;
    parameters: WidgetParametersColumn[] = [];

    modalVisible = false;

    isVertical = true;
    designOptionsModalVisible = false;

    tableWidgetConfiguration: CreateOrEditTableWidgetConfigurationDto;
    designOptionsObj: TableDesignOptions = {
        headerBackgroundColor: '#ffffff',
        headerTextColor: '#333333',
        borderColor: '#cccccc',
        bandedRows: false,
        headerFontFamily: 'Arial, sans-serif',
        cellFontFamily: 'Arial, sans-serif'
    };

    tabs: any[] = [];

    private _allTabs = [
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
        // {
        //     ID: 5,
        //     name: 'Exception',
        //     template: 'exceptionTemplate',
        // },
        {
            ID: 5,
            name: 'Event',
            template: 'eventTemplate',
        },
    ];

    constructor(
        injector: Injector,
        private _tableWidgetConfigurationsServiceProxy: TableWidgetConfigurationsServiceProxy,
        private _customParameterServiceProxy: CustomParametersServiceProxy,
    ) {
        super(injector);
        this.tabs = this._allTabs;
    }

    isFormValid(): boolean {
        const isDateRangeValid =
            this.dateRangeSelectionState?.rangeOption &&
            (this.dateRangeSelectionState.rangeOption !== DateRangeUnits.CUSTOM ||
                (this.dateRangeSelectionState.startDate &&
                    this.dateRangeSelectionState.endDate &&
                    this.dateRangeSelectionState.startDate < this.dateRangeSelectionState.endDate));

        const isTableNotEmpty = this.parameters && this.parameters.length > 0;

        return isDateRangeValid && isTableNotEmpty;
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

    onComponentsStateChange() {
        this.componentsState = { ...this.componentsState };

        if (this.componentsState.feeders === undefined || this.componentsState.feeders === null || this.componentsState.feeders.length === 0) {
            this.parameters = this.parameters.filter(
                (p) =>
                    p.type === ColumnType.Event ||
                    (p.type === ColumnType.BaseParameter &&
                        (JSON.parse(p.data.toString()).type === BaseParameterType.Channel ||
                        JSON.parse(p.data.toString()).type === BaseParameterType.Additional)),
            );
        }

        this.updatePossibleTabs();
    }

    onDateRangeChanged(dateRangeState: DateRangeState) {
        this.tableWidgetConfiguration.dateRange = dateRangeState.toJSON();
    }

    onAddCustomParameter(event: AddCustomParameterEventCallBack) {
        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            const newItem = {
                id: Guid.newGuid().toString(),
                name: parameter.customParameter.name,
                quantity: event.quantity,
                type: ColumnType.CustomParameter,
                data: event.customParameterId,
                advancedSettings: event.advancedSettings,
                resolution:0
            };
            this.parameters = [newItem, ...this.parameters];
        });
    }

    onAddBaseParameter(event: AddBaseParameterEventCallBack) {
        const newItem = {
            id: Guid.newGuid().toString(),
            name: event.parameter.name,
            quantity: event.quantity,
            type: ColumnType.BaseParameter,
            data: safeStringify(event.parameter),
            advancedSettings: event.advancedSettings,
            resolution:0
        };

        this.parameters = [...this.parameters, newItem];
        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        setTimeout(() => {
            this.scrollDown();
        }, 100);
    }

    // onAddException(event: AddExceptionEventCallBack) {
    //     this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
    //         const newItem = {
    //             id: Guid.newGuid().toString(),
    //             name: parameter.customParameter.name,
    //             quantity: event.quantity,
    //             type: ColumnType.Exception,
    //             data: event.customParameterId,
    //         };

    //         this.parameters = [newItem, ...this.parameters];
    //     });
    // }

    onAddEvent(event: AddEventParameterEventCallBack) {
        const phaseNames = event.phases.map((phase) => phase).join(', ');
        const formattedName = `${event.event.name} (${phaseNames}) ${event.parameter}`;

        const newItem = {
            id: Guid.newGuid().toString(),
            name: formattedName,
            quantity: event.quantity,
            type: ColumnType.Event,
            resolution:0,
            data: safeStringify({
                event: event.event,
                phases: event.phases,
                parameter: event.parameter,
                isPolyphase: event.polyphase,
                aggregationInSeconds: event.aggregation.aggregationValue,
            }),
            advancedSettings: event.advancedSettings
        };

        this.parameters = [...this.parameters, newItem];

        this.grid.instance.pageIndex(this.grid.instance.pageCount());
        setTimeout(() => {
            this.scrollDown();
        }, 100);
    }

    onEditCustomParameter(event: EditCustomParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
            tableParameter.advancedSettings = event.advancedSettings;
        });
    }

    onEditBaseParameter(event: EditBaseParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        tableParameter.name = event.parameter.name;
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify(event.parameter);
        tableParameter.advancedSettings = event.advancedSettings;

    }

    onEditException(event: EditExceptionEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);

        if (!tableParameter) {
            return;
        }

        this._customParameterServiceProxy.getCustomParameterForView(event.customParameterId).subscribe((parameter) => {
            tableParameter.name = parameter.customParameter.name;
            tableParameter.quantity = event.quantity;
            tableParameter.data = event.customParameterId;
        });
    }

    onEditEvent(event: EditEventParameterEventCallBack) {
        const tableParameter: WidgetParametersColumn = this.parameters.find((p) => p.id === event.id);
        if (!tableParameter) return;

        const phaseNames = event.phases.map((phase) => phase).join(', ');
        tableParameter.name = `${event.event.name} (${phaseNames}) ${event.parameter}`;
        tableParameter.quantity = event.quantity;
        tableParameter.data = safeStringify({
            event: event.event,
            phases: event.phases,
            parameter: event.parameter,
            isPolyphase: event.polyphase,
            aggregationInSeconds: event.aggregation.aggregationValue,

        });
        tableParameter.advancedSettings = event.advancedSettings;
    }

    onDesignOptionsChange(newDesignOptions: TableDesignOptions) {
        this.designOptionsObj = { ...newDesignOptions };
        this.tableWidgetConfiguration.designOptions = JSON.stringify(this.designOptionsObj);
        console.log(this.designOptionsObj);
    }

    onRowRemoved() {
        this.parameters = [...this.parameters];
    }

    save() {
        this.saving = true;
        this.tableWidgetConfiguration.dateRange = this.dateRangeSelectionState.toJSON();
        this.tableWidgetConfiguration.configuration = safeStringify(this.parameters);
        this.tableWidgetConfiguration.components = safeStringify(this.componentsState);
        this.tableWidgetConfiguration.designOptions = JSON.stringify(this.designOptionsObj);

        if (this.tableWidgetConfiguration.id) {
            this._tableWidgetConfigurationsServiceProxy
                .createOrEdit(this.tableWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.hide();
                    this.onSave.emit(this.tableWidgetConfiguration);
                });
        } else {
            this._tableWidgetConfigurationsServiceProxy
                .createAndGetId(this.tableWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.tableWidgetConfiguration.id = result;
                    this.hide();
                    this.onSave.emit(this.tableWidgetConfiguration);
                });
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto) {
        this.modalVisible = true;
        if (configuration && configuration.configuration) {
            this._tableWidgetConfigurationsServiceProxy
                .getTableWidgetConfigurationForEdit(+configuration.configuration)
                .subscribe((result) => {
                    this.tableWidgetConfiguration = result.tableWidgetConfiguration;
                    this.dateRangeSelectionState = DateRangeState.fromJSON(result.tableWidgetConfiguration.dateRange);
                    this.componentsState = JSON.parse(result.tableWidgetConfiguration.components);
                    this.parameters = JSON.parse(result.tableWidgetConfiguration.configuration);
                    this.updatePossibleTabs();
                    if (this.tableWidgetConfiguration.designOptions) {
                        try {
                            this.designOptionsObj = JSON.parse(this.tableWidgetConfiguration.designOptions);
                        } catch (e) {
                            this.designOptionsObj = {
                                headerBackgroundColor: '#ffffff',
                                headerTextColor: '#333333',
                                borderColor: '#cccccc',
                                bandedRows: false,
                                headerFontFamily: 'Arial, sans-serif',
                                cellFontFamily: 'Arial, sans-serif'
                            };
                        }
                    }
                    setTimeout(() => {
                        this.scrollDown();
                    }, 100);
                });
        } else {
            this.tableWidgetConfiguration = new CreateOrEditTableWidgetConfigurationDto();
            this.dateRangeSelectionState = new DateRangeState({
                rangeOption: DateRangeUnits.LAST_30_DAYS,
                startDate: null,
                endDate: null,
            });
            this.parameters = [];
            this.componentsState = null;
            this.designOptionsObj = {
                headerBackgroundColor: '#ffffff',
                headerTextColor: '#333333',
                borderColor: '#cccccc',
                bandedRows: false,
                headerFontFamily: 'Arial, sans-serif',
                cellFontFamily: 'Arial, sans-serif'
            };
        }
    }

    hide(): void {
        this.customParameterSelectionTab.finishEdit();
        this.logicalParameterSelectionTab.finishEdit();
        this.channelParameterSelectionTab.finishEdit();
        this.additionalParameterSelectionTab.finishEdit();
        // this.exceptionParameterSelectionTab.finishEdit();
        this.eventParameterSelectionTab.finishEdit();
        this.modalVisible = false;
    }

    updateParameter(event: DxDataGridTypes.EditingStartEvent) {
        event.cancel = true; // disables default behavior of component, DO NOT REMOVE
        this.handleParameter(event.data, 'edit');
    }

    duplicateParameterCommand = (e: DxDataGridTypes.ColumnButtonClickEvent) => {
        const parameter = e.row.data as WidgetParametersColumn;
        this.handleParameter(parameter, 'duplicate');
    };

    private updatePossibleTabs() {
        this.tabs =  this.componentsState?.feeders && this.componentsState.feeders.length > 0
            ? this._allTabs
            : this._allTabs.filter(t => t.ID === 3 || t.ID === 4 || t.ID === 5);
    }

    private handleParameter(data: WidgetParametersColumn, action: 'edit' | 'duplicate') {
        const populateOrEdit = (tab: any) => {
            if (action === 'edit') {
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
                this.tabPanel.selectedIndex = this.tabs.findIndex(t => t.ID === 1);
                populateOrEdit(this.customParameterSelectionTab);
                break;

            case ColumnType.BaseParameter:
                const parameter: Parameter = JSON.parse(data.data.toString());
                if (parameter?.type) {
                    switch (parameter.type) {
                        case BaseParameterType.Logical:
                            this.tabPanel.selectedIndex = this.tabs.findIndex(t => t.ID === 2);
                            populateOrEdit(this.logicalParameterSelectionTab);
                            break;
                        case BaseParameterType.Channel:
                            this.tabPanel.selectedIndex = this.tabs.findIndex(t => t.ID === 3);
                            populateOrEdit(this.channelParameterSelectionTab);
                            break;
                        case BaseParameterType.Additional:
                            this.tabPanel.selectedIndex = this.tabs.findIndex(t => t.ID === 4);
                            populateOrEdit(this.additionalParameterSelectionTab);
                            break;
                    }
                }
                break;

            // case ColumnType.Exception:
            //     this.tabPanel.selectedIndex = 4;
            //     populateOrEdit(this.exceptionParameterSelectionTab);
            //     break;

            case ColumnType.Event:
                this.tabPanel.selectedIndex = this.tabs.findIndex(t => t.ID === 5);
                populateOrEdit(this.eventParameterSelectionTab);
                break;
        }
    }

    private scrollDown() {
        this.scrollView.instance.scrollTo(10000);
    }
}
