﻿using System.ComponentModel.DataAnnotations;

namespace PQBI.Authorization.Accounts.Dto
{
    public class SendEmailActivationLinkInput
    {
        [Required]
        public string EmailAddress { get; set; }
    }
}