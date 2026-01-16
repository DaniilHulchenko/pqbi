import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardSettingsComponent } from './dashboard-settings.component';

const routes: Routes = [
    {
        path: '',
        component: DashboardSettingsComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class DashboardSettingsRoutingModule {}

