import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';

/* services */
import { FileDownloadService } from './file-download.service';
import { LocalStorageService } from './local-storage.service';
import { ScriptLoaderService } from './script-loader.service';
import { StyleLoaderService } from './style-loader.service';
import { ArrayToTreeConverterService } from './array-to-tree-converter.service';
import { TreeDataHelperService } from './tree-data-helper.service';
import { GuidGeneratorService } from './guid-generator.service';

/* standalone directives / components / pipes */
import { EqualValidator } from './validation/equal-validator.directive';
import { PasswordComplexityValidator } from './validation/password-complexity-validator.directive';
import { ButtonBusyDirective } from './button-busy.directive';
import { AutoFocusDirective } from './auto-focus.directive';
import { BusyIfDirective } from './busy-if.directive';
import { FriendProfilePictureComponent } from './friend-profile-picture.component';
import { LuxonFormatPipe } from './luxon-format.pipe';
import { LuxonFromNowPipe } from './luxon-from-now.pipe';
import { ValidationMessagesComponent } from './validation-messages.component';
import { NullDefaultValueDirective } from './null-value.directive';
import { DatePickerLuxonModifierDirective } from './date-time/date-picker-luxon-modifier.directive';
import { DateRangePickerLuxonModifierDirective } from './date-time/date-range-picker-luxon-modifier.directive';

/* shared standalone pipes */
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { PermissionAnyPipe } from '@shared/common/pipes/permission-any.pipe';
import { PermissionAllPipe } from '@shared/common/pipes/permission-all.pipe';
import { FeatureCheckerPipe } from '@shared/common/pipes/feature-checker.pipe';
import { RefreshWidgetPipe } from '@shared/common/pipes/refresh-widget.pipe';
import { RefreshWidgetUnitValuePipe } from '@shared/common/pipes/refresh-widget-unit-value.pipe';
import { RefreshWidgetCustomUnitValuePipe } from '@shared/common/pipes/refresh-widget-custom-unit-value.pipe';

@NgModule({
    imports: [
        CommonModule,

        /* standalone stuff */
        EqualValidator,
        PasswordComplexityValidator,
        ButtonBusyDirective,
        AutoFocusDirective,
        BusyIfDirective,
        FriendProfilePictureComponent,
        LuxonFormatPipe,
        LuxonFromNowPipe,
        ValidationMessagesComponent,
        NullDefaultValueDirective,
        LocalizePipe,
        PermissionPipe,
        PermissionAnyPipe,
        PermissionAllPipe,
        FeatureCheckerPipe,
        DatePickerLuxonModifierDirective,
        DateRangePickerLuxonModifierDirective,
        RefreshWidgetPipe,
        RefreshWidgetUnitValuePipe,
        RefreshWidgetCustomUnitValuePipe,
    ],
    providers: [
        FileDownloadService,
        LocalStorageService,
        ScriptLoaderService,
        StyleLoaderService,
        ArrayToTreeConverterService,
        TreeDataHelperService,
        GuidGeneratorService,
    ],
})
export class UtilsModule {}
