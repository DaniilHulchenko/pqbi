import { Component, Output, EventEmitter, Injector, OnInit, ViewChild } from '@angular/core';
import { WidgetConfigurationModalBaseComponent } from '../../widget-configuration-modal-base';
import {
    BarChartWidgetConfigurationsServiceProxy,
    ComponentDto,
    CreateOrEditBarChartWidgetConfigurationDto,
    CreateOrEditWidgetConfigurationDto,
    CustomParametersServiceProxy,
    EventClassDescription,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs';
import safeStringify from 'fast-safe-stringify';
import { NgForm } from '@angular/forms';
import { DateRangeState } from '@app/shared/models/date-range-state';
import { ComponentsState } from '@app/shared/models/components-state';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { DxDataGridComponent, DxScrollViewComponent, DxTabPanelComponent } from '@node_modules/devextreme-angular';
import { AdditionalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';
import { LogicalParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import { AddCustomParameterEventCallBack, CustomParameterSelectionTabComponent, EditCustomParameterEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { ChannelParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import { AddEventParameterEventCallBack, EditEventParameterEventCallBack, EventParameterSelectionTabComponent } from '@app/shared/common/components/parameter-selection-tabs/event-parameter-selection-tab/event-parameter-selection-tab.component';
import { Guid } from 'guid-ts';
import { AddBaseParameterEventCallBack, EditBaseParameterEventCallBack } from '@app/shared/interfaces/base-parameter-event-callbacks';
import { ColumnType } from '@app/shared/enums/column-type';
import { EditExceptionEventCallBack } from '@app/shared/common/components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';

@Component({
    selector: 'createOrEditBarChartConfiguration',
    templateUrl: './create-or-edit-bar-chart-configuration.component.html',
    styleUrl: './create-or-edit-bar-chart-configuration.component.css',
})
export class CreateOrEditBarChartConfigurationComponent
    extends WidgetConfigurationModalBaseComponent
    implements OnInit {
    @ViewChild('barChartWidgetConfigurationForm') pqsForm: NgForm;
    @ViewChild('customParameterSelectionTab')    customParameterSelectionTab: CustomParameterSelectionTabComponent;
    @ViewChild('logicalParameterSelectionTab')   logicalParameterSelectionTab: LogicalParameterSelectionTabComponent;
    @ViewChild('channelParameterSelectionTab')   channelParameterSelectionTab: ChannelParameterSelectionTabComponent;
    @ViewChild('additionalParameterSelectionTab') additionalParameterSelectionTab: AdditionalParameterSelectionTabComponent;
    @ViewChild('eventParameterSelectionTab')     eventParameterSelectionTab: EventParameterSelectionTabComponent;
    @ViewChild('tabPanel')                       tabPanel: DxTabPanelComponent;
    @ViewChild('scrollView') scrollView: DxScrollViewComponent;
    @ViewChild('grid') grid: DxDataGridComponent;
    @Output() onSave = new EventEmitter<CreateOrEditBarChartWidgetConfigurationDto>();

    saving = false;
    expandTags = true;

    barChartWidgetConfiguration: CreateOrEditBarChartWidgetConfigurationDto =
        new CreateOrEditBarChartWidgetConfigurationDto();

    componentsState: ComponentsState

    parameters: WidgetParametersColumn[] = [];

    tabs = [
        { ID: 1, name: 'Custom',   template: 'customTemplate'   },
        { ID: 2, name: 'Logical',  template: 'logicalTemplate'  },
        { ID: 3, name: 'Channel',  template: 'channelTemplate'  },
        { ID: 4, name: 'Additional', template: 'additionalTemplate' },
        { ID: 5, name: 'Event',    template: 'eventTemplate'    },
    ];

    selectedEvents: EventClassDescription[];
    events: EventClassDescription[];

    dateRangeSelectionState: DateRangeState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null});

    constructor(
        injector: Injector,
        private _barChartConfigurationService: BarChartWidgetConfigurationsServiceProxy,
        private _customParameterServiceProxy: CustomParametersServiceProxy,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy,
    ) {
        super(injector);
    }

    ngOnInit(): void {
        this._pqsRestApiServiceProxy.pQSEvents().subscribe((result) => {
            this.events = result;
        });
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

    isFormValid() {
        return this.pqsForm?.form?.valid ?? false;
    }

    changeExpandState() {
        this.expandTags = !this.expandTags;
    }

    onDateRangeChanged(dateRangeState: DateRangeState) {
        this.barChartWidgetConfiguration.dateRange = dateRangeState.toJSON();
    }

    onComponentsStateChange() {
        this.componentsState = { ...this.componentsState };
    }

    onEventsChanged() {
        this.barChartWidgetConfiguration.events = safeStringify(this.selectedEvents);
    }

    save() {
        this.saving = true;
        if (this.barChartWidgetConfiguration.id) {
            this._barChartConfigurationService
                .createOrEdit(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        } else {
            this._barChartConfigurationService
                .createAndGetId(this.barChartWidgetConfiguration)
                .pipe(
                    finalize(() => {
                        this.saving = false;
                    }),
                )
                .subscribe((result) => {
                    this.barChartWidgetConfiguration.id = result;
                    this.close();
                    this.onSave.emit(this.barChartWidgetConfiguration);
                });
        }
    }

    show(configuration: CreateOrEditWidgetConfigurationDto) {
        this.open();
        if (configuration && configuration.configuration) {
            this._barChartConfigurationService
                .getBarChartWidgetConfigurationForEdit(+configuration.configuration)
                .subscribe((result) => {
                    this.barChartWidgetConfiguration = result.barChartWidgetConfiguration;
                    this.dateRangeSelectionState = DateRangeState.fromJSON(this.barChartWidgetConfiguration.dateRange);

                    this.componentsState = JSON.parse(this.barChartWidgetConfiguration.components);

                    this.selectedEvents = JSON.parse(this.barChartWidgetConfiguration.events);
                });
        } else {
            this.barChartWidgetConfiguration = new CreateOrEditBarChartWidgetConfigurationDto();
            this.dateRangeSelectionState = new DateRangeState({ rangeOption: null, startDate: null, endDate: null});
            this.componentsState = null;
            this.selectedEvents = null;
        }
    }

    private scrollDown() {
        this.scrollView.instance.scrollTo(10000);
    }
}
