import { Injectable } from '@angular/core';
import { Observable, of, tap, shareReplay } from 'rxjs';
import { TableWidgetConfigurationsServiceProxy, CreateOrEditTableWidgetConfigurationDto, GetTableWidgetConfigurationForEditOutput } from '@shared/service-proxies/service-proxies';
import { CacheStorageService } from '../cache-storage.service';
import { ConfigurationVersionService } from '../configuration-version-service.service';

@Injectable({
    providedIn: 'root',
})
export class TableWidgetConfigurationService {
    private readonly CACHE_PREFIX = 'table_widget_config_';
    private _pendingRequests = new Map<number, Observable<GetTableWidgetConfigurationForEditOutput>>();

    constructor(
        private proxy: TableWidgetConfigurationsServiceProxy,
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

    getForEdit(id: number): Observable<GetTableWidgetConfigurationForEditOutput> {
        const cacheKey = this.CACHE_PREFIX + id;
        const cached = this.cacheStorage.get<GetTableWidgetConfigurationForEditOutput>(cacheKey);
        if (cached) {
            return of(cached);
        }

        if (this._pendingRequests.has(id)) {
            return this._pendingRequests.get(id)!;
        }

        const request$ = this.proxy.getTableWidgetConfigurationForEdit(id).pipe(
            tap(response => {
                this.cacheStorage.set(cacheKey, response);
                this._pendingRequests.delete(id);
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        this._pendingRequests.set(id, request$);
        return request$;
    }

    update(configuration: CreateOrEditTableWidgetConfigurationDto) {
        this.cacheStorage.set(
            this.CACHE_PREFIX + configuration.id,
            {
                tableWidgetConfiguration: configuration
            }
        );
    }
}

