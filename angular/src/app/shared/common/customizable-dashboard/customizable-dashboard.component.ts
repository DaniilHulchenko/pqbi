import {
    Component,
    OnInit,
    Injector,
    Input,
    ViewChild,
    OnDestroy,
    Injectable,
    InjectionToken,
    HostListener,

} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardViewConfigurationService } from './dashboard-view-configuration.service';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { GridsterConfig } from 'angular-gridster2';
import {
    DashboardCustomizationServiceProxy,
    DashboardOutput,
    AddNewPageInput,
    AddNewPageOutput,
    RenamePageInput,
    Page,
    Widget,
    WidgetFilterOutput,
    Dashboard,
    WidgetConfigurationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { BsDropdownDirective } from 'ngx-bootstrap/dropdown';
import { WidgetViewDefinition, WidgetFilterViewDefinition } from './definitions';
import { AddWidgetModalComponent } from './add-widget-modal/add-widget-modal.component';
import { DashboardCustomizationConst } from './DashboardCustomizationConsts';
import { ModalDirective } from 'ngx-bootstrap/modal';
import * as rtlDetect from 'rtl-detect';
import { Subject, Subscription, forkJoin } from 'rxjs';
import { GuidGeneratorService } from '@shared/utils/guid-generator.service';
import { EditModeService } from '@app/shared/services/edit-mode-service.service';
import { max } from 'lodash-es'
import { WidgetUpdateModel } from '@app/shared/models/widget-update-model';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import {
    DashboardConfigurationService,
    DashboardConfigurationState,
} from './dashboard-configuration.service';
import { DashboardToolbarService } from '@app/shared/services/dashboard-toolbar.service';


export const WIDGETONRESIZEEVENTHANDLERTOKEN = new InjectionToken<WidgetOnResizeEventHandler>(
    'WidgetOnResizeEventHandlerToken',
);

@Injectable()
export class WidgetOnResizeEventHandler {
    onResize: Subject<any> = new Subject();
}

@Component({
    selector: 'customizable-dashboard',
    templateUrl: './customizable-dashboard.component.html',
    styleUrls: ['./customizable-dashboard.component.css'],
    animations: [appModuleAnimation()],
})
export class CustomizableDashboardComponent extends AppComponentBase implements OnInit, OnDestroy {
    @Input() dashboardName: string;
    @ViewChild('addWidgetModal') addWidgetModal: AddWidgetModalComponent;
    @ViewChild('dashboardTabs') dashboardTabs: TabsetComponent;
    @ViewChild('filterModal', { static: true }) modal: ModalDirective;
    @ViewChild('dropdownRenamePage') dropdownRenamePage: BsDropdownDirective;
    @ViewChild('dropdownAddPage') dropdownAddPage: BsDropdownDirective;

    
    loading = true;
    busy = true;
    editModeEnabled = false;
    readonly defaultDashboardBackgroundColor = this.bgColor;

    //gridster options. all gridster needs its options. In our scenario, they are all same.
    options: GridsterConfig[] = [];

    dashboardDefinition: DashboardOutput;
    userDashboard: any;

    dashboardConfiguration: DashboardConfigurationState = {
        backgroundColor: this.defaultDashboardBackgroundColor,
    };

    selectedPage = {
        id: '',
        name: '',
    };

    loadedTabs: { [key: number]: boolean } = {};

    renamePageInput = '';
    addPageInput = '';

    widgetSubjects: {
        [key: string]: {
            handler: WidgetOnResizeEventHandler;
            injector: Injector;
        };
    } = {};

    myinjector: Injector;

    private configurationSubscription?: Subscription;
    private editModeSubscription?: Subscription;

    widgetNameFontSizes = [12, 14, 16, 18, 20, 22, 24];
    readonly defaultWidgetTitleSize = 20;
    widgetTitleSizeLabel = 'WidgetTitleSize';

    constructor(
        private _injector: Injector,
        private _dashboardViewConfiguration: DashboardViewConfigurationService,
        private _dashboardCustomizationServiceProxy: DashboardCustomizationServiceProxy,
        private _widgetConfigurationsServiceProxy: WidgetConfigurationsServiceProxy,
        private _guidGenerator: GuidGeneratorService,
        private editModeService: EditModeService,
        private dashboardPagesService: DashboardPagesService,
        private dashboardConfigurationService: DashboardConfigurationService,
        private dashboardToolbarService: DashboardToolbarService,
    ) {
        super(_injector);
        this.widgetTitleSizeLabel = this.getWidgetTitleSizeLabel();
    }

        requestWidgetEdit(widget: any): void {
        abp.event.trigger('app.dashboard.editWidgetRequested', {
            widgetGuid: widget.guid,
            widgetId: widget.id,
            configurationId: widget.configurationId,
        });
    }

    requestWidgetRename(widget: any): void {
        abp.event.trigger('app.dashboard.renameWidgetRequested', {
            widgetGuid: widget.guid,
            widgetId: widget.id,
            currentName: widget.displayName,
        });
    }
    ngOnInit() {
        this.loading = true;
        this.dashboardToolbarService.setDashboardActive(true);
        this.editModeEnabled = this.editModeService.getEditModeValue();
        this.registerEditModeSubscription();
        this.registerConfigurationSubscription();
        this.subscribeToEvent('app.dashboard.removeWidget', (widgetGuid, widgetId) => this.removeItem(widgetGuid, widgetId, true));
        this.subscribeToEvent('app.dashboard.saveWidget', (widgetUpdateModel: WidgetUpdateModel) => this.updateItem(widgetUpdateModel));
        this.subscribeToEvent('app.dashboard.navigateToPage', (pageId: string) => this.onNavigateToPage(pageId));


        forkJoin([
            this._dashboardCustomizationServiceProxy.getUserDashboard(
                this.dashboardName,
                DashboardCustomizationConst.Applications.Angular,
            ),
            this._dashboardCustomizationServiceProxy.getDashboardDefinition(
                this.dashboardName,
                DashboardCustomizationConst.Applications.Angular,
            ),
        ]).subscribe(([userDashboardResultFromServer, dashboardDefinitionResult]) => {
            this.dashboardDefinition = dashboardDefinitionResult;
            this.initializeDashboardConfiguration((userDashboardResultFromServer as any)?.configuration);
            if (!this.dashboardDefinition.widgets || this.dashboardDefinition.widgets.length === 0) {
                this.loading = false;
                this.busy = false;
                return;
            }

            if (!userDashboardResultFromServer.pages || userDashboardResultFromServer.pages.length === 0) {
                this.loading = false;
                this.busy = false;
                return;
            }

            this.initializeUserDashboardDefinition(userDashboardResultFromServer, dashboardDefinitionResult);
            this.initializeUserDashboardFilters();

            var widgetIds: string[] = this.userDashboard.pages
                .flatMap(page => page.widgets.map(widget => widget.guid));

            if (widgetIds.length === 0) {
                this.loading = false;
                this.busy = false;

                this.selectedPage = {
                    id: this.userDashboard.pages[0].id,
                    name: this.userDashboard.pages[0].name,
                };
                this.selectPageTab(this.userDashboard.pages[0].id);

                return;
            }

            this._widgetConfigurationsServiceProxy
                .getWidgetConfigurationBatchesByWidgetIds(widgetIds)
                .subscribe((cfg) => {
                    this.userDashboard.pages.forEach((page) => {
                        page.widgets.forEach((widget) => {
                            var config = cfg.find((c) => c.widgetConfiguration.widgetGuid === widget.guid);
                            widget.displayName = config?.widgetConfiguration?.name || widget.id;
                            widget.configuration = config?.widgetConfiguration?.configuration;
                            widget.configurationId = config?.widgetConfiguration?.id;
                        });
                    });

                    this.loading = false;
                    this.busy = false;

                    this.selectedPage = {
                        id: this.userDashboard.pages[0].id,
                        name: this.userDashboard.pages[0].name,
                    };
                    this.selectPageTab(this.userDashboard.pages[0].id);
                });
        });

        this.subscribeToEvent('app.kt_aside_toggler.onClick', this.onMenuToggle);
        this.subscribeToEvent('app.dashboard.renameWidget', (widgetId, newName) => {
            const widget = this.userDashboard.pages
                .flatMap((page) => page.widgets)
                .find((widget) => widget.guid === widgetId);

            if (widget) {
                widget.displayName = newName;
            }
        });
    }

    private initializeDashboardConfiguration(configuration: DashboardConfigurationState | undefined): void {
        const storedBackgroundColor = this.dashboardConfigurationService.getStoredBackgroundColor();
        const backgroundColor =
            configuration?.backgroundColor || storedBackgroundColor || this.defaultDashboardBackgroundColor;

        this.dashboardConfiguration = {
            ...configuration,
            backgroundColor,
        };

        this.dashboardConfigurationService.setConfiguration(this.dashboardConfiguration);

        if (!storedBackgroundColor && backgroundColor) {
            this.dashboardConfigurationService.updateBackgroundColor(backgroundColor);
        }
    }

    initializeUserDashboardDefinition(
        userDashboardResultFromServer: Dashboard,
        dashboardDefinitionResult: DashboardOutput,
    ) {
        this.userDashboard = {
            dashboardName: this.dashboardName,
            filters: [],
            configuration: this.dashboardConfiguration,
            pages: userDashboardResultFromServer.pages.map((page) => {
                //gridster should has its own options
                const cfg = this.getGridsterConfig();
                this.options.push(cfg);

                if (!page.widgets) {
                    return {
                        id: page.id,
                        name: page.name,
                        widgets: [],
                    };
                }

                //only use widgets which dashboard definition contains and have view definition
                //(dashboard definition can be changed after users save their dashboard, because it depends on permissions and other stuff)
                page.widgets = page.widgets.filter(
                    (w) =>
                        dashboardDefinitionResult.widgets.find((d) => d.id === w.widgetId) &&
                        this.getWidgetViewDefinition(w.widgetId),
                );

                return {
                    id: page.id,
                    name: page.name,
                    widgets: page.widgets.map((widget) => ({
                        id: widget.widgetId,
                        guid: widget.widgetGuid, //add here loaded guid
                        //View definitions are stored in the angular side(a component of widget/filter etc.) get view definition and use defined component
                        component: this.getWidgetViewDefinition(widget.widgetId).component,
                        gridInformation: {
                            id: widget.widgetId,
                            cols: widget.width,
                            rows: widget.height,
                            x: widget.positionX,
                            y: widget.positionY,
                        },
                    })),
                };
            }),
        };

        this.createWidgetSubjects();
        this.updatePagesStore();
    }

    onNavigateToPage(pageId: string): void {
        const targetPage = this.userDashboard?.pages?.find((page) => page.id === pageId);

        if (!targetPage) {
            this.notify.error('Selected page no longer exists.');
            return;
        }

        this.selectPageTab(pageId, true);
    }

    updateItem(widgetUpdateModel: WidgetUpdateModel) {
        const page = this.userDashboard.pages.find(p => p.id === this.selectedPage.id);
        const widget = page.widgets.find(w => w.guid === widgetUpdateModel.guid);

        if (!widget) {
            return;
        }

        widget.displayName = widgetUpdateModel.name;
        widget.configuration = widgetUpdateModel.configuration;
        widget.configurationId = widgetUpdateModel.id;
        widget.guid = widgetUpdateModel.guid;
        widget.isNew = undefined;
    }

    removeItem(widgetGuid: string, widgetId: string, isConfirmed: boolean) {
        const page = this.userDashboard.pages.find(p => p.id === this.selectedPage.id);
        const widget = page.widgets.find(w => w.guid === widgetGuid);
        const widgetDefinition = this.dashboardDefinition.widgets.find(wd => wd.id === widgetId);

        if (!widget || !widgetDefinition) {
            return;
        }

        function removeFromDashboardAndUpdate()
        {
            page.widgets.splice(page.widgets.indexOf(widget), 1);
        }

        if (isConfirmed) {
            removeFromDashboardAndUpdate();
        } else
        {
            const nameToShow = widget.displayName || widgetDefinition.id;
            this.message.confirm(
                this.l('WidgetDeleteWarningMessage', nameToShow, this.selectedPage.name),
                this.l('AreYouSure'),
                (isConfirmed) => {
                    if (isConfirmed) {
                        removeFromDashboardAndUpdate();
                    }
                },
            );
        }
    }

    addWidget(widgetId: any): void {
        if (!widgetId) {
            return;
        }

        let widgetViewConfiguration = this._dashboardViewConfiguration.WidgetViewDefinitions.find(
            (w) => w.id === widgetId,
        );
        if (!widgetViewConfiguration) {
            abp.notify.error(this.l('ThereIsNoViewConfigurationForX', widgetId));
            return;
        }

        this.busy = true;

        const widgetWidth = widgetId === 'Widgets_Tenant_PQSCard'|| widgetId === 'Widgets_Tenant_PQSGauge'
            ? 2
            : widgetViewConfiguration.defaultWidth


        // this._dashboardCustomizationServiceProxy
        //     .addWidget(
        //         new AddWidgetInput({
        //             widgetId: widgetId,
        //             widgetGuid: this._guidGenerator.guid(),
        //             pageId: this.selectedPage.id,
        //             dashboardName: this.dashboardName,
        //             width: widgetWidth,
        //             height: widgetViewConfiguration.defaultHeight,
        //             application: DashboardCustomizationConst.Applications.Angular,
        //         }),
        //     )
        //     .subscribe((addedWidget) => {

            const y =
                max(
                    this.userDashboard.pages
                        .find((page) => page.id === this.selectedPage.id)
                        .widgets?.map((w) => w.gridInformation.x + w.gridInformation.rows),
                ) ?? 0;

            const newWidget = {
                id: widgetId,
                guid: this._guidGenerator.guid(),
                isNew: true,
                component: widgetViewConfiguration.component,
                gridInformation: {
                    id: widgetId,
                    cols: widgetWidth,
                    rows: widgetViewConfiguration.defaultHeight,
                    x: 0,
                    y: y,
                },
            };

            this.userDashboard.pages.find((page) => page.id === this.selectedPage.id).widgets.push(newWidget);

            this.createWidgetSubject(newWidget.guid);

            this.initializeUserDashboardFilters();

            this.busy = false;

            setTimeout(() => {
                newWidget.isNew = false;
            }, 5000);
            //this.notify.success(this.l('SavedSuccessfully'));
            // });
    }

    changeEditMode(): void {
        //change all gridster options
        //setTimeout for letting the DOM first update so that the edit button appears
        setTimeout(() => {
            this.editModeService.setEditMode(!this.editModeEnabled);
        }, 150);
    }

    refreshAllGrids(): void {
        if (this.options) {
            const rowHeight = this.getResponsiveRowHeight();

            this.options.forEach((option) => {
                option.draggable.enabled = this.editModeEnabled;
                option.resizable.enabled = this.editModeEnabled;
                option.api?.optionsChanged();
                if (option.fixedRowHeight !== rowHeight) {
                    option.fixedRowHeight = rowHeight;
                }
            });
        }
    }

    private getWidgetTitleSizeLabel(): string {
        const label = this.l('WidgetTitleSize');

        if (!label || label.startsWith('[') || label === 'WidgetTitleSize') {
            return 'Widget Title Size';
        }

        return label;
    }

    openAddWidgetModal(): void {
        let page = this.userDashboard.pages.find((page) => page.id === this.selectedPage.id);
        if (page) {
            this.addWidgetModal.show(this.dashboardName, this.selectedPage.id);
        }
    }

    addNewPage(pageName: string): void {
        if (!pageName || pageName.trim() === '') {
            this.notify.warn(this.l('PageNameCanNotBeEmpty'));
            return;
        }

        pageName = pageName.trim();

        this.busy = true;
        this._dashboardCustomizationServiceProxy
            .addNewPage(
                new AddNewPageInput({
                    dashboardName: this.dashboardName,
                    name: pageName,
                    application: DashboardCustomizationConst.Applications.Angular,
                }),
            )
            .subscribe((result: AddNewPageOutput) => {
                //gridster options for new page
                this.options.push(this.getGridsterConfig());

                this.userDashboard.pages.push({
                    id: result.pageId,
                    name: pageName,
                    widgets: [],
                });

                this.updatePagesStore();

                this.busy = false;
                this.notify.success(this.l('SavedSuccessfully'));

                if (this.selectedPage.id === '') {
                    this.selectPageTab(result.pageId);
                }
            });

        this.dropdownAddPage.hide();
    }

    selectPageTab(pageId: string, activateTab: boolean = false): void {
        if (!pageId) {
            this.selectedPage = {
                id: '',
                name: '',
            };

            return;
        }

        this.selectedPage = {
            id: pageId,
            name: this.userDashboard.pages.find((page) => page.id === pageId).name,
        };
        if (activateTab) {
            this.activateTab(pageId);
        }

        if (!this.loadedTabs[pageId]) {
            this.loadedTabs[pageId] = true;
            
        }
        
        if (this.editModeEnabled) {
            setTimeout(() => {
                abp.event.trigger('app.dashboardEdit.onEditStateChange', this.editModeEnabled);
            }, 0);
        }

        //when tab change gridster should redraw because if a tab is not active gridster think that its height is 0 and do not draw it.
        this.options.forEach((option) => {
            if (option.api) {
                option.api.optionsChanged();
            }
        });
    }

    private activateTab(pageId: string): void {
        const tabIndex = this.userDashboard?.pages?.findIndex((page) => page.id === pageId);

        if (tabIndex === undefined || tabIndex === null || tabIndex < 0) {
            return;
        }

        const tab = this.dashboardTabs?.tabs?.[tabIndex];

        if (tab && !tab.active) {
            tab.active = true;
        }
    }

    renamePage(pageName: string): void {
        if (!pageName || pageName === '') {
            this.notify.warn(this.l('PageNameCanNotBeEmpty'));
            return;
        }

        pageName = pageName.trim();

        this.busy = true;

        let pageId = this.selectedPage.id;
        this._dashboardCustomizationServiceProxy
            .renamePage(
                new RenamePageInput({
                    dashboardName: this.dashboardName,
                    id: pageId,
                    name: pageName,
                    application: DashboardCustomizationConst.Applications.Angular,
                }),
            )
            .subscribe(() => {
                let dashboardPage = this.userDashboard.pages.find((page) => page.id === pageId);
                dashboardPage.name = pageName;
                this.notify.success(this.l('Renamed'));
                this.busy = false;
                this.updatePagesStore();
            });

        this.dropdownRenamePage.hide();
    }

    deletePage(): void {
        let message =
            this.userDashboard.pages.length > 1
                ? this.l('PageDeleteWarningMessage', this.selectedPage.name)
                : this.l('BackToDefaultPageWarningMessage', this.selectedPage.name);

        this.message.confirm(message, this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this.busy = true;
                this._dashboardCustomizationServiceProxy
                    .deletePage(
                        this.selectedPage.id,
                        this.dashboardName,
                        DashboardCustomizationConst.Applications.Angular,
                    )
                    .subscribe(() => {
                        let dashboardPage = this.userDashboard.pages.find((page) => page.id === this.selectedPage.id);

                        this.options.pop(); // since all of our gridster has same options, its not important which options we are removing
                        this.userDashboard.pages.splice(this.userDashboard.pages.indexOf(dashboardPage), 1);
                        this.activateFirstPage();

                        this.updatePagesStore();

                        this.busy = false;
                        this.notify.success(this.l('SuccessfullyRemoved'));

                        if (this.userDashboard.pages.length === 0) {
                            window.location.reload();
                        }
                    });
            }
        });
    }

    activateFirstPage() {
        if (this.userDashboard.pages[0]) {
            setTimeout(() => {
                let tab = this.dashboardTabs.tabs[0];
                tab.active = true;
            }, 0);

            this.selectPageTab(this.userDashboard.pages[0].id);
            this.initializeUserDashboardFilters();
        } else {
            this.selectPageTab(null);
        }
    }

    savePage(): void {
        this.busy = true;

        abp.event.trigger('app.dashboardEdit.onSave');

        const savePageInput: any = {
            dashboardName: this.dashboardName,
            configuration: this.dashboardConfiguration,
            pages: this.userDashboard.pages.map(
                (page) =>
                    new Page({
                        id: page.id,
                        name: page.name,
                        widgets: page.widgets.map((widget) => {
                            // let widgetConf = safeStringify(JSON.parse(sessionStorage.getItem('Widget_'+widget.guid)));

                            let newWidget = new Widget({
                                widgetId: widget.id,
                                widgetGuid: widget.guid,
                                height: widget.gridInformation.rows,
                                width: widget.gridInformation.cols,
                                positionX: widget.gridInformation.x,
                                positionY: widget.gridInformation.y,
                            });

                            return newWidget;
                        }),
                    }),
            ),
            application: DashboardCustomizationConst.Applications.Angular,
        };

        this._dashboardCustomizationServiceProxy.savePage(savePageInput).subscribe(() => {
            this.changeEditMode(); //after changes saved close edit mode
            this.initializeUserDashboardFilters();

            this.busy = false;
            this.notify.success(this.l('SavedSuccessfully'));
            //window.location.reload();
        });
    }

    moreThanOnePage(): boolean {
        return this.userDashboard && this.userDashboard.pages && this.userDashboard.pages.length > 1;
    }

    close(): void {
        this.modal.hide();
    }

    addPageDropdownShown(): void {
        this.addPageInput = '';
    }

    renamePageDropdownShown(): void {
        this.renamePageInput = '';
    }

    onMenuToggle = () => {
        this.refreshAllGrids();
    };

    onGridSterItemResize(item: any): void {
        if (this.editModeEnabled) {
            if (this.widgetSubjects[item.guid]) {
                this.widgetSubjects[item.guid].handler.onResize.next(null);
            }
        }
    }

    createWidgetSubjects() {
        for (let i = 0; i < this.userDashboard.pages.length; i++) {
            let page = this.userDashboard.pages[i];
            for (let i = 0; i < page.widgets.length; i++) {
                const widget = page.widgets[i];
                this.createWidgetSubject(widget.guid);
            }
        }
    }

    createWidgetSubject(guid: string) {
        let handler = new WidgetOnResizeEventHandler();
        this.widgetSubjects[guid] = {
            handler,
            injector: Injector.create({
                providers: [
                    { provide: WIDGETONRESIZEEVENTHANDLERTOKEN, useValue: handler },
                    { provide: 'widgetRefresh', useValue: (event: any) => {} },
                ],
                parent: this._injector,
            }),
        };
    }

    private registerConfigurationSubscription(): void {
        this.configurationSubscription = this.dashboardConfigurationService
            .getConfiguration()
            .subscribe((configuration) => {
                this.dashboardConfiguration = {
                    ...configuration,
                    backgroundColor: configuration.backgroundColor || this.defaultDashboardBackgroundColor,
                };
            });
    }

    private registerEditModeSubscription(): void {
        this.editModeSubscription = this.editModeService.getEditMode().subscribe((enabled) => {
            const previousValue = this.editModeEnabled;
            this.editModeEnabled = enabled;

            if (previousValue === enabled) {
                return;
            }

            abp.event.trigger('app.dashboardEdit.onEditStateChange', this.editModeEnabled);
            this.refreshAllGrids();
        });
    }

    private updatePagesStore(): void {
        this.dashboardPagesService.setPages(this.userDashboard?.pages ?? []);
    }

     onWidgetNameFontSizeChange(size: number | undefined): void {
        this.dashboardConfigurationService.updateWidgetNameFontSize(size ? +size : undefined);
    }

    private getWidgetViewDefinition(id: string): WidgetViewDefinition {
        return this._dashboardViewConfiguration.WidgetViewDefinitions.find((widget) => widget.id === id);
    }

    private getWidgetFilterViewDefinition(id: string): WidgetFilterViewDefinition {
        return this._dashboardViewConfiguration.widgetFilterDefinitions.find((filter) => filter.id === id);
    }

    @HostListener('window:resize')
    onWindowResize(): void {
        this.updateGridsterRowHeights();
    }

    private updateGridsterRowHeights(): void {
        const rowHeight = this.getResponsiveRowHeight();
        this.options?.forEach((option) => {
            if (option.fixedRowHeight !== rowHeight) {
                option.fixedRowHeight = rowHeight;
                option.api?.optionsChanged();
            }
        });
    }

    ngOnDestroy(): void {
        this.dashboardToolbarService.setDashboardActive(false);
        this.configurationSubscription?.unsubscribe();
        this.editModeSubscription?.unsubscribe();
        super.ngOnDestroy();
    }

    private getResponsiveRowHeight(): number {
        const baseRowHeight = 30;
        const minRowHeight = 18;
        const maxRowHeight = 36;
        const viewportHeight = typeof window !== 'undefined'
            ? window.innerHeight || document.documentElement?.clientHeight || 0
            : 0;

        if (!viewportHeight) {
            return baseRowHeight;
        }

        const referenceHeight = 1080;
        const scaledRowHeight = Math.round((viewportHeight / referenceHeight) * baseRowHeight);
        return Math.max(minRowHeight, Math.min(maxRowHeight, scaledRowHeight));
    }

    get dashboardBackgroundColor(): string {
        return this.dashboardConfiguration?.backgroundColor || this.defaultDashboardBackgroundColor;
    }


    //after we load page or add widget initialize needed filter too.
    private initializeUserDashboardFilters(): void {
        let allFilters: WidgetFilterOutput[] = [];

        this.dashboardDefinition.widgets
            .filter((widget) => widget.filters != null && widget.filters.length > 0)
            .forEach((widget) => {
                if (this.userDashboard.pages) {
                    this.userDashboard.pages.forEach((page) => {
                        //if user has this widget in any page
                        if (page.widgets.filter((userWidget) => userWidget.id === widget.id).length !== 0) {
                            widget.filters.forEach((filter) => {
                                if (!allFilters.find((f) => f.id === filter.id)) {
                                    allFilters.push(filter);
                                }
                            });
                        }
                    });
                }
            });

        this.userDashboard.filters = allFilters.map((filter) => {
            let definition = this.getWidgetFilterViewDefinition(filter.id);
            definition['name'] = filter.name;
            return definition;
        });
    }

    //all pages use gridster and its where they get their options. Changing this will change all gristers.
    private getGridsterConfig(): GridsterConfig {
        const isRtl = rtlDetect.isRtlLang(abp.localization.currentLanguage.name);
        return {
            pushItems: true,
            draggable: {
                enabled: this.editModeEnabled,
                ignoreContentClass: 'notDraggable'
            },
            resizable: {
                enabled: this.editModeEnabled,
            },
            compactType: 'compactUp',
            fixedRowHeight: this.getResponsiveRowHeight(),
            fixedColWidth: 30,
            minCols: 12,
            gridType: 'verticalFixed',
            dirType: isRtl ? 'rtl' : 'ltr',
        };
    }
}
