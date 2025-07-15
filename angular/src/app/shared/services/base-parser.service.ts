import { Injectable, OnInit } from '@angular/core';
import { BaseState } from '../models/base-state';
import { BaseUnits } from '../enums/base-units';
import { BaseDataInfo, PQSRestApiServiceProxy } from '@shared/service-proxies/service-proxies';

@Injectable({
    providedIn: 'root',
})
export class BaseParserService {
    bases: BaseDataInfo[] = [];

    constructor(private _pqsRestApiServiceroxy: PQSRestApiServiceProxy){
        this._pqsRestApiServiceroxy.measurementsBases().subscribe(bases => {
            this.bases = bases;
        })
    }

    tryParse(baseValue: string, baseModel: BaseState): boolean {
        const fractionPattern = /(\d+\/\d+)\s*([a-zA-Z]*)/;
        const singleFractionPattern = /([a-zA-Z]+)\/(\d+)/;
        const numberPattern = /\d+(?:\.\d+)?/;
        const halfPattern = /half|1\/2/i;
        const unitPattern = new RegExp(Object.values(BaseUnits).join('|'), 'i');

        const base = this.bases.find(base => base.phaseName === baseValue).description;

        baseModel.value = 1;

        const fractionMatch = base.match(fractionPattern);
        if (fractionMatch) {
            const [numerator, denominator] = fractionMatch[1].split('/').map(Number);
            baseModel.value = numerator / denominator;
            baseModel.unit = fractionMatch[2] ? (fractionMatch[2].toLowerCase() as BaseUnits) : BaseUnits.Cycle;
        } else {
            const singleFractionMatch = base.match(singleFractionPattern);
            if (singleFractionMatch) {
                const denominator = parseFloat(singleFractionMatch[2]);
                baseModel.value = 1 / denominator;
                baseModel.unit = singleFractionMatch[1].toLowerCase() as BaseUnits;
            } else if (halfPattern.test(base)) {
                baseModel.value = 0.5;
            } else {
                const numberMatch = base.match(numberPattern);
                if (numberMatch) {
                    baseModel.value = parseFloat(numberMatch[0]);
                }
            }
        }

        const unitMatch = base.match(unitPattern);
        if (unitMatch) {
            baseModel.unit = unitMatch[0].toLowerCase() as BaseUnits;
            return true;
        }

        return false;
    }
}
