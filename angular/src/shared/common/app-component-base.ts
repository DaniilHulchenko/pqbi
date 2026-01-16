import {
    PermissionCheckerService,
    FeatureCheckerService,
    LocalizationService,
    MessageService,
    AbpMultiTenancyService,
    NotifyService,
    SettingService,
} from 'abp-ng2-module';
import { Component, Injector, OnDestroy, OnInit } from '@angular/core';
import { AppConsts } from '@shared/AppConsts';
import { AppUrlService } from '@shared/common/nav/app-url.service';
import { AppSessionService } from '@shared/common/session/app-session.service';
import { AppUiCustomizationService } from '@shared/common/ui/app-ui-customization.service';
import { PrimengTableHelper } from 'shared/helpers/PrimengTableHelper';
import { UiCustomizationSettingsDto } from '@shared/service-proxies/service-proxies';
import '@shared/service-proxies/tenant-login-info-dto-extensions';
import { NgxSpinnerService } from 'ngx-spinner';
import { NgxSpinnerTextService } from '@app/shared/ngx-spinner-text.service';
import notify from 'devextreme/ui/notify';
import { DefaultValuesService } from '@app/shared/services/default-values-service.service';
import { UtcOffsetModel } from '@app/shared/models/utc-offset-model';
import { DateTimeDisplayFormatModel } from '@app/shared/models/date-time-display-format-model';
import { DateTimeService } from '@app/shared/common/timing/date-time.service';
import { Observable, shareReplay, tap, catchError, of, map } from 'rxjs';

interface AbpEventSubscription {
    eventName: string;
    callback: (...args: any[]) => void;
}

@Component({
    template: '',
})
export abstract class AppComponentBase implements OnDestroy, OnInit {
    localizationSourceName = AppConsts.localization.defaultLocalizationSourceName;
    injector: Injector;

    localization: LocalizationService;
    permission: PermissionCheckerService;
    feature: FeatureCheckerService;
    notify: NotifyService;
    setting: SettingService;
    message: MessageService;
    multiTenancy: AbpMultiTenancyService;
    appSession: AppSessionService;
    primengTableHelper: PrimengTableHelper;
    ui: AppUiCustomizationService;
    appUrlService: AppUrlService;
    spinnerService: NgxSpinnerService;
    eventSubscriptions: AbpEventSubscription[] = [];

    private ngxSpinnerTextService: NgxSpinnerTextService;
    protected defaultValuesService: DefaultValuesService;
    protected dateTimeService: DateTimeService;

    // Default values fields
    defaultUtcOffset: UtcOffsetModel;
    defaultDateTimeDisplayFormat: DateTimeDisplayFormatModel;
    defaultFirstDayOfWeek: string;
    defaultNumberOfDecimals: number;
    defaultNumberOfDecimalsForPercentage: number;
    advancedParameterColors: any[];
    defaultColors: Record<string, string> = {};

    private _defaultValuesLoading$: Observable<void> | null = null;

    constructor(injector: Injector) {
        this.injector = injector;
        this.localization = injector.get(LocalizationService);
        this.permission = injector.get(PermissionCheckerService);
        this.feature = injector.get(FeatureCheckerService);
        this.notify = injector.get(NotifyService);
        this.setting = injector.get(SettingService);
        this.message = injector.get(MessageService);
        this.multiTenancy = injector.get(AbpMultiTenancyService);
        this.appSession = injector.get(AppSessionService);
        this.ui = injector.get(AppUiCustomizationService);
        this.appUrlService = injector.get(AppUrlService);
        this.primengTableHelper = new PrimengTableHelper();
        this.spinnerService = injector.get(NgxSpinnerService);
        this.ngxSpinnerTextService = injector.get(NgxSpinnerTextService);
        this.defaultValuesService = injector.get(DefaultValuesService);
        this.dateTimeService = injector.get(DateTimeService);

        this.initializeDefaultValues();
    }

    private initializeDefaultValues(): void {
        this.defaultUtcOffset = new UtcOffsetModel();
        this.defaultDateTimeDisplayFormat = new DateTimeDisplayFormatModel();
        this.defaultFirstDayOfWeek = 'Auto';
        this.defaultNumberOfDecimals = 2;
        this.defaultNumberOfDecimalsForPercentage = 2;
        this.advancedParameterColors = [];
        this.defaultColors = {};
    }

    protected ensureDefaultValuesLoaded(): Observable<void> {
        if (this._defaultValuesLoading$) {
            return this._defaultValuesLoading$;
        }

        this._defaultValuesLoading$ = this.defaultValuesService.getAllDefaultValues().pipe(
            tap((values) => {
                this.defaultUtcOffset = values.utcOffset;
                this.defaultDateTimeDisplayFormat = values.dateTimeDisplayFormat;
                this.defaultFirstDayOfWeek = values.firstDayOfWeek;
                this.defaultNumberOfDecimals = values.numberOfDecimals;
                this.defaultNumberOfDecimalsForPercentage = values.numberOfDecimalsForPercentage;
                this.advancedParameterColors = values.advancedParameterColors;
                this.defaultColors = values.defaultColors;
            }),
            map(() => void 0),
            catchError((error) => {
                console.error('Error loading default values:', error);
                return of(void 0);
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        return this._defaultValuesLoading$;
    }

    get currentTheme(): UiCustomizationSettingsDto {
        return this.appSession.theme;
    }

    get bgColor() {
        return this.appSession.theme.baseSettings.layout.darkMode ? '#1e1e2f' : '#F1F1F1';
    }

    get appLogoSkin(): string {
        if (this.currentTheme.isTopMenuUsed || this.currentTheme.isTabMenuUsed) {
            return this.currentTheme.baseSettings.layout.darkMode ? 'light' : 'dark';
        }

        return this.currentTheme.baseSettings.menu.asideSkin;
    }

    get containerClass(): string {
        if (this.appSession.theme.baseSettings.layout.layoutType === 'fluid') {
            return 'app-container container-fluid';
        } else if (
            this.appSession.theme.baseSettings.layout.layoutType === 'fixed' ||
            this.appSession.theme.baseSettings.layout.layoutType === 'fluid-xxl'
        ) {
            return 'app-container container-xxl';
        }

        return 'app-container container';
    }

    ngOnInit(): void {
        this.ensureDefaultValuesLoaded().subscribe();
    }

    ngOnDestroy(): void {
        this.unSubscribeAllEvents();
    }

    /**
     * Converts date format configuration to DevExtreme displayFormat string
     * @returns DevExtreme date format string for displayFormat property
     */
    protected getDevExtremeDateFormat(): string {
        return this.dateTimeService.getDevExtremeDateTimeFormat(this.defaultDateTimeDisplayFormat);
    }

    /**
     * Converts manual date format string (e.g., "dd/mm/yyyy") to Luxon format (e.g., "dd/MM/yyyy")
     * @returns Luxon date format string for date only
     */
    protected getLuxonDateFormat(): string {
        return this.dateTimeService.getLuxonDateFormat(this.defaultDateTimeDisplayFormat);
    }

    /**
     * Gets Luxon date-time format string (includes time)
     * @returns Luxon date-time format string
     */
    protected getLuxonDateTimeFormat(): string {
        return this.dateTimeService.getLuxonDateTimeFormat(this.defaultDateTimeDisplayFormat);
    }

    /**
     * Gets the first day of week as a number for calendar components
     * @returns 0 for Sunday, 1 for Monday, or null for Auto
     */
    protected getFirstDayOfWeekNumber(): number | null {
        if (this.defaultFirstDayOfWeek === 'Auto') {
            return null; // Let the component use default
        } else if (this.defaultFirstDayOfWeek === 'Sunday') {
            return 0;
        } else if (this.defaultFirstDayOfWeek === 'Monday') {
            return 1;
        }
        
        return null; // Default to Auto
    }

    flattenDeep(array) {
        return array.reduce(
            (acc, val) => (Array.isArray(val) ? acc.concat(this.flattenDeep(val)) : acc.concat(val)),
            [],
        );
    }

    l(key: string, ...args: any[]): string {
        args.unshift(key);
        args.unshift(this.localizationSourceName);
        return this.ls.apply(this, args);
    }

    ls(sourcename: string, key: string, ...args: any[]): string {
        let localizedText = this.localization.localize(key, sourcename);

        if (!localizedText) {
            localizedText = key;
        }

        if (!args || !args.length) {
            return localizedText;
        }

        args.unshift(localizedText);
        return abp.utils.formatString.apply(this, this.flattenDeep(args));
    }

    isGranted(permissionName: string): boolean {
        return this.permission.isGranted(permissionName);
    }

    isGrantedAny(...permissions: string[]): boolean {
        if (!permissions) {
            return false;
        }

        for (const permission of permissions) {
            if (this.isGranted(permission)) {
                return true;
            }
        }

        return false;
    }

    s(key: string): string {
        return abp.setting.get(key);
    }

    appRootUrl(): string {
        return this.appUrlService.appRootUrl;
    }

    showMessage(message: string, type: string, time: number = 3500): void {
        const position = {
            top: undefined,
            bottom: 20,
            left: undefined,
            right: 0,
        };

        const direction = 'up-push';

        notify(
            {
                message: message,
                width: 450,
                type: type,
                displayTime: time,
                animation: {
                    show: {
                        type: 'fade',
                        duration: 400,
                        from: 0,
                        to: 1,
                    },
                    hide: { type: 'fade', duration: 400, to: 0 },
                },
            },
            { position, direction },
        );
    }

    showMainSpinner(text?: string): void {
        this.ngxSpinnerTextService.setText(text);
        this.spinnerService.show();
    }

    hideMainSpinner(text?: string): void {
        this.spinnerService.hide();
    }

    protected subscribeToEvent(eventName: string, callback: (...args: any[]) => void): void {
        abp.event.on(eventName, callback);
        this.eventSubscriptions.push({
            eventName,
            callback,
        });
    }

    private unSubscribeAllEvents() {
        this.eventSubscriptions.forEach((s) => abp.event.off(s.eventName, s.callback));
        this.eventSubscriptions = [];
    }
}
