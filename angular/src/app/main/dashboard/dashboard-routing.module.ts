import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { PendingChangesGuard } from '@app/shared/common/customizable-dashboard/pending-changes.guard';

const routes: Routes = [
    {
        path: '',
        component: DashboardComponent,
        pathMatch: 'full',
        canDeactivate: [PendingChangesGuard]
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class DashboardRoutingModule {}
