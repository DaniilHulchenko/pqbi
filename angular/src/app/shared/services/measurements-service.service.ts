import { Injectable } from "@angular/core";
import { Observable, of, shareReplay, tap } from "rxjs";
import { GroupDataInfo, PhaseDataInfo, BaseDataInfo, QuantityDataInfo, PQSRestApiServiceProxy } from "@shared/service-proxies/service-proxies";

@Injectable({
    providedIn: 'root',
})
export class MeasurementsService {
    private _groupMeasurements: GroupDataInfo[];
    private _phaseMeasurements: PhaseDataInfo[];
    private _baseMeasurements: BaseDataInfo[];
    private _quantityMeasurements: QuantityDataInfo[];

    constructor(private _pqsRestApiServiceProxy: PQSRestApiServiceProxy) {
    }

    get groupMeasurements(): Observable<GroupDataInfo[]> {
        return this._groupMeasurements
            ? of(this._groupMeasurements)
            : this._pqsRestApiServiceProxy.measurementsGroups().pipe(
                tap((result: GroupDataInfo[]) => {
                    this._groupMeasurements = result;
                }),
                shareReplay(1)
            );
    }

    get phaseMeasurements(): Observable<PhaseDataInfo[]> {
        return this._phaseMeasurements
            ? of(this._phaseMeasurements)
            : this._pqsRestApiServiceProxy.measurementsPhases().pipe(
                tap((result: PhaseDataInfo[]) => {
                    this._phaseMeasurements = result;
                }),
                shareReplay(1)
            );
    }

    get baseMeasurements(): Observable<BaseDataInfo[]> {
        return this._baseMeasurements
            ? of(this._baseMeasurements)
            : this._pqsRestApiServiceProxy.measurementsBases().pipe(
                tap((result: BaseDataInfo[]) => {
                    this._baseMeasurements = result;
                }),
                shareReplay(1)
            );
    }

    get quantityMeasurements(): Observable<QuantityDataInfo[]> {
        return this._quantityMeasurements
            ? of(this._quantityMeasurements)
            : this._pqsRestApiServiceProxy.measurementsQunatities().pipe(
                tap((result: QuantityDataInfo[]) => {
                    this._quantityMeasurements = result;
                }),
                shareReplay(1)
            );
    }
}