import { Injectable } from '@angular/core';
import { Observable, of, tap, shareReplay } from 'rxjs';
import { CardWidgetConfigurationsServiceProxy, CreateOrEditCardWidgetConfigurationDto, GetCardWidgetConfigurationForEditOutput } from '@shared/service-proxies/service-proxies';
import { CacheStorageService } from '../cache-storage.service';
import { ConfigurationVersionService } from '../configuration-version-service.service';

@Injectable({
    providedIn: 'root',
})
export class CardWidgetConfigurationService {
    private readonly CACHE_PREFIX = 'card_widget_config_';
    private _pendingRequests = new Map<number, Observable<GetCardWidgetConfigurationForEditOutput>>();

    constructor(
        private proxy: CardWidgetConfigurationsServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    ) {
        this.configurationVersionService.getVersionChanged$().subscribe(() => {
            this.clearCache();
        });
    }

    private clearCache(): void {
        this.cacheStorage.clearByPattern(this.CACHE_PREFIX);
        this._pendingRequests.clear();
    }

    getForEdit(id: number): Observable<GetCardWidgetConfigurationForEditOutput> {
        const cacheKey = this.CACHE_PREFIX + id;
        const cached = this.cacheStorage.get<GetCardWidgetConfigurationForEditOutput>(cacheKey);
        if (cached) {
            return of(cached);
        }

        if (this._pendingRequests.has(id)) {
            return this._pendingRequests.get(id)!;
        }

        const request$ = this.proxy.getCardWidgetConfigurationForEdit(id).pipe(
            tap(response => {
                this.cacheStorage.set(cacheKey, response);
                this._pendingRequests.delete(id);
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        this._pendingRequests.set(id, request$);
        return request$;
    }

    update(configuration: CreateOrEditCardWidgetConfigurationDto) {
        this.cacheStorage.set(
            this.CACHE_PREFIX + configuration.id,
            {
                cardWidgetConfiguration: configuration
            }
        );
    }
}

