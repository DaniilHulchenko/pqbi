import { Injectable } from "@angular/core";
import { ComponentSlimDto, GetComponentByTagsRequest, GetComponentSlimInfosRequest, TagWithComponents, TreeBuilderServiceProxy } from "@shared/service-proxies/service-proxies";
import { CacheStorageService } from "./cache-storage.service";
import { ConfigurationVersionService } from "./configuration-version-service.service";
import { Observable, of, tap, map, shareReplay } from "rxjs";

@Injectable({
    providedIn: 'root',
})
export class ComponentsService {
    private readonly CACHE_TAGS_PREFIX = 'tags_';
    private readonly CACHE_COMPONENTS_PREFIX = 'components_';
    private _pendingRequests = new Map<string, Observable<TagWithComponents[]>>();
    private _pendingRequestsComponentsSlims = new Map<string, Observable<ComponentSlimDto[]>>();
    
    constructor(private _treeBuilderServiceProxy: TreeBuilderServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    )
    {
        this.configurationVersionService.getVersionChanged$().subscribe(() => {
            this.clearCache();
        });
    }

    private clearCache(): void {
        this.cacheStorage.clearByPattern(this.CACHE_TAGS_PREFIX);
        this.cacheStorage.clearByPattern(this.CACHE_COMPONENTS_PREFIX);
        this._pendingRequests.clear();
    }

    componentByTags(request: GetComponentByTagsRequest): Observable<TagWithComponents[]> {
        const requestKey = request.tags.join(',');

        const cacheKey = this.CACHE_TAGS_PREFIX + requestKey;
        const cachedComponents = this.cacheStorage.get<TagWithComponents[]>(cacheKey);
        if (cachedComponents) {
            return of(cachedComponents);
        }

        if (this._pendingRequests.has(requestKey)) {
            return this._pendingRequests.get(requestKey)!;
        }

        const request$ = this._treeBuilderServiceProxy.componentByTags(request).pipe(
            map(response => response.components),
            tap(response => {
                this.cacheStorage.set(cacheKey, response);
                this._pendingRequests.delete(requestKey);
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        this._pendingRequests.set(requestKey, request$);
        return request$;
    }

    componentSlimsInfo(request: GetComponentSlimInfosRequest): Observable<ComponentSlimDto[]> {
        const requestKey = request.componentIds.join(',');
        const cacheKey = this.CACHE_COMPONENTS_PREFIX + requestKey;

        const cachedComponents = this.cacheStorage.get<ComponentSlimDto[]>(cacheKey);
        if (cachedComponents) {
            return of(cachedComponents);
        }

        if (this._pendingRequestsComponentsSlims.has(requestKey)) {
            return this._pendingRequestsComponentsSlims.get(requestKey)!;
        }

        const request$ = this._treeBuilderServiceProxy.componentSlimsInfo(request).pipe(
            map(response => response.components),
            tap(response => {
                this.cacheStorage.set(cacheKey, response);
                this._pendingRequestsComponentsSlims.delete(requestKey);
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        this._pendingRequestsComponentsSlims.set(requestKey, request$);
        return request$;
    }
}