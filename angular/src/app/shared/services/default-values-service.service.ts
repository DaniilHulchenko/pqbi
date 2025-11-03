import { Injectable } from '@angular/core';
import { map, Observable, of, shareReplay, tap, switchMap } from 'rxjs';
import { CreateOrEditDefaultValueDto, DefaultValuesServiceProxy, GetDefaultValueForEditOutput } from '@shared/service-proxies/service-proxies';
import { ConfigurationVersionService } from './configuration-version-service.service';
import { CacheStorageService } from './cache-storage.service';

@Injectable({
    providedIn: 'root',
})
export class DefaultValuesService {
    private readonly CACHE_PREFIX = 'default_value_';
    private _pendingRequests = new Map<string, Observable<string>>();

    constructor(
        private _defaultValuesServiceProxy: DefaultValuesServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    ) {
        this.configurationVersionService.getVersionChanged$().subscribe((versionChanged: boolean) => {
            if (versionChanged) {
                this.clearCache();
            }
        });
    }

    private clearCache(): void {
        this.cacheStorage.clearByPattern(this.CACHE_PREFIX);
        this._pendingRequests.clear();
    }

    getValue(name: string): Observable<string> {
        const cacheKey = this.CACHE_PREFIX + name;
        const cachedValue = this.cacheStorage.get<string>(cacheKey);
        if (cachedValue) {
            return of(cachedValue);
        }

        if (this._pendingRequests.has(name)) {
            return this._pendingRequests.get(name)!;
        }

        const request$ = this._defaultValuesServiceProxy
            .getDefaultValueByName(name)
            .pipe(
                map((result: GetDefaultValueForEditOutput) => result.defaultValue?.value),
                tap((value: string) => {
                    this.cacheStorage.set(cacheKey, value);
                    this._pendingRequests.delete(name);
                }),
                shareReplay({ bufferSize: 1, refCount: false })
            );

        this._pendingRequests.set(name, request$);
        return request$;
    }

    createOrEdit(value: CreateOrEditDefaultValueDto) : Observable<void> {
        return this._defaultValuesServiceProxy
            .createOrEdit(value)
            .pipe(
                tap(() => {
                    const cacheKey = this.CACHE_PREFIX + value.name;
                    this.cacheStorage.set(cacheKey, value.value);
                    this.configurationVersionService.refreshVersion().subscribe();
                })
            );
    }
}
