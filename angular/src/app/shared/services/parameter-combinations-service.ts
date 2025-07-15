import { Injectable } from '@angular/core';
import { Parameter } from '@app/main/customParameters/customParameters/table-parameters/models/parameter';
import {
    BaseParameterNameSlim,
    HarmonicsDto,
    PQSRestApiServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { concatMap, from, map } from 'rxjs';
import { BaseParameterType } from '../enums/base-parameter-type';

@Injectable({
    providedIn: 'root',
})
export class ParameterCombinationsService {
    private _combinations = new Set<string>();
    private _result = [];

    constructor(private _pqsRestApiServiceProxy: PQSRestApiServiceProxy) {}

    combineParameters(
        parameter: Parameter,
        selectedGroupName: string,
        selectedPhases: string[],
        selectedBases: string[],
        selectedQuantities: string[],
        selectedHarmonics: number[] | null = null,
        selectedResolutions: number[] | null = null,
        selectedOperators: string[] | null = null,
        selectedAggregationFunctions: string[] | null = null,
    ) {
        this._combinations = new Set<string>();
        this._result = [];

        this.generateCombinations(
            selectedPhases,
            selectedBases,
            selectedQuantities,
            selectedHarmonics?.length > 0 ? selectedHarmonics : null,
            selectedResolutions?.length > 0 ? selectedResolutions : null,
            selectedOperators?.length > 0 ? selectedOperators : null,
            selectedAggregationFunctions?.length > 0 ? selectedAggregationFunctions : null,
        );

        return from(this._result).pipe(
            concatMap((combination) => {
                const newParameter: Parameter = { ...parameter };
                newParameter.id = this.createId();
                newParameter.group = selectedGroupName;
                newParameter.phase = combination.phase;
                newParameter.baseResolution = combination.base;
                newParameter.quantity = combination.quantity;
                newParameter.resolution = combination.resolution;
                newParameter.operator = combination.operator;
                newParameter.aggregationFunction = combination.aggregationFunction;
                newParameter.resolution = null;

                // Set harmonic value only if it exists
                if (combination.harmonic !== null) {
                    newParameter.harmonics.value = combination.harmonic;
                } else {
                    newParameter.harmonics.value = null; // If harmonic is absent
                }

                return this._pqsRestApiServiceProxy
                    .baseParameterName(
                        new BaseParameterNameSlim({
                            aggregationFunction: newParameter.aggregationFunction,
                            operator: newParameter.operator,
                            quantity: newParameter.quantity,
                            resolution: newParameter.resolution,
                            group: newParameter.group,
                            harmonics: new HarmonicsDto({
                                range: newParameter.harmonics.range,
                                rangeOn: newParameter.harmonics.rangeOn,
                                value: newParameter.harmonics.value,
                                harmonicNums: newParameter.harmonics.value ? [newParameter.harmonics.value] : [],
                                index: 1,
                            }),
                            phase: newParameter.phase,
                            base: newParameter.baseResolution,
                            componentId: newParameter.fromComponents?.componentId,
                            feederId: newParameter.fromComponents?.feederId.toString(),
                            isLogical: newParameter.type === BaseParameterType.Logical,
                        }),
                    )
                    .pipe(
                        map((result) => {
                            newParameter.name = result;
                            return newParameter;
                        }),
                    );
            }),
        );
    }

    combineAdditionalParameters(parameter: Parameter, selectedBases: string[], selectedQuantities: string[]) {
        let combinations = [];

        for (let base of selectedBases) {
            for (let quantity of selectedQuantities) {
                const newParameter: Parameter = JSON.parse(JSON.stringify(parameter));
                newParameter.id = this.createId();
                newParameter.baseResolution = base;
                newParameter.quantity = quantity;
                
                combinations.push(newParameter);
            }
        }

        return combinations;
    }

    private generateCombinations(
        phases: string[],
        bases: string[],
        quantities: string[],
        harmonics: number[] | null = null,
        resolutions: number[] | null = null,
        operators: string[] | null = null,
        aggregationFunctions: string[] | null = null,
    ) {
        // Check that arrays are not null and not undefined
        if (!phases || !phases.length) {
            console.error('No phases selected');
            return;
        }
        if (!bases || !bases.length) {
            console.error('No bases selected');
            return;
        }
        if (!quantities || !quantities.length) {
            console.error('No quantities selected');
            return;
        }

        const phase = phases[0];
        const base = bases[0];
        const quantity = quantities[0];
        const harmonic = harmonics ? harmonics[0] : null;
        const resolution = resolutions ? resolutions[0] : null;
        const operator = operators ? operators[0] : null;
        const aggregationFunction = aggregationFunctions ? aggregationFunctions[0] : null;

        const key = `${phase}-${base}-${quantity}-${harmonic}-${resolution}-${operator}-${aggregationFunction}`;

        if (this._combinations.has(key)) {
            return;
        }

        this._combinations.add(key);

        const newItem = {
            phase: phase,
            base: base,
            quantity: quantity,
            harmonic: harmonic,
            resolution: resolution,
            operator: operator,
            aggregationFunction: aggregationFunction,
        };

        this._result.push(newItem);

        for (let i = 1; i < phases.length; i++) {
            this.generateCombinations(
                phases.slice(i),
                bases,
                quantities,
                harmonics,
                resolutions,
                operators,
                aggregationFunctions,
            );
        }

        for (let i = 1; i < bases.length; i++) {
            this.generateCombinations(
                phases,
                bases.slice(i),
                quantities,
                harmonics,
                resolutions,
                operators,
                aggregationFunctions,
            );
        }

        for (let i = 1; i < quantities.length; i++) {
            this.generateCombinations(
                phases,
                bases,
                quantities.slice(i),
                harmonics,
                resolutions,
                operators,
                aggregationFunctions,
            );
        }

        if (harmonics) {
            for (let i = 1; i < (harmonics ? harmonics.length : 0); i++) {
                this.generateCombinations(
                    phases,
                    bases,
                    quantities,
                    harmonics.slice(i),
                    resolutions,
                    operators,
                    aggregationFunctions,
                );
            }
        }

        if (resolutions) {
            for (let i = 1; i < (resolutions ? resolutions.length : 0); i++) {
                this.generateCombinations(
                    phases,
                    bases,
                    quantities,
                    harmonics,
                    resolutions.slice(i),
                    operators,
                    aggregationFunctions,
                );
            }
        }

        if (operators) {
            for (let i = 1; i < (operators ? operators.length : 0); i++) {
                this.generateCombinations(
                    phases,
                    bases,
                    quantities,
                    harmonics,
                    resolutions,
                    operators.slice(i),
                    aggregationFunctions,
                );
            }
        }

        if (aggregationFunctions) {
            for (let i = 1; i < (aggregationFunctions ? aggregationFunctions.length : 0); i++) {
                this.generateCombinations(
                    phases,
                    bases,
                    quantities,
                    harmonics,
                    resolutions,
                    operators,
                    aggregationFunctions.slice(i),
                );
            }
        }
    }

    private createId(): string {
        let id = '';
        let chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        for (let i = 0; i < 5; i++) {
            id += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return id;
    }
}
