// src/app/shared/guards/pending-changes.guard.ts
import { Injectable } from '@angular/core';
import { CanDeactivate } from '@angular/router';
import { DashboardComponent } from '@app/main/dashboard/dashboard.component';
import { NgxSpinner, NgxSpinnerService } from '@node_modules/ngx-spinner';

@Injectable({ providedIn: 'root' })
export class PendingChangesGuard implements CanDeactivate<DashboardComponent>{
    constructor(private spinner: NgxSpinnerService){}

    canDeactivate(component: DashboardComponent): boolean {
        const okToLeave = component.canDeactivate();

        if (!okToLeave) {
        this.spinner.hide();
        }
        return okToLeave;
    }
}
