import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DashboardToolbarService {
    private readonly dashboardActive$ = new BehaviorSubject<boolean>(false);

    setDashboardActive(isActive: boolean): void {
        this.dashboardActive$.next(isActive);
    }

    isDashboardActive(): Observable<boolean> {
        return this.dashboardActive$.asObservable();
    }
}
