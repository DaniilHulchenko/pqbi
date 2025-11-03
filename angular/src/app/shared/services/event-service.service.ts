import { Injectable } from "@angular/core";
import { EventClassDescription, PQSRestApiServiceProxy } from "@shared/service-proxies/service-proxies";
import { Observable, of, shareReplay, tap } from "rxjs";
import { ConfigurationVersionService } from "./configuration-version-service.service";
import { CacheStorageService } from "./cache-storage.service";

@Injectable({
    providedIn: 'root',
})
export class EventService {
    private readonly CACHE_KEY = 'pqs_events';
    private _pendingRequest: Observable<EventClassDescription[]> | null = null;

    constructor(
        private pqsRestApi: PQSRestApiServiceProxy,
        private configurationVersionService: ConfigurationVersionService,
        private cacheStorage: CacheStorageService
    ){
        this.configurationVersionService.getVersionChanged$().subscribe(() => {
            this.clearCache();
        });
    }

    private clearCache(): void {
        this.cacheStorage.remove(this.CACHE_KEY);
        this._pendingRequest = null;
    }

    pqsEvents(): Observable<EventClassDescription[]> {
        const cachedEvents = this.cacheStorage.get<EventClassDescription[]>(this.CACHE_KEY);
        if (cachedEvents && cachedEvents.length > 0) {
            return of(cachedEvents);
        }

        if (this._pendingRequest) {
            return this._pendingRequest;
        }

        this._pendingRequest = this.pqsRestApi.pQSEvents().pipe(
            tap((events: EventClassDescription[]) => {
                this.cacheStorage.set(this.CACHE_KEY, events);
                this._pendingRequest = null;
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        return this._pendingRequest;
    }
}
