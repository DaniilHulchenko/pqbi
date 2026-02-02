import { NgModule } from '@angular/core';
import { AdminSharedModule } from '@app/admin/shared/admin-shared.module';
import { AppSharedModule } from '@app/shared/app-shared.module';
import { SubscriptionManagementRoutingModule } from './subscription-management-routing.module';
import { SubscriptionManagementComponent } from './subscription-management.component';
import { ShowDetailModalComponent } from './show-detail-modal.component';
import { FormContainerComponent } from "../../shared/common/components/form-container/form-container.component";

@NgModule({
    declarations: [],
    imports: [AppSharedModule, AdminSharedModule, SubscriptionManagementRoutingModule, FormContainerComponent, SubscriptionManagementComponent, ShowDetailModalComponent],
})
export class SubscriptionManagementModule {}
