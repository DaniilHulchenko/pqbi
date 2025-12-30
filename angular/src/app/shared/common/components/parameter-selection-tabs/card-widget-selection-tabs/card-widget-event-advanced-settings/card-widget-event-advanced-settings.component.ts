import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { ExcludeFlagged, Limit, ColorSchema } from '@app/shared/enums/advanced-settings-options';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { uniqBy } from 'lodash-es';
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
import {
    NormalizeEnum,
    EventClassDescription,
    EventClass,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { AdvancedSettingsConfig } from '../../advanced-settings/advanced-settings.component';
import { CardWidgetAdvancedSettingsConfig } from '@app/shared/interfaces/CardWidgetAdvancedSettingsConfig';
import { EventService } from '@app/shared/services/event-service.service';
import { DashboardPagesService } from '@app/shared/services/dashboard-pages.service';
import { Subscription } from 'rxjs';
import { CardIconService } from '@app/shared/services/card-icon.service';
import { CardIcon } from '@app/shared/interfaces/card-icon';

@Component({
    selector: 'cardWidgetEventAdvancedSettings',
    standalone: true,
    imports: [
        DxPopupModule,
        DxScrollViewModule,
        DxRadioGroupModule,
        DxTextBoxModule,
        DxColorBoxModule,
        DxButtonModule,
        CommonModule,
        DxNumberBoxModule,
        DxSelectBoxModule,
        MultiSelectModule,
        FormsModule,
        CheckboxModule,
        RadioButtonModule,
        DxFileUploaderModule,
    ],
    templateUrl: './card-widget-event-advanced-settings.component.html',
    styleUrl: './card-widget-event-advanced-settings.component.css',
})
export class CardWidgetEventAdvancedSettingsComponent implements OnInit, OnChanges, OnDestroy {
    @Input() isBaseParameter = false;
    @Input() config: CardWidgetAdvancedSettingsConfig | null = null;
    @Output() configChange = new EventEmitter<CardWidgetAdvancedSettingsConfig>();

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

    decimalPoints = 2;
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
    icon: {
        id?: number | null;
        file: string;
        name?: string | null;
        appearance: 'always' | 'limits';
        colorMode: 'scheme' | 'custom';
        customColor: string;
        setAsDefault?: boolean;
    } = {
        id: null,
        file: null,
        name: null,
        appearance: 'always',
        colorMode: 'custom',
        customColor: '#000000',
        setAsDefault: false,
    };
    link = { page: null };

    availableIcons: CardIcon[] = [];
    selectedIconPreview: string | null = null;
    defaultIconId: number | null = null;
    setAsDefaultIcon = false;

    decimalPointOptions = [0, 1, 2, 3];

    get hasSelectedIcon(): boolean {
        return !!(this.icon?.id || this.icon?.file);
    }

    fontFamilies = ['Arial', 'Verdana', 'Tahoma', 'Times New Roman', 'Courier New'];


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
        private _eventService: EventService,
        private dashboardPagesService: DashboardPagesService,
        private cardIconService: CardIconService,
    ) {}

    ngOnInit() {
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

    onFileChanged(e: any) {
        const file = e.value[0];
        if (file) {
            this.cardIconService.uploadIcon(file).subscribe((icon) => this.applySelectedIcon(icon));
        }
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
        this.reset();
        this.modalVisible = false;
    }

    onSelectReplaceAggregation() {}

    save() {
        const config: CardWidgetAdvancedSettingsConfig = {
            parameterName: this.parameterName?.trim(),
            normalizeValue: this.normalizeValue,
            normalizeNominalValue: this.normalizeNominalValue,
            excludeFlagged: this.excludeFlagged,
            defaultFlagEvent: this.excludeFlagged === ExcludeFlagged.UserSelected ? this.selectedFlagEvents : [],
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

    private reset() {
        this.normalizeValue = NormalizeEnum.NO;
        this.normalizeNominalValue = 0;
        this.excludeFlagged = ExcludeFlagged.None;
        this.selectedFlagEvents = [];
        this.parameterName = '';
        // this.setLimits = Limit.None;
        // this.lowerLimit = 0;
        // this.upperLimit = 0;
        // this.limitFromNominal = false;
        // this.limitFromNormalization = false;
        // this.colorScheme = ColorSchema.None;
        // this.outOfLimitColor = '';
        // this.gradientFromColor = '';
        // this.gradientToColor = '';
        // this.okColor = '';
        // this.noDataColor = '';
        // this.useOkColor = false;
        // this.useNoDataColor = false;
        // this.showOkColor = false;
        // this.showNoDataColor = false;
        this.decimalPoints = 2;
        this.link.page = null;
        this.icon = this.normalizeIconSettings(null);
        this.selectedIconPreview = null;
        this.setAsDefaultIcon = false;
        this.titleFont = this.normalizeFontSettings(null);
        this.valueFont = this.normalizeFontSettings(null);
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
        });
        this.selectedIconPreview = null;
        this.setAsDefaultIcon = false;
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

    private normalizeIconSettings(
icon:
            | {
                  id?: number | null;
                  file?: string | null;
                  name?: string | null;
                  appearance?: 'always' | 'limits';
                  colorMode?: 'scheme' | 'custom';
                  customColor?: string;
                  setAsDefault?: boolean;
              }
            | null,
    ): {
        id?: number | null;
        file: string;
        name?: string | null;
        appearance: 'always' | 'limits';
        colorMode: 'custom';
        customColor: string;
        setAsDefault?: boolean;
    } {        
        return {
            id: icon?.id ?? null,
            file: icon?.file ?? null,
            name: icon?.name ?? null,
            appearance: icon?.appearance ?? 'always',
            colorMode: 'custom',
            customColor: icon?.customColor ?? '#000000',
            setAsDefault: icon?.setAsDefault,
        };
    }
    private prepareIconForSave() {
        return {
            id: this.icon.id,
            name: this.icon.name,
            file: null,
            appearance: this.icon.appearance,
            colorMode: this.icon.colorMode,
            customColor: this.icon.customColor,
            setAsDefault: this.setAsDefaultIcon,
        } as CardWidgetAdvancedSettingsConfig['icon'];
    }
}
