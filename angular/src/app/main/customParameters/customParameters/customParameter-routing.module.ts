﻿import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomParametersComponent } from './customParameters.component';

const routes: Routes = [
    {
        path: '',
        component: CustomParametersComponent,
        pathMatch: 'full',
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class CustomParameterRoutingModule {}
