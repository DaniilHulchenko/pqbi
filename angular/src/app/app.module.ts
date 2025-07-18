import { CommonModule } from '@angular/common';
import { APP_INITIALIZER, Injector, NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HttpClientJsonpModule } from '@angular/common/http';
import { ChatSignalrService } from '@app/shared/layout/chat/chat-signalr.service';
import { LinkAccountModalComponent } from '@app/shared/layout/link-account-modal.component';
import { LinkedAccountsModalComponent } from '@app/shared/layout/linked-accounts-modal.component';
import { UserDelegationsModalComponent } from '@app/shared/layout/user-delegations-modal.component';
import { CreateNewUserDelegationModalComponent } from '@app/shared/layout/create-new-user-delegation-modal.component';
import { ChangePasswordModalComponent } from '@app/shared/layout/profile/change-password-modal.component';
import { MySettingsModalComponent } from '@app/shared/layout/profile/my-settings-modal.component';
import { SmsVerificationModalComponent } from '@app/shared/layout/profile/sms-verification-modal.component';
import { ServiceProxyModule } from '@shared/service-proxies/service-proxy.module';
import { UtilsModule } from '@shared/utils/utils.module';
import { FileUploadModule } from 'ng2-file-upload';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { TooltipModule } from 'ngx-bootstrap/tooltip';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { FileUploadModule as PrimeNgFileUploadModule } from 'primeng/fileupload';
import { PaginatorModule } from 'primeng/paginator';
import { ProgressBarModule } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { ImpersonationService } from './admin/users/impersonation.service';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DefaultLayoutComponent } from './shared/layout/themes/default/default-layout.component';
import { AppCommonModule } from './shared/common/app-common.module';
import { ChatBarComponent } from './shared/layout/chat/chat-bar.component';
import { ThemeSelectionPanelComponent } from './shared/layout/theme-selection/theme-selection-panel.component';
import { ChatFriendListItemComponent } from './shared/layout/chat/chat-friend-list-item.component';
import { ChatMessageComponent } from './shared/layout/chat/chat-message.component';
import { FooterComponent } from './shared/layout/footer.component';
import { LinkedAccountService } from './shared/layout/linked-account.service';
import { SideBarMenuComponent } from './shared/layout/nav/side-bar-menu.component';
import { TopBarMenuComponent } from './shared/layout/nav/top-bar-menu.component';
import { QuickThemeSelectionComponent } from './shared/layout/topbar/quick-theme-selection.component';
import { LanguageSwitchDropdownComponent } from './shared/layout/topbar/language-switch-dropdown.component';
import { ChatToggleButtonComponent } from './shared/layout/topbar/chat-toggle-button.component';
import { SubscriptionNotificationBarComponent } from './shared/layout/topbar/subscription-notification-bar.component';
import { UserMenuComponent } from './shared/layout/topbar/user-menu.component';
import { DefaultBrandComponent } from './shared/layout/themes/default/default-brand.component';
import { UserNotificationHelper } from './shared/layout/notifications/UserNotificationHelper';
import { HeaderNotificationsComponent } from './shared/layout/notifications/header-notifications.component';
import { NotificationSettingsModalComponent } from './shared/layout/notifications/notification-settings-modal.component';
import { NotificationsComponent } from './shared/layout/notifications/notifications.component';
import { ImageCropperModule } from 'ngx-image-cropper';
import { ActiveDelegatedUsersComboComponent } from './shared/layout/topbar/active-delegated-users-combo.component';

import { DefaultLogoComponent } from './shared/layout/themes/default/default-logo.component';
import { IMaskModule } from 'angular-imask';
// Metronic
import { PerfectScrollbarModule } from '@craftsjs/perfect-scrollbar';

import { SessionTimeoutModalComponent } from './shared/common/session-timeout/session-timeout-modal-component';
import { SessionTimeoutComponent } from './shared/common/session-timeout/session-timeout.component';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { MenuSearchBarComponent } from './shared/layout/nav/menu-search-bar/menu-search-bar.component';
import { NgxSpinnerModule, NgxSpinnerComponent } from 'ngx-spinner';
import { ScrollTopComponent } from './shared/layout/scroll-top.component';
import { AppBsModalModule } from '@shared/common/appBsModal/app-bs-modal.module';
import { SubheaderModule } from './shared/common/sub-header/subheader.module';
import { ChangeProfilePictureModalModule } from './shared/layout/profile/change-profile-picture-modal.module';
import { ToggleDarkModeComponent } from './shared/layout/toggle-dark-mode/toggle-dark-mode.component';
import { ActivatedRoute, Router } from '@angular/router';
import { EnableTwoFactorAuthenticationModalComponent } from './shared/layout/profile/enable-two-factor-authentication-modal.component';
import { RecoveryCodesComponent } from './shared/layout/profile/recovery-codes.component';
import { VerifyCodeModalComponent } from './shared/layout/profile/verify-code-modal.component';
import { ViewRecoveryCodesModalComponent } from './shared/layout/profile/view-recovery-codes-modal.component';
import { AddFriendModalComponent } from './shared/layout/chat/add-friend-modal.component';
import { AddFromDifferentTenantModalComponent } from './shared/layout/chat/add-from-different-tenant-modal.component';
import { FriendsLookupTableComponent } from './shared/layout/chat/friends-lookup-table.component';

@NgModule({
    declarations: [
        AppComponent,
        DefaultLayoutComponent,
        HeaderNotificationsComponent,
        SideBarMenuComponent,
        TopBarMenuComponent,
        FooterComponent,
        ScrollTopComponent,
        LinkedAccountsModalComponent,
        UserDelegationsModalComponent,
        CreateNewUserDelegationModalComponent,
        LinkAccountModalComponent,
        ChangePasswordModalComponent,
        MySettingsModalComponent,
        SmsVerificationModalComponent,
        EnableTwoFactorAuthenticationModalComponent,
        RecoveryCodesComponent,
        VerifyCodeModalComponent,
        ViewRecoveryCodesModalComponent,
        NotificationsComponent,
        ChatBarComponent,
        AddFriendModalComponent,
        AddFromDifferentTenantModalComponent,
        FriendsLookupTableComponent,
        ThemeSelectionPanelComponent,
        ChatFriendListItemComponent,
        NotificationSettingsModalComponent,
        ChatMessageComponent,
        QuickThemeSelectionComponent,
        LanguageSwitchDropdownComponent,
        ChatToggleButtonComponent,
        SubscriptionNotificationBarComponent,
        UserMenuComponent,
        DefaultBrandComponent,
        SessionTimeoutModalComponent,
        SessionTimeoutComponent,
        MenuSearchBarComponent,
        ActiveDelegatedUsersComboComponent,
        DefaultLogoComponent,
        ToggleDarkModeComponent,
    ],
    providers: [
        ImpersonationService,
        LinkedAccountService,
        UserNotificationHelper,
        ChatSignalrService,
        {
            provide: APP_INITIALIZER,
            useFactory: appInitializerFactory,
            multi: true,
        },
    ],
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        HttpClientJsonpModule,
        ModalModule.forRoot(),
        TooltipModule.forRoot(),
        TabsModule.forRoot(),
        BsDropdownModule.forRoot(),
        PopoverModule.forRoot(),
        BsDatepickerModule.forRoot(),
        FileUploadModule,
        AppRoutingModule,
        UtilsModule,
        AppCommonModule.forRoot(),
        ServiceProxyModule,
        TableModule,
        PaginatorModule,
        PrimeNgFileUploadModule,
        ProgressBarModule,
        PerfectScrollbarModule,
        IMaskModule,
        ImageCropperModule,
        AutoCompleteModule,
        NgxSpinnerModule,
        AppBsModalModule,
        SubheaderModule,
        ChangeProfilePictureModalModule
    ]
})
export class AppModule { }

function appInitializerFactory() {
    return () => {

        const url = new URL(location.href);
        const params = url.searchParams;

        if (params.has('t')) {
            params.delete('t');
            window.location.href = url.toString();
        }
    };
}
