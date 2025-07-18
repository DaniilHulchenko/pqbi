﻿using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using PQBI.Authorization.Users.Profile;
using PQBI.Authorization.Users.Profile.Dto;
using PQBI.Core.Threading;
using PQBI.Localization;
using PQBI.ViewModels.Base;
using PQBI.Views;

namespace PQBI.ViewModels
{
    public class ChangePasswordViewModel : XamarinViewModel
    {
        public ICommand SendChangePasswordCommand => AsyncCommand.Create(SendChangePasswordAsync);

        private readonly IProfileAppService _profileAppService;
        private bool _isChangePasswordEnabled;


        public ChangePasswordViewModel(IProfileAppService profileAppService)
        {
            _profileAppService = profileAppService;
        }

        private string _currentPassword;
        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                _currentPassword = value;
                SetChangePasswordButtonEnabled();
                RaisePropertyChanged(() => CurrentPassword);
            }
        }

        private string _newPassword;
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                SetChangePasswordButtonEnabled();
                RaisePropertyChanged(() => NewPassword);
            }
        }

        private string _newPasswordRepeat;
        public string NewPasswordRepeat
        {
            get => _newPasswordRepeat;
            set
            {
                _newPasswordRepeat = value;
                SetChangePasswordButtonEnabled();
                RaisePropertyChanged(() => NewPasswordRepeat);
            }
        }

        public bool IsChangePasswordEnabled
        {
            get => _isChangePasswordEnabled;
            set
            {
                _isChangePasswordEnabled = value;
                RaisePropertyChanged(() => IsChangePasswordEnabled);
            }
        }

        public void SetChangePasswordButtonEnabled()
        {
            IsChangePasswordEnabled = !string.IsNullOrWhiteSpace(CurrentPassword)
                                      && !string.IsNullOrWhiteSpace(NewPassword)
                                      && !string.IsNullOrWhiteSpace(NewPasswordRepeat);
        }

        private async Task SendChangePasswordAsync()
        {
            if (NewPassword != NewPasswordRepeat)
            {
                await UserDialogs.Instance.AlertAsync(L.Localize("PasswordsDontMatch"));
            }
            else
            {
                await SetBusyAsync(async () =>
                {
                    await WebRequestExecuter.Execute(
                        async () =>
                            await _profileAppService.ChangePassword(new ChangePasswordInput
                            {
                                CurrentPassword = CurrentPassword,
                                NewPassword = NewPassword
                            }),
                        PasswordChangedAsync
                    );
                });
            }
        }

        private async Task PasswordChangedAsync()
        {
            await UserDialogs.Instance.AlertAsync(L.Localize("YourPasswordHasChangedSuccessfully"), L.Localize("ChangePassword"), L.Localize("Ok"));

            await NavigationService.SetMainPage<MainView>(clearNavigationHistory: true);
        }
    }
}
