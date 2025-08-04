import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                children: [
                    
                    {
                        path: 'groups/groups',
                        loadChildren: () => import('./groups/groups/group.module').then(m => m.GroupModule),
                        data: { permission: 'Pages.Groups' }
                    },
                
                    {
                        path: 'customParameters',
                        loadChildren: () =>
                            import('./customParameters/customParameters/customParameter.module').then((m) => m.CustomParameterModule),
                        data: { permission: 'Pages.CustomParameters' },
                    },
                    {
                        path: 'dashboard',
                        loadChildren: () => import('./dashboard/dashboard.module').then((m) => m.DashboardModule),
                        data: { permission: 'Pages.Tenant.Dashboard' },
                    },
                    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
                    { path: '**', redirectTo: 'dashboard' },
                ],
            },
        ]),
    ],
    exports: [RouterModule],
})
export class MainRoutingModule {}
