import { Component, EventEmitter, Input, Injector, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { uniqBy } from 'lodash-es';
import { NormalizeEnum, EventClassDescription, EventClass } from '@shared/service-proxies/service-proxies';
import { AdvancedSettingsConfig } from '../../advanced-settings/advanced-settings.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    DxPopupModule,
    DxScrollViewModule,
    DxRadioGroupModule,
    DxTextBoxModule,
    DxColorBoxModule,
    DxButtonModule,
    DxNumberBoxModule,
    DxSelectBoxModule,
    DxFileUploaderModule,
} from 'devextreme-angular';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { RadioButtonModule } from 'primeng/radiobutton';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EventService } from '@app/shared/services/event-service.service';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';
import { CardIcon } from '@app/shared/interfaces/card-icon';
import { CardIconService } from '@app/shared/services/card-icon.service';

type IconSettings = CardWidgetAdvancedSettingsConfig['icon'];
type IconColorMode = IconSettings['colorMode'];
type IconSettingsInput = (Partial<IconSettings> & { colorMode?: string | IconColorMode }) | null;

@Component({
    selector: 'cardWidgetParameterAdvancedSettings',
    //standalone: true
,
    imports: [
        DxPopupModule,
        DxScrollViewModule,
        DxRadioGroupModule,
        DxTextBoxModule,
        DxColorBoxModule,
        DxButtonModule,
        DxFileUploaderModule,
        CommonModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        MultiSelectModule,
        FormsModule,
        CheckboxModule,
        RadioButtonModule,
    ],
    templateUrl: './card-widget-parameter-advanced-settings.component.html',
    styleUrl: './card-widget-parameter-advanced-settings.component.css',
})
export class CardWidgetParameterAdvancedSettingsComponent
    extends AppComponentBase
    implements OnInit, OnChanges, OnDestroy
{
    @Output() advancedSettingsChanged = new EventEmitter<AdvancedSettingsConfig>();
    @Input() config: CardWidgetAdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<CardWidgetAdvancedSettingsConfig>();

    readonly defaultIconSize = 32;
    readonly defaultIconSizeUnit = 'px';
    readonly iconPreviewSizePx = 32;
    readonly iconSizeUnits = ['px', 'em', 'rem', '%', 'vw', 'vh', 'vmin', 'vmax'];

    modalVisible = false;
    parameterName = '';
    normalizationOptions: any[];
    normalizeTypes = NormalizeEnum;
    excludeFlaggedTypes = ExcludeFlagged;
    limitTypes = Limit;
    colorSchemaTypes = ColorSchema;
    flaggingEvents: EventClassDescription[] = [];

    normalizeValue = NormalizeEnum.NO;
    normalizeNominalValue = 0;
    excludeFlagged = ExcludeFlagged.None;
    selectedFlagEvents: EventClass[] = [];
    setLimits = Limit.None;
    lowerLimit = 0;
    upperLimit = 0;
    limitFromNominal = false;
    limitFromNormalization = false;
    colorScheme = ColorSchema.None;
    outOfLimitColor = '';
    gradientFromColor = '';
    gradientToColor = '';
    okColor = '';
    noDataColor = '';
    useOkColor = false;
    useNoDataColor = false;
    showOkColor?: boolean;
    showNoDataColor?: boolean;
    decimalPoints = null;
    titleFont: { family?: string; size?: number | null; colorMode?: 'scheme' | 'custom'; customColor?: string } = {
        family: '',
        size: null,
        colorMode: 'custom',
        customColor: '#000000',
    };
    valueFont: { family?: string; size?: number | null; colorMode?: 'scheme' | 'custom'; customColor?: string } = {
        family: '',
        size: null,
        colorMode: 'custom',
        customColor: '#000000',
    };
        icon: IconSettings = {
        id: null,
        file: null,
        name: null,
        size: this.defaultIconSize,
        sizeUnit: this.defaultIconSizeUnit,
        appearance: 'always',
        colorMode: this.normalizeColorMode(),
        customColor: '#000000',
        setAsDefault: false,
    };
    link = { page: null };

    availableIcons: CardIcon[] = [];
    selectedIconPreview: string | null = null;
    setAsDefaultIcon = false;
    defaultIconId: number | null = null;
    uploadFailedMessage = '';
    isUploadFailed = false;

    decimalPointOptions = [0, 1, 2, 3];

    get hasSelectedIcon(): boolean {
        return !!(this.icon?.id || this.icon?.file);
    }

    fontFamilies = ['Arial', 'Verdana', 'Tahoma', 'Times New Roman', 'Courier New'];

    get iconSizeValue(): string {
        const size = this.icon?.size ?? this.defaultIconSize;
        const unit = this.icon?.sizeUnit ?? this.defaultIconSizeUnit;
        return `${size}${unit}`;
    }


    appearanceOptions = [
        { id: 'always', text: 'show always' },
        { id: 'limits', text: 'show only if limits are exceeded' },
    ];

    pqbiPages: { id: string; name: string }[] = [];

    private subscription: Subscription;

    excludeFlaggedOptions = [
        { value: ExcludeFlagged.DefaultEvents, text: 'yes  (with Dip, Swell, Interrupt)' },
        { value: ExcludeFlagged.UserSelected, text: 'yes - with selected events' },
    ];

    limitOptions = [
        { value: Limit.Fixed, text: 'lower limit / upper limit' },
        { value: Limit.PercentNominal, text: 'lower limit % from nominal / Upper limit % from nominal' },
        {
            value: Limit.PercentNormalization,
            text: 'lower limit from Normalization value / Upper limit from Normalization value',
        },
    ];

    colorSchemaOptions = [
        { value: ColorSchema.None, text: 'No color highlight' },
        { value: ColorSchema.OutOfLimit, text: 'Out-of-limit color' },
        { value: ColorSchema.Gradient, text: 'Out of limits gradient - from:   to:' },
    ];

    aggregationFuncOptions = [
        { value: 'AVG', text: 'Average' },
        { value: 'MAX', text: 'Maximum' },
        { value: 'MIN', text: 'Minimum' },
    ];

constructor(
        injector: Injector,
        private _eventService: EventService,
        private dashboardPagesService: DashboardPagesService,
        private cardIconService: CardIconService,
    ) {
        super(injector);
    }

    onFileChanged(e: any) {
        const file = e.value?.[0];
        this.isUploadFailed = false;

        if (!file) {
            return;
        }

        const uploader = e.component;

        this.cardIconService.uploadIcon(file).subscribe({
            next: (icon) => {
                this.applySelectedIcon(icon);
                this.isUploadFailed = false;
                uploader?.reset();
            },
            error: () => {
                this.isUploadFailed = true;
                this.notify.error(this.uploadFailedMessage);
                uploader?.reset();
            },
        });
    }

    ngOnInit() {
        super.ngOnInit();
        this.uploadFailedMessage = this.l('CardIconUploadFailed');
        this.normalizationOptions = this.getNormalizationOptions();
        this._eventService.pqsEvents().subscribe((evts) => {
            this.flaggingEvents = evts;
            this.flaggingEvents = uniqBy(this.flaggingEvents, (x) => x.eventClass);
        });
        this.subscription = this.dashboardPagesService.getPages().subscribe((pages) => {
            this.pqbiPages = pages.map((page) => ({ id: page.id, name: page.name }));
        });
        this.cardIconService.getAvailableIcons().subscribe((icons) => (this.availableIcons = icons));
        this.cardIconService.getDefaultIconId().subscribe((id) => {
            this.defaultIconId = id;
            this.setAsDefaultIcon = this.icon?.id === id;
            this.ensurePreview();
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.config && this.config) {
            const c = this.config;
            this.parameterName = c.parameterName ?? '';
            this.normalizeValue = c.normalizeValue;
            this.normalizeNominalValue = +c.normalizeNominalValue;
            this.excludeFlagged = c.excludeFlagged;
            this.selectedFlagEvents = c.defaultFlagEvent ?? null;
            this.setLimits = c.setLimits;
            this.lowerLimit = +c.lowerLimit;
            this.upperLimit = +c.upperLimit;
            this.limitFromNominal = c.limitFromNominal;
            this.limitFromNormalization = c.limitFromNormalization;
            this.colorScheme = c.colorScheme;
            this.outOfLimitColor = c.outOfLimitColor;
            this.gradientFromColor = c.gradientFromColor;
            this.gradientToColor = c.gradientToColor;
            this.okColor = c.okColor;
            this.noDataColor = c.noDataColor;
            this.useOkColor = !!this.okColor;
            this.useNoDataColor = !!this.noDataColor;
            this.decimalPoints = c.decimalPoints;
            this.link.page = c.linkPage;
            this.icon = this.normalizeIconSettings(c.icon);
            this.setAsDefaultIcon = this.icon?.id != null && this.icon.id === this.defaultIconId;
            this.ensurePreview();
            this.titleFont = this.normalizeFontSettings(c.titleFont);
            this.valueFont = this.normalizeFontSettings(c.valueFont);
        }
    }
    ngOnDestroy(): void {
        this.subscription?.unsubscribe();
    }

    getNormalizationOptions() {
        const opts = [];
        // if (this.isBaseParameter) {
        opts.push({ value: NormalizeEnum.NOMINAL, text: 'yes - by nominal value' });
        // }
        opts.push({ value: NormalizeEnum.VALUE, text: 'yes - by Normalization Value:' });
        return opts;
    }

    show(settings: CardWidgetAdvancedSettingsConfig | null = null) {
        if (settings) {
            this.config = settings;
            this.ngOnChanges({
                config: { currentValue: settings, previousValue: null, firstChange: true, isFirstChange: () => true },
            });
        }
        this.modalVisible = true;
    }

    hide() {
        this.modalVisible = false;
        this.reset();
    }

    onSelectReplaceAggregation() {}

    save() {
        const config: CardWidgetAdvancedSettingsConfig = {
            parameterName: this.parameterName?.trim(),
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent: this.excludeFlagged === ExcludeFlagged.UserSelected ? this.selectedFlagEvents : [],
            setLimits: this.setLimits,
            lowerLimit: this.lowerLimit,
            upperLimit: this.upperLimit,
            limitFromNominal: this.limitFromNominal,
            limitFromNormalization: this.limitFromNormalization,
            colorScheme: this.colorScheme,
            outOfLimitColor: this.outOfLimitColor,
            gradientFromColor: this.gradientFromColor,
            gradientToColor: this.gradientToColor,
            okColor: this.okColor,
            noDataColor: this.noDataColor,
            showOkColor: this.useOkColor,
            showNoDataColor: this.useNoDataColor,
            decimalPoints: this.decimalPoints,
            linkPage: this.link.page,
            icon: this.prepareIconForSave(),
            titleFont: this.titleFont,
            valueFont: this.valueFont,
        };
        this.configChange.emit(config);
        this.hide();
    }
    onSelectNormalize(value: NormalizeEnum) {
        this.normalizeValue = this.normalizeValue === value ? NormalizeEnum.NO : value;
    }
    onSelectExcludeFlagged(value: ExcludeFlagged) {
        this.excludeFlagged = this.excludeFlagged === value ? null : value;
    }
    onSelectLimitType(value: Limit) {
        this.setLimits = this.setLimits === value ? null : value;
    }
    onSelectColorScheme(value: ColorSchema) {
        this.colorScheme = this.colorScheme === value ? null : value;
    }
    onColorClosed(type: 'from' | 'to') {
        if (type === 'from' && !this.gradientFromColor) {
            this.gradientFromColor = '#000000';
        }
        if (type === 'to' && !this.gradientToColor) {
            this.gradientToColor = '#000000';
        }
    }
    reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        this.parameterName = '';
        this.setLimits = Limit.None;
        this.lowerLimit = 0;
        this.upperLimit = 0;
        this.limitFromNominal = false;
        this.limitFromNormalization = false;
        this.colorScheme = ColorSchema.None;
        this.outOfLimitColor = '';
        this.gradientFromColor = '';
        this.gradientToColor = '';
        this.okColor = '';
        this.noDataColor = '';
        this.useOkColor = false;
        this.useNoDataColor = false;
        this.showOkColor = false;
        this.showNoDataColor = false;
        this.decimalPoints = null;
        this.link.page = null;
        this.icon = this.normalizeIconSettings(null);
        this.setAsDefaultIcon = false;
        this.selectedIconPreview = null;
        this.titleFont = this.normalizeFontSettings(null);
        this.valueFont = this.normalizeFontSettings(null);
        this.isUploadFailed = false;
    }

    onIconSelected(iconId: number) {
        if (!iconId) {
            this.resetIcon();
            return;
        }

        const icon = this.availableIcons.find((i) => i.id === iconId);
        if (icon) {
            this.applySelectedIcon(icon);
            return;
        }

        this.cardIconService.getIconById(iconId).subscribe((fetchedIcon) => {
            if (fetchedIcon) {
                this.applySelectedIcon(fetchedIcon);
            }
        });
    }

    resetIcon() {
        this.icon = this.normalizeIconSettings({
            appearance: this.icon.appearance,
            colorMode: this.icon.colorMode,
            customColor: this.icon.customColor,
            size: this.icon.size,
            sizeUnit: this.icon.sizeUnit,
        });
        this.setAsDefaultIcon = false;
        this.selectedIconPreview = null;
    }

    private applySelectedIcon(icon: CardIcon) {
        this.icon = {
            ...this.icon,
            id: icon.id,
            name: icon.name,
            file: icon.content,
        };
        this.selectedIconPreview = icon.content;
        this.setAsDefaultIcon = false;
    }

    private ensurePreview() {
        if (this.icon?.file) {
            this.selectedIconPreview = this.icon.file;
            return;
        }

        if (this.icon?.id) {
            this.cardIconService.getIconById(this.icon.id).subscribe((icon) => {
                if (icon) {
                    this.selectedIconPreview = icon.content;
                    this.icon.file = icon.content;
                    this.icon.name = icon.name;
                }
            });
        }
    }

    private normalizeFontSettings(
        font: { family?: string; size?: number | null; colorMode?: 'scheme' | 'custom'; customColor?: string } | null,
    ): { family: string; size?: number | null; colorMode: 'custom'; customColor: string } {
        return {
            family: font?.family ?? '',
            size: font?.size ?? null,
            colorMode: 'custom',
            customColor: font?.customColor ?? '#000000',
        };
    }

    private normalizeIconSettings(icon: IconSettingsInput): IconSettings {
        return {
            id: icon?.id ?? null,
            file: icon?.file ?? null,
            name: icon?.name ?? null,
            size: icon?.size ?? this.defaultIconSize,
            sizeUnit: icon?.sizeUnit ?? this.defaultIconSizeUnit,
            appearance: icon?.appearance ?? 'always',
            colorMode: this.normalizeColorMode(icon?.colorMode),
            customColor: icon?.customColor ?? '#000000',
            setAsDefault: icon?.setAsDefault,
        };
    }
    private normalizeColorMode(colorMode?: string | IconColorMode): IconColorMode {
        return colorMode === 'custom' ? 'custom' : 'scheme';
    }

    private prepareIconForSave(): IconSettings {
        return {
            id: this.icon.id,
            name: this.icon.name,
            file: null,
            size: this.icon.size,
            sizeUnit: this.icon.sizeUnit,
            appearance: this.icon.appearance,
            colorMode: this.icon.colorMode,
            customColor: this.icon.customColor,
            setAsDefault: this.setAsDefaultIcon,
        };
    }
}
