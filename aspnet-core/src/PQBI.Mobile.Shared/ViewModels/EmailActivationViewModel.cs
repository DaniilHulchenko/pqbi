﻿using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using PQBI.Authorization.Accounts;
using PQBI.Authorization.Accounts.Dto;
using PQBI.Commands;
using PQBI.Core.Threading;
using PQBI.Localization;
using PQBI.ViewModels.Base;
using PQBI.Views;

namespace PQBI.ViewModels
{
    public class EmailActivationViewModel : XamarinViewModel
    {
        public ICommand SendEmailActivationCommand => HttpRequestCommand.Create(SendEmailActivationAsync);

        private readonly IAccountAppService _accountAppService;
        private bool _isEmailActivationEnabled;

        public EmailActivationViewModel(IAccountAppService accountAppService)
        {
            _accountAppService = accountAppService;
        }

        private string _emailAddress;
        public string EmailAddress
        {
            get => _emailAddress;
            set
            {
                _emailAddress = value;
                SetEmailActivationButtonEnabled();
                RaisePropertyChanged(() => EmailAddress);
            }
        }

        public bool IsEmailActivationEnabled
        {
            get => _isEmailActivationEnabled;
            set
            {
                _isEmailActivationEnabled = value;
                RaisePropertyChanged(() => IsEmailActivationEnabled);
            }
        }

        public void SetEmailActivationButtonEnabled()
        {
            IsEmailActivationEnabled = !string.IsNullOrWhiteSpace(EmailAddress);
        }

        private async Task SendEmailActivationAsync()
        {
            await SetBusyAsync(async () =>
            {
                await WebRequestExecuter.Execute(
                    async () =>
                    await _accountAppService.SendEmailActivationLink(new SendEmailActivationLinkInput { EmailAddress = EmailAddress }),
                    PasswordResetMailSentAsync
                );
            });
        }

        private async Task PasswordResetMailSentAsync()
        {
            await UserDialogs.Instance.AlertAsync(L.Localize("ActivationMailSentMessage"), L.Localize("MailSent"), L.Localize("Ok"));

            await NavigationService.SetMainPage<LoginView>(clearNavigationHistory: true);
        }
    }
}
