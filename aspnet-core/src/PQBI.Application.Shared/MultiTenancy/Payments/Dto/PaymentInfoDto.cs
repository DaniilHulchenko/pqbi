﻿using PQBI.Editions.Dto;

namespace PQBI.MultiTenancy.Payments.Dto
{
    public class PaymentInfoDto
    {
        public EditionSelectDto Edition { get; set; }

        public decimal AdditionalPrice { get; set; }

        public bool IsLessThanMinimumUpgradePaymentAmount()
        {
            return AdditionalPrice < PQBIConsts.MinimumUpgradePaymentAmount;
        }
    }
}
