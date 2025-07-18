import {
    Component,
    OnInit,
    Injector,
    Input,
    ViewChild,
    OnDestroy,
    AfterViewInit,
    Injectable,
    InjectionToken,
    ViewChildren,
    QueryList,
} from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DashboardViewConfigurationService } from './dashboard-view-configuration.service';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { GridsterConfig, GridsterItem } from 'angular-gridster2';
import {
    DashboardCustomizationServiceProxy,
    DashboardOutput,
    AddNewPageInput,
    AddNewPageOutput,
    AddWidgetInput,
    RenamePageInput,
    SavePageInput,
    Page,
    Widget,
    WidgetFilterOutput,
    WidgetOutput,
    Dashboard,
    IWidget,
    WidgetConfigurationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { TabsetComponent } from 'ngx-bootstrap/tabs';
import { BsDropdownDirective } from 'ngx-bootstrap/dropdown';
import { WidgetViewDefinition, WidgetFilterViewDefinition } from './definitions';
import { AddWidgetModalComponent } from './add-widget-modal/add-widget-modal.component';
import { DashboardCustomizationConst } from './DashboardCustomizationConsts';
import { ModalDirective } from 'ngx-bootstrap/modal';
import * as rtlDetect from 'rtl-detect';
import { Subject, forkJoin } from 'rxjs';
import { GuidGeneratorService } from '@shared/utils/guid-generator.service';
import { LocalStorageService } from '@shared/utils/local-storage.service';
import safeStringify from 'fast-safe-stringify';

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
    hasPendingChanges = false;

    //gridster options. all gridster needs its options. In our scenario, they are all same.
    options: GridsterConfig[] = [];

    dashboardDefinition: DashboardOutput;
    userDashboard: any;

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

    constructor(
        private _injector: Injector,
        private _dashboardViewConfiguration: DashboardViewConfigurationService,
        private _dashboardCustomizationServiceProxy: DashboardCustomizationServiceProxy,
        private _widgetConfigurationsServiceProxy: WidgetConfigurationsServiceProxy,
        private _guidGenerator: GuidGeneratorService,
        private _localStorageService: LocalStorageService,
    ) {
        super(_injector);
    }

    ngOnInit() {
        this.loading = true;
        const storedMode = localStorage.getItem('app.dashboard.editMode');
        if (storedMode === 'true') {
            this.editModeEnabled = true;
        }
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

            this.userDashboard.pages.forEach(page => {
                page.widgets.forEach(widget => {
                    this._widgetConfigurationsServiceProxy
                        .getWidgetConfigurationForEditByWidgetId(widget.guid)
                        .subscribe(cfg => {
                            widget.displayName = cfg.widgetConfiguration?.name || widget.id;
                        });
                });
            });

            if (this.userDashboard.pages?.length) {
                this.selectedPage = {
                    id: this.userDashboard.pages[0].id,
                    name: this.userDashboard.pages[0].name,
                };
                this.selectPageTab(this.userDashboard.pages[0].id);
            }

            this.loading = false;
            this.busy = false;
        });

        this.subscribeToEvent('app.kt_aside_toggler.onClick', this.onMenuToggle);
    }

    initializeUserDashboardDefinition(
        userDashboardResultFromServer: Dashboard,
        dashboardDefinitionResult: DashboardOutput,
    ) {
        this.userDashboard = {
            dashboardName: this.dashboardName,
            filters: [],
            pages: userDashboardResultFromServer.pages.map((page) => {
                //gridster should has its own options
                const cfg = this.getGridsterConfig();
                cfg.itemChangeCallback = () => this.markDirty();
                cfg.itemResizeCallback = () => this.markDirty();
                cfg.itemRemovedCallback = () => this.markDirty();
                cfg.itemInitCallback = () => this.markDirty();
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
    }

    removeItem(item: GridsterItem) {
        const page = this.userDashboard.pages.find(p => p.id === this.selectedPage.id);
        const widget = page.widgets.find(w => w.guid === item.guid);
        const widgetDefinition = this.dashboardDefinition.widgets.find(wd => wd.id === item.id);

        if (!widget || !widgetDefinition) {
            return;
        }

        const nameToShow = widget.displayName || widgetDefinition.id;
        this.message.confirm(
            this.l('WidgetDeleteWarningMessage', nameToShow, this.selectedPage.name),
            this.l('AreYouSure'),
            isConfirmed => {
                if (isConfirmed) {
                    page.widgets.splice(page.widgets.indexOf(widget), 1);
                    this.refreshAllGrids();
                }
            }
        );
        this.markDirty();
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

        this._dashboardCustomizationServiceProxy
            .addWidget(
                new AddWidgetInput({
                    widgetId: widgetId,
                    widgetGuid: this._guidGenerator.guid(),
                    pageId: this.selectedPage.id,
                    dashboardName: this.dashboardName,
                    width: widgetViewConfiguration.defaultWidth,
                    height: widgetViewConfiguration.defaultHeight,
                    application: DashboardCustomizationConst.Applications.Angular,
                }),
            )
            .subscribe((addedWidget) => {
                const newWidget = {
                    id: widgetId,
                    guid: addedWidget.widgetGuid,
                    isNew: true,
                    component: widgetViewConfiguration.component,
                    gridInformation: {
                        id: widgetId,
                        cols: addedWidget.width,
                        rows: addedWidget.height,
                        x: addedWidget.positionX,
                        y: addedWidget.positionY,
                    },
                };

                this.userDashboard.pages.find((page) => page.id === this.selectedPage.id).widgets.push(newWidget);

                this.createWidgetSubject(newWidget.guid);

                this.initializeUserDashboardFilters();

                this.busy = false;
                this.notify.success(this.l('SavedSuccessfully'));
            });

            this.markDirty();
    }

    changeEditMode(): void {
        this.editModeEnabled = !this.editModeEnabled;
        localStorage.setItem('app.dashboard.editMode', String(this.editModeEnabled));
        if (!this.editModeEnabled) {
            localStorage.removeItem('editMode');
        }
        //change all gridster options
        //setTimeout for letting the DOM first update so that the edit button appears
        setTimeout(() => {
            this.refreshAllGrids();
        }, 0);
        abp.event.trigger('app.dashboardEdit.onEditStateChange', this.editModeEnabled);
    }

    refreshAllGrids(): void {
        if (this.options) {
            this.options.forEach((option) => {
                option.draggable.enabled = this.editModeEnabled;
                option.resizable.enabled = this.editModeEnabled;
                option.api.optionsChanged();
            });
        }
    }

    onWidgetRefresh(event: any) {
        console.log(event);
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

                this.busy = false;
                this.notify.success(this.l('SavedSuccessfully'));

                if (this.selectedPage.id === '') {
                    this.selectPageTab(result.pageId);
                }
            });

        this.dropdownAddPage.hide();

        this.markDirty();
    }

    selectPageTab(pageId: string): void {
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

        if (!this.loadedTabs[pageId]) {
            this.loadedTabs[pageId] = true;
        }

        //when tab change gridster should redraw because if a tab is not active gridster think that its height is 0 and do not draw it.
        this.options.forEach((option) => {
            if (option.api) {
                option.api.optionsChanged();
            }
        });
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
            });

        this.dropdownRenamePage.hide();

        this.markDirty();
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

                        this.busy = false;
                        this.notify.success(this.l('SuccessfullyRemoved'));

                        if (this.userDashboard.pages.length === 0) {
                            window.location.reload();
                        }
                    });
            }
        });

        this.markDirty();
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
        this.hasPendingChanges = false;

        let savePageInput = new SavePageInput({
            dashboardName: this.dashboardName,
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
        });

        this._dashboardCustomizationServiceProxy.savePage(savePageInput).subscribe(() => {
            this.changeEditMode(); //after changes saved close edit mode
            this.initializeUserDashboardFilters();

            this.busy = false;
            this.notify.success(this.l('SavedSuccessfully'));
            window.location.reload();
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
                    { provide: 'widgetRefresh', useValue: (event: any) => this.onWidgetRefresh(event) },
                ],
                parent: this._injector,
            }),
        };
    }

    private markDirty() {
        if (!this.hasPendingChanges) {
            this.hasPendingChanges = true;
            abp.event.trigger('app.dashboardEdit.onDirtyStateChange', true);
        }
    }

    private getWidgetViewDefinition(id: string): WidgetViewDefinition {
        return this._dashboardViewConfiguration.WidgetViewDefinitions.find((widget) => widget.id === id);
    }

    private getWidgetFilterViewDefinition(id: string): WidgetFilterViewDefinition {
        return this._dashboardViewConfiguration.widgetFilterDefinitions.find((filter) => filter.id === id);
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
            },
            resizable: {
                enabled: this.editModeEnabled,
            },
            compactType: 'compactUp',
            fixedRowHeight: 30,
            fixedColWidth: 30,
            gridType: 'verticalFixed',
            dirType: isRtl ? 'rtl' : 'ltr',
        };
    }
}
