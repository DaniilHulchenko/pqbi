import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface DashboardConfigurationState {
    widgetNameFontSize?: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardConfigurationService {
    private readonly configuration$ = new BehaviorSubject<DashboardConfigurationState>({});

    setConfiguration(configuration: DashboardConfigurationState | undefined): void {
        this.configuration$.next(configuration ?? {});
    }

    updateWidgetNameFontSize(widgetNameFontSize?: number): void {
        const configuration = this.configuration$.getValue();
        this.configuration$.next({
            ...configuration,
            widgetNameFontSize,
        });
    }

    getConfiguration(): Observable<DashboardConfigurationState> {
        return this.configuration$.asObservable();
    }

    getWidgetNameFontSize(): Observable<number | undefined> {
        return this.configuration$.pipe(map((configuration) => configuration.widgetNameFontSize));
    }
}