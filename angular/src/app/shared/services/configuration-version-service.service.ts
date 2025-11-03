import { Injectable } from "@angular/core";
import { PQSRestApiServiceProxy } from "@shared/service-proxies/service-proxies";
import { BehaviorSubject, Observable, tap } from "rxjs";

@Injectable({
    providedIn: 'root',
})
export class ConfigurationVersionService {
    private version: number | null = null;
    private versionChanged$ = new BehaviorSubject<boolean>(false);

    constructor(private pqsRestApiServiceProxy: PQSRestApiServiceProxy) {}

    refreshVersion(): Observable<number> {
        return this.pqsRestApiServiceProxy.confVersion().pipe(
            tap((res) => {
                const changed = res !== this.version;
                if (changed) {
                    this.version = res;
                    this.versionChanged$.next(true);
                }
            })
        );
    }

    getVersionChanged$(): Observable<boolean> {
        return this.versionChanged$.asObservable();
    }
}