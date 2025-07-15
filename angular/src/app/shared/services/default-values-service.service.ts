import { Injectable } from '@angular/core';
import { Observable, share } from 'rxjs';
import { CreateOrEditDefaultValueDto, DefaultValuesServiceProxy, GetDefaultValueForEditOutput } from '@shared/service-proxies/service-proxies';

@Injectable({
    providedIn: 'root',
})
export class DefaultValuesService {
    private _valuesCache = new Map<string, Observable<GetDefaultValueForEditOutput>>();

    constructor(private _defaultValuesServiceProxy: DefaultValuesServiceProxy) {}

    getValue(name: string): Observable<GetDefaultValueForEditOutput> {
        if (!this._valuesCache.has(name)) {
            const observable$ = this._defaultValuesServiceProxy.getDefaultValueByName(name).pipe(share());
            this._valuesCache.set(name, observable$);
        }
        return this._valuesCache.get(name)!;
    }

    createOrEdit(value: CreateOrEditDefaultValueDto) : Observable<void> {
        return this._defaultValuesServiceProxy.createOrEdit(value)
    }
}
