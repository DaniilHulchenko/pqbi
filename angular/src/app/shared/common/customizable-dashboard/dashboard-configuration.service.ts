import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface DashboardConfigurationState {
    widgetNameFontSize?: number;
    backgroundColor?: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardConfigurationService {
    private readonly backgroundColorStorageKey = 'app.dashboard.backgroundColor';
    private readonly configuration$ = new BehaviorSubject<DashboardConfigurationState>({});

    setConfiguration(configuration: DashboardConfigurationState | undefined): void {
        const storedBackgroundColor = this.getStoredBackgroundColor();
        const nextConfiguration = configuration ?? {};

        if (!nextConfiguration.backgroundColor && storedBackgroundColor) {
            nextConfiguration.backgroundColor = storedBackgroundColor;
        }

        this.configuration$.next(nextConfiguration);
    }

    updateWidgetNameFontSize(widgetNameFontSize?: number): void {
        const configuration = this.configuration$.getValue();
        this.configuration$.next({
            ...configuration,
            widgetNameFontSize,
        });
    }

    updateBackgroundColor(backgroundColor?: string): void {
        const configuration = this.configuration$.getValue();
        const updatedConfiguration = {
            ...configuration,
            backgroundColor,
        };

        this.configuration$.next(updatedConfiguration);

        if (backgroundColor) {
            localStorage.setItem(this.backgroundColorStorageKey, backgroundColor);
        } else {
            localStorage.removeItem(this.backgroundColorStorageKey);
        }
    }

    getConfiguration(): Observable<DashboardConfigurationState> {
        return this.configuration$.asObservable();
    }

    getWidgetNameFontSize(): Observable<number | undefined> {
        return this.configuration$.pipe(map((configuration) => configuration.widgetNameFontSize));
    }

    getBackgroundColor(): Observable<string | undefined> {
        return this.configuration$.pipe(map((configuration) => configuration.backgroundColor));
    }

    getStoredBackgroundColor(): string | null {
        return localStorage.getItem(this.backgroundColorStorageKey);
    }
}
