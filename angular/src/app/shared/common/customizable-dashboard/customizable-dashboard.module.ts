import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { CustomizableDashboardComponent } from '@app/shared/common/customizable-dashboard/customizable-dashboard.component';
import { WidgetGeneralStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-general-stats/widget-general-stats.component';
import { DashboardViewConfigurationService } from '@app/shared/common/customizable-dashboard/dashboard-view-configuration.service';
import { GridsterModule } from 'angular-gridster2';
import { WidgetDailySalesComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-daily-sales/widget-daily-sales.component';
import { WidgetEditionStatisticsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-edition-statistics/widget-edition-statistics.component';
import { WidgetHostTopStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-host-top-stats/widget-host-top-stats.component';
import { WidgetIncomeStatisticsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-income-statistics/widget-income-statistics.component';
import { WidgetMemberActivityComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-member-activity/widget-member-activity.component';
import { WidgetProfitShareComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-profit-share/widget-profit-share.component';
import { WidgetRecentTenantsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-recent-tenants/widget-recent-tenants.component';
import { WidgetRegionalStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-regional-stats/widget-regional-stats.component';
import { WidgetSalesSummaryComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-sales-summary/widget-sales-summary.component';
import { WidgetSubscriptionExpiringTenantsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-subscription-expiring-tenants/widget-subscription-expiring-tenants.component';
import { WidgetTopStatsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-top-stats/widget-top-stats.component';
import { FilterDateRangePickerComponent } from '@app/shared/common/customizable-dashboard/filters/filter-date-range-picker/filter-date-range-picker.component';
import { AddWidgetModalComponent } from '@app/shared/common/customizable-dashboard/add-widget-modal/add-widget-modal.component';
import { PieChartModule, AreaChartModule, LineChartModule, BarChartModule } from '@swimlane/ngx-charts';
import { WidgetComponentBaseComponent } from './widgets/widget-component-base';
import { UtilsModule } from '@shared/utils/utils.module';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { DropdownModule } from 'primeng/dropdown';
import { ModalModule } from 'ngx-bootstrap/modal';
import { FormsModule } from '@angular/forms';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { PerfectScrollbarModule } from '@craftsjs/perfect-scrollbar';
import { AppBsModalModule } from '@shared/common/appBsModal/app-bs-modal.module';
import { Angular2CountoModule } from '@awaismirza/angular2-counto';
import { TableModule } from 'primeng/table';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { SubheaderModule } from '../sub-header/subheader.module';
import { WidgetPQSComponent } from './widgets/widget-pqs/widget-pqs.component';
import { CreateOrEditTrendConfigurationComponent } from './widgets/widget-pqs/create-or-edit-trend-configuration/create-or-edit-trend-configuration.component';
import { TreeSelectModule } from 'primeng/treeselect';
import { OrderListModule } from 'primeng/orderlist';
import { ScrollPanelModule } from 'primeng/scrollpanel';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';
import { AccordionModule } from 'primeng/accordion';
import { PickListModule } from 'primeng/picklist';
import { SplitterModule } from 'primeng/splitter';
import { DynamicTreeBuilderComponent } from '../components/dynamic-tree-builder/dynamic-tree-builder.component';
import { WidgetPQSTableComponent } from './widgets/widget-pqs-table/widget-pqs-table.component';
import { TreeTableModule } from 'primeng/treetable';
import {
  DxChartModule,
  DxPivotGridModule,
  DxRadioGroupModule,
  DxPopupModule,
  DxScrollViewModule,
  DxButtonModule,
  DxDataGridModule,
  DxTabPanelModule,
  DxLoadIndicatorModule, DxTextBoxModule,
  DxTreeListModule,
  DxSelectBoxModule,
  DxNumberBoxModule,
  DxCheckBoxModule,
  DxColorBoxModule
} from 'devextreme-angular';
import { WidgetPqsBarChartComponent } from './widgets/widget-pqs-bar-chart/widget-pqs-bar-chart.component';
import { CreateOrEditBarChartConfigurationComponent } from './widgets/widget-pqs-bar-chart/create-or-edit-bar-chart-configuration/create-or-edit-bar-chart-configuration.component';
import { CreateOrEditTableConfigurationComponent } from './widgets/widget-pqs-table/create-or-edit-table-configuration/create-or-edit-table-configuration.component';
import { TreeModule } from 'primeng/tree';
import { RadioButtonModule } from 'primeng/radiobutton';
import { ToastModule } from 'primeng/toast';
import { ContextMenuModule } from 'primeng/contextmenu';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatetimeRangeSelectorComponent } from '../components/datetime-range-selector/datetime-range-selector.component';
import { ResolutionSelectorComponent } from '../components/resolution-selector/resolution-selector.component';
import { ComponentsSelectorComponent } from '../components/components-selector/components-selector.component';
import { CustomParameterSelectionTabComponent } from '../components/parameter-selection-tabs/custom-parameter-selection-tab/custom-parameter-selection-tab.component';
import { ExceptionParameterSelectionTabComponent } from '../components/parameter-selection-tabs/exception-parameter-selection-tab/exception-parameter-selection-tab.component';
import { LogicalParameterSelectionTabComponent } from '../components/parameter-selection-tabs/logical-parameter-selection-tab/logical-parameter-selection-tab.component';
import { ChannelParameterSelectionTabComponent } from '../components/parameter-selection-tabs/channel-parameter-selection-tab/channel-parameter-selection-tab.component';
import { EventParameterSelectionTabComponent } from '../components/parameter-selection-tabs/event-parameter-selection-tab/event-parameter-selection-tab.component';
import {
    RenameWidgetModalComponent
} from '@app/shared/common/customizable-dashboard/rename-widget-modal/rename-widget-modal.component';
import { TablePreviewComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-pqs-table/create-or-edit-table-configuration/table-preview/table-preview.component';
import { TableDesignOptionsComponent } from '@app/shared/common/customizable-dashboard/widgets/widget-pqs-table/create-or-edit-table-configuration/table-design-options/table-design-options.component';
import { AdditionalParameterSelectionTabComponent } from '../components/parameter-selection-tabs/additional-parameter-selection-tab/additional-parameter-selection-tab.component';
import { BarChartPreviewComponent } from "./widgets/widget-pqs-bar-chart/create-or-edit-bar-chart-configuration/bar-chart-preview/bar-chart-preview.component";

@NgModule({
    imports: [
    CommonModule,
    FormsModule,
    UtilsModule,
    GridsterModule,
    PieChartModule,
    AreaChartModule,
    LineChartModule,
    BarChartModule,
    BsDropdownModule,
    DropdownModule,
    ModalModule,
    TabsModule,
    PerfectScrollbarModule,
    AppBsModalModule,
    Angular2CountoModule,
    TableModule,
    BsDatepickerModule,
    SubheaderModule,
    TreeSelectModule,
    OrderListModule,
    ScrollPanelModule,
    InputNumberModule,
    MultiSelectModule,
    AccordionModule,
    PickListModule,
    SplitterModule,
    DynamicTreeBuilderComponent,
    TreeTableModule,
    DxPivotGridModule,
    DxChartModule,
    DxRadioGroupModule,
    TreeModule,
    DxPopupModule,
    DxScrollViewModule,
    DxButtonModule,
    DxDataGridModule,
    DxTreeListModule,
    DxTabPanelModule,
    DxSelectBoxModule,
    DxNumberBoxModule,
    DxCheckBoxModule,
    DxColorBoxModule,
    RadioButtonModule,
    ToastModule,
    ContextMenuModule,
    ConfirmDialogModule,
    DatetimeRangeSelectorComponent,
    ResolutionSelectorComponent,
    CustomParameterSelectionTabComponent,
    ExceptionParameterSelectionTabComponent,
    LogicalParameterSelectionTabComponent,
    ChannelParameterSelectionTabComponent,
    ComponentsSelectorComponent,
    EventParameterSelectionTabComponent,
    DxLoadIndicatorModule,
    DxTextBoxModule,
    AdditionalParameterSelectionTabComponent,
    TabsModule.forRoot(),
    BarChartPreviewComponent
],

    declarations: [
        CustomizableDashboardComponent,
        WidgetGeneralStatsComponent,
        WidgetDailySalesComponent,
        WidgetEditionStatisticsComponent,
        WidgetHostTopStatsComponent,
        WidgetIncomeStatisticsComponent,
        WidgetMemberActivityComponent,
        WidgetProfitShareComponent,
        WidgetRecentTenantsComponent,
        WidgetRegionalStatsComponent,
        WidgetSalesSummaryComponent,
        WidgetSubscriptionExpiringTenantsComponent,
        WidgetTopStatsComponent,
        FilterDateRangePickerComponent,
        AddWidgetModalComponent,
        RenameWidgetModalComponent,
        WidgetComponentBaseComponent,
        WidgetPQSComponent,
        WidgetPQSTableComponent,
        WidgetPqsBarChartComponent,
        CreateOrEditTrendConfigurationComponent,
        CreateOrEditBarChartConfigurationComponent,
        CreateOrEditTableConfigurationComponent,
        RenameWidgetModalComponent,
        TablePreviewComponent,
        TableDesignOptionsComponent,
    ],

    providers: [DashboardViewConfigurationService],

    exports: [
        CustomizableDashboardComponent,
        WidgetGeneralStatsComponent,
        WidgetDailySalesComponent,
        WidgetEditionStatisticsComponent,
        WidgetHostTopStatsComponent,
        WidgetIncomeStatisticsComponent,
        WidgetMemberActivityComponent,
        WidgetProfitShareComponent,
        WidgetRecentTenantsComponent,
        WidgetRegionalStatsComponent,
        WidgetSalesSummaryComponent,
        WidgetSubscriptionExpiringTenantsComponent,
        WidgetTopStatsComponent,
        FilterDateRangePickerComponent,
        AddWidgetModalComponent,
        RenameWidgetModalComponent,
        WidgetPQSComponent,
        WidgetPQSTableComponent,
        WidgetPqsBarChartComponent,
        CreateOrEditTrendConfigurationComponent,
        CreateOrEditBarChartConfigurationComponent,
        CreateOrEditTableConfigurationComponent,
        RenameWidgetModalComponent,
        TablePreviewComponent,
    ],
})
export class CustomizableDashboardModule {}
