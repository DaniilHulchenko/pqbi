import { Component, Injector, OnInit } from '@angular/core';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DefaultValuesServiceProxy, GetDefaultValueForEditOutput, CreateOrEditDefaultValueDto, SettingScopes, TimingServiceProxy, NameValueDto } from '@shared/service-proxies/service-proxies';
import { DefaultValueKeys } from '@shared/DefaultValueKeys';
import { finalize } from 'rxjs/operators';
import { DefaultValuesService } from '@app/shared/services/default-values-service.service';
import { UtcOffsetModel } from '@app/shared/models/utc-offset-model';
import { DateTimeDisplayFormatModel } from '@app/shared/models/date-time-display-format-model';

@Component({
    templateUrl: './localization.component.html',
    styleUrls: ['./localization.component.less'],
    animations: [appModuleAnimation()],
})
export class LocalizationComponent extends AppComponentBase implements OnInit {
    // Date & Time Display Format
    dateTimeDisplayFormatMode: 'auto' | 'custom' | 'manual' = 'auto';
    customCulture: string = '';
    manualDateFormat: string = 'dd/mm/yyyy';
    manualTimeFormat: string = '24 hours';
    
    // Date format options for manual mode
    dateFormatOptions: { value: string; label: string }[] = [
        { value: 'dd/mm/yyyy', label: 'dd/mm/yyyy' },
        { value: 'mm/dd/yyyy', label: 'mm/dd/yyyy' },
        { value: 'yyyy-mm-dd', label: 'yyyy-mm-dd' },
        { value: 'dd.mm.yyyy', label: 'dd.mm.yyyy' },
        { value: 'mm/dd/yy', label: 'mm/dd/yy' },
        { value: 'dd-mm-yyyy', label: 'dd-mm-yyyy' },
    ];

    // Time format options for manual mode
    timeFormatOptions: { value: string; label: string }[] = [
        { value: '24 hours', label: '24 hours' },
        { value: '12 hours', label: '12 hours' },
    ];

    // Available cultures
    cultures: abp.localization.ILanguageInfo[] = [];

    // Date and Time Display Format model
    private dateTimeDisplayFormatModel: DateTimeDisplayFormatModel = new DateTimeDisplayFormatModel();

    firstDayOfWeek: string = 'Auto';
    firstDayOfWeekOptions: string[] = ['Auto', 'Sunday', 'Monday'];

    // UTC Offset
    utcOffsetMode: 'timezone' | 'custom' | 'manual' = 'timezone';
    utcOffsetModeOptions = [
        { value: 'timezone', text: 'Time zone' },
        { value: 'custom', text: 'Custom time zone' },
        { value: 'manual', text: 'Manual' },
    ];
    customTimeZone: string = '';
    manualUtcOffset: number = 0;
    timeZones: NameValueDto[] = [];

    // UTC Offset model
    private utcOffsetModel: UtcOffsetModel = new UtcOffsetModel();

    // Original values from DB (for cancel functionality)
    private originalValues = {
        dateTimeDisplayFormat: '',
        firstDayOfWeek: '',
        utcOffset: '',
    };

    // IDs from DB (for update functionality)
    private valueIds: { [key: string]: number | undefined } = {};

    saving = false;

    constructor(injector: Injector,
        private defaultValuesServiceProxy: DefaultValuesServiceProxy,
        private timingService: TimingServiceProxy) {
        super(injector);
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.loadCultures();
        this.loadTimeZones();
        this.loadDefaultValues();
    }

    private loadCultures(): void {
        this.cultures = abp.localization.languages.filter((l) => !l.isDisabled);
    }

    private loadTimeZones(): void {
        this.timingService.getTimezones(SettingScopes.Application).subscribe((result) => {
            this.timeZones = result.items || [];
        });
    }

    private loadDefaultValues(): void {
        const keys = [
            DefaultValueKeys.dateTimeDisplayFormatSettingName,
            DefaultValueKeys.defaultFirstDayOfWeekSettingName,
            DefaultValueKeys.utcOffsetSettingName,
        ];

        this.defaultValuesServiceProxy.getDefaultValueByNames(keys).subscribe((result: GetDefaultValueForEditOutput[]) => {
            result.forEach((item: GetDefaultValueForEditOutput) => {
                const key = item.defaultValue?.name;
                const value = item.defaultValue?.value;
                const id = item.defaultValue?.id;

                if (!key) {
                    return;
                }

                // Save ID for update
                this.valueIds[key] = id;

                if (!value) {
                    return;
                }

                // Map settings
                if (key === DefaultValueKeys.dateTimeDisplayFormatSettingName) {
                    const model = DateTimeDisplayFormatModel.fromJson(value);
                    if (model) {
                        this.dateTimeDisplayFormatModel = model;
                        this.dateTimeDisplayFormatMode = model.mode;
                        this.customCulture = model.customCulture;
                        this.manualDateFormat = model.manualDateFormat;
                        this.manualTimeFormat = model.manualTimeFormat;
                        this.originalValues.dateTimeDisplayFormat = value;
                    } else {
                        // Invalid format, use defaults
                        this.dateTimeDisplayFormatModel = new DateTimeDisplayFormatModel();
                        this.dateTimeDisplayFormatMode = this.dateTimeDisplayFormatModel.mode;
                        this.customCulture = this.dateTimeDisplayFormatModel.customCulture;
                        this.manualDateFormat = this.dateTimeDisplayFormatModel.manualDateFormat;
                        this.manualTimeFormat = this.dateTimeDisplayFormatModel.manualTimeFormat;
                        this.originalValues.dateTimeDisplayFormat = this.dateTimeDisplayFormatModel.toJson();
                    }
                } else if (key === DefaultValueKeys.defaultFirstDayOfWeekSettingName) {
                    // Validate that value is one of the allowed options
                    if (this.firstDayOfWeekOptions.includes(value)) {
                        this.firstDayOfWeek = value;
                        this.originalValues.firstDayOfWeek = value;
                    } else {
                        // If value from DB is not in options, use default
                        this.firstDayOfWeek = 'Auto';
                        this.originalValues.firstDayOfWeek = 'Auto';
                    }
                } else if (key === DefaultValueKeys.utcOffsetSettingName) {
                    const model = UtcOffsetModel.fromJson(value);
                    if (model) {
                        this.utcOffsetModel = model;
                        this.utcOffsetMode = model.mode;
                        this.customTimeZone = model.customTimeZone;
                        this.manualUtcOffset = model.manualUtcOffset;
                        this.originalValues.utcOffset = value;
                    } else {
                        // Invalid format, use defaults
                        this.utcOffsetModel = new UtcOffsetModel();
                        this.utcOffsetMode = this.utcOffsetModel.mode;
                        this.customTimeZone = this.utcOffsetModel.customTimeZone;
                        this.manualUtcOffset = this.utcOffsetModel.manualUtcOffset;
                        this.originalValues.utcOffset = this.utcOffsetModel.toJson();
                    }
                }
            });
        });
    }


    saveChanges(): void {
        this.saving = true;

        // Update Date and Time Display Format model with current values
        this.dateTimeDisplayFormatModel.mode = this.dateTimeDisplayFormatMode;
        this.dateTimeDisplayFormatModel.customCulture = this.customCulture;
        this.dateTimeDisplayFormatModel.manualDateFormat = this.manualDateFormat;
        this.dateTimeDisplayFormatModel.manualTimeFormat = this.manualTimeFormat;

        // Update UTC offset model with current values
        this.utcOffsetModel.mode = this.utcOffsetMode;
        this.utcOffsetModel.customTimeZone = this.customTimeZone;
        this.utcOffsetModel.manualUtcOffset = this.manualUtcOffset;

        const valuesToSave: CreateOrEditDefaultValueDto[] = [
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.dateTimeDisplayFormatSettingName],
                name: DefaultValueKeys.dateTimeDisplayFormatSettingName,
                value: this.dateTimeDisplayFormatModel.toJson(),
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.defaultFirstDayOfWeekSettingName],
                name: DefaultValueKeys.defaultFirstDayOfWeekSettingName,
                value: this.firstDayOfWeek,
            }),
            new CreateOrEditDefaultValueDto({
                id: this.valueIds[DefaultValueKeys.utcOffsetSettingName],
                name: DefaultValueKeys.utcOffsetSettingName,
                value: this.utcOffsetModel.toJson(),
            }),
        ];

        this.defaultValuesServiceProxy
            .createOrEditValues(valuesToSave)
            .pipe(
                finalize(() => {
                    this.saving = false;
                }),
            )
            .subscribe({
                next: () => {
                    // Update original values after successful save
                    this.originalValues.dateTimeDisplayFormat = this.dateTimeDisplayFormatModel.toJson();
                    this.originalValues.firstDayOfWeek = this.firstDayOfWeek;
                    this.originalValues.utcOffset = this.utcOffsetModel.toJson();

                    // Update cache with new values
                    this.defaultValuesService.updateDateTimeDisplayFormat(this.dateTimeDisplayFormatModel);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.defaultFirstDayOfWeekSettingName, this.firstDayOfWeek);
                    this.defaultValuesService.cacheValue(DefaultValueKeys.utcOffsetSettingName, this.utcOffsetModel.toJson());

                    this.notify.success(this.l('SettingsSavedSuccessfully'));
                },
                error: () => {
                    this.notify.error(this.l('ErrorOccurredWhileSavingSettings'));
                },
            });
    }

    cancel(): void {
        // Restore original values from DB
        if (this.originalValues.dateTimeDisplayFormat) {
            const model = DateTimeDisplayFormatModel.fromJson(this.originalValues.dateTimeDisplayFormat);
            if (model) {
                this.dateTimeDisplayFormatModel = model;
                this.dateTimeDisplayFormatMode = model.mode;
                this.customCulture = model.customCulture;
                this.manualDateFormat = model.manualDateFormat;
                this.manualTimeFormat = model.manualTimeFormat;
            } else {
                this.dateTimeDisplayFormatModel = new DateTimeDisplayFormatModel();
                this.dateTimeDisplayFormatMode = this.dateTimeDisplayFormatModel.mode;
                this.customCulture = this.dateTimeDisplayFormatModel.customCulture;
                this.manualDateFormat = this.dateTimeDisplayFormatModel.manualDateFormat;
                this.manualTimeFormat = this.dateTimeDisplayFormatModel.manualTimeFormat;
            }
        }
        this.firstDayOfWeek = this.originalValues.firstDayOfWeek || this.firstDayOfWeek;

        // Restore UTC offset from JSON
        if (this.originalValues.utcOffset) {
            const model = UtcOffsetModel.fromJson(this.originalValues.utcOffset);
            if (model) {
                this.utcOffsetModel = model;
                this.utcOffsetMode = model.mode;
                this.customTimeZone = model.customTimeZone;
                this.manualUtcOffset = model.manualUtcOffset;
            } else {
                // If parsing fails, use defaults
                this.utcOffsetModel = new UtcOffsetModel();
                this.utcOffsetMode = this.utcOffsetModel.mode;
                this.customTimeZone = this.utcOffsetModel.customTimeZone;
                this.manualUtcOffset = this.utcOffsetModel.manualUtcOffset;
            }
        }

        this.notify.info(this.l('ChangesCancelled'));
    }
}
