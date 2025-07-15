import { Injectable } from '@angular/core';
import {
    AdditionalData,
    BaseDataInfo,
    CalculationBase,
    GetComponentSlimInfosRequest,
    Group,
    GroupDataInfo,
    PhaseDataInfo,
    PhaseMeasurementEnum,
    PQSRestApiServiceProxy,
    QuantityDataInfo,
    QuantityEnum,
    TreeBuilderServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { BaseParameterType } from '../enums/base-parameter-type';
import { map, Observable, ReplaySubject } from 'rxjs'
import { AdditionalParameterTreeModel } from '../models/additional-parameter';

@Injectable({
    providedIn: 'root',
})
export class BaseParameterCreationTreeBuilder {
    private _groupMeasurements: GroupDataInfo[];
    private _phaseMeasurements: PhaseDataInfo[];
    private _baseMeasurements: BaseDataInfo[];
    private _quantityMeasurements: QuantityDataInfo[];

    private readonly _logicalPattern = /FEEDER_\d+/;
    private readonly _channelPattern = /CH_\d+/;

    private readonly _harmonicParameterStartsWith = 'MULTI_STD';

    private readonly _parameterInfoSeparator = '_';
    private readonly _parameterQuantitiesSeparator = '#';

    constructor(private _pqsRestApiServiceProxy: PQSRestApiServiceProxy) {
        this._pqsRestApiServiceProxy.measurementsGroups().subscribe((result: GroupDataInfo[]) => {
            this._groupMeasurements = result;
        });

        this._pqsRestApiServiceProxy.measurementsPhases().subscribe((result: PhaseDataInfo[]) => {
            this._phaseMeasurements = result;
        });

        this._pqsRestApiServiceProxy.measurementsBases().subscribe((result: BaseDataInfo[]) => {
            this._baseMeasurements = result;
        });

        this._pqsRestApiServiceProxy.measurementsQunatities().subscribe((result: QuantityDataInfo[]) => {
            this._quantityMeasurements = result;
        });
    }

    public buildAdditionalTree(componentParameterInfos: AdditionalData[]): any {
        let tree = {
            groups: [] as AdditionalParameterTreeModel[],
        };

        componentParameterInfos.forEach((parameterInfo) => {
            let groupInTree : AdditionalParameterTreeModel = null;

            groupInTree = tree.groups.find(group => group.groupName === parameterInfo.propertyName);
            
            if (!groupInTree) {
                groupInTree = new AdditionalParameterTreeModel();
                groupInTree.groupName = parameterInfo.propertyName;
                groupInTree.description = parameterInfo.measurmentsParameterDetails.description ||
                                 parameterInfo.measurmentsParameterDetails.name ||
                                 parameterInfo.propertyName.substring(0, parameterInfo.propertyName.lastIndexOf('_'))
                groupInTree.bases = [];
                tree.groups.push(groupInTree);
            }

            let base = parameterInfo.base;
            let baseInGroup = groupInTree.bases.find((baseInfo) => baseInfo.base === CalculationBase[base]);
            if (!baseInGroup) {
                baseInGroup = { ...this.getParameterBase(base) };
                groupInTree.bases.push(baseInGroup);
            }
        });

        return tree;
    }

    buildTree(baseParametrerType: BaseParameterType, componentId: string, componentParameterInfos: string[]): object {
        let tree = {};

        tree[componentId] = {
            groups: [],
            feeders: [],
        };

        const parameterInfos = this.filterParameterInfosByType(baseParametrerType, componentParameterInfos);

        parameterInfos.forEach((parameterInfo) => {
            const isHarmonic = parameterInfo.startsWith(this._harmonicParameterStartsWith);
            let counter = isHarmonic ? 2 : 1; // After separating, groupName will be either at second or in third position in array, depending if it is harmonic or not

            const parameterInfoArr = parameterInfo.split(this._parameterInfoSeparator);

            let quantitiesString = parameterInfoArr[parameterInfoArr.length - 1];
            let quantities = quantitiesString.split(this._parameterQuantitiesSeparator); // PAY ATTENTION: quantities contain number of feeder or channel at position 0

            const feederId = quantities[0];

            if (baseParametrerType === BaseParameterType.Logical) {
                if (!tree[componentId].feeders.some((f) => f.feederId === feederId)) {
                    tree[componentId].feeders.push({ feederId: feederId, groups: [] });
                }
            }

            const groupName = parameterInfoArr[counter];
            counter++;

            let groupInTree = null;

            if (baseParametrerType === BaseParameterType.Logical) {
                groupInTree = tree[componentId].feeders
                    .find((f) => f.feederId === feederId)
                    .groups.find((group) => group.groupId === Group[groupName]);
            } else {
                groupInTree = tree[componentId].groups.find((group) => group.groupId === Group[groupName]);
            }

            if (!groupInTree) {
                groupInTree = { ...this.getParameterGroup(groupName) };
                groupInTree.phases = [];
                if (baseParametrerType === BaseParameterType.Logical) {
                    tree[componentId].feeders.find((f) => f.feederId === feederId).groups.push(groupInTree);
                } else {
                    tree[componentId].groups.push(groupInTree);
                }
            }

            if (isHarmonic) {
                if (!groupInTree.harmonics) {
                    let [from, to] = parameterInfoArr[counter].split(':'); // Divide 'From' and 'To' harmonic
                    groupInTree.harmonics = Array.from({ length: +to - +from + 1 }, (_, i) => +from + i);
                }
                counter++;
            }

            let base = parameterInfoArr[counter];
            counter++;

            let phase: string;
            switch (baseParametrerType) {
                case BaseParameterType.Logical:
                    phase = parameterInfoArr[counter];
                    break;
                case BaseParameterType.Channel:
                    phase = `${parameterInfoArr[counter]}_${quantities[0]}`;
                    break;
            }
            counter++;

            let phaseInGroup = groupInTree.phases.find((phaseInfo) => phaseInfo.phaseName === phase);
            if (!phaseInGroup) {
                switch (baseParametrerType) {
                    case BaseParameterType.Logical:
                        phaseInGroup = { ...this.getParameterPhaseForLogical(phase) };
                        break;
                    case BaseParameterType.Channel:
                        phaseInGroup = { ...this.getParameterPhaseForChannel(phase) };
                        break;
                }
                phaseInGroup.bases = [];
                groupInTree.phases.push(phaseInGroup);
            }

            let baseInPhase = phaseInGroup.bases.find((baseInfo) => baseInfo.base === CalculationBase[base]);
            if (!baseInPhase) {
                baseInPhase = { ...this.getParameterBase(base) };
                baseInPhase.quantities = [];
                phaseInGroup.bases.push(baseInPhase);
            }

            quantities = quantities.slice(1);
            quantities.forEach((quantity) => {
                if (!baseInPhase.quantities.some((bq) => bq.phaseName === quantity)) {
                    let created = this.getParameterQuantity(quantity);
                    if (created) {
                        baseInPhase.quantities.push({ ...created });
                    }
                }
            });
        });

        return tree;
    }

    private getParameterGroup(groupInfo: string): GroupDataInfo {
        return this._groupMeasurements?.find((group) => group.groupId === Group[groupInfo]);
    }

    private getParameterPhaseForLogical(phaseInfo: string): PhaseDataInfo {
        return this._phaseMeasurements?.find((phase) => phase.phase === PhaseMeasurementEnum[phaseInfo]);
    }

    private getParameterBase(baseInfo: string): BaseDataInfo {
        return this._baseMeasurements?.find((base) => base.base === CalculationBase[baseInfo]);
    }

    private getParameterQuantity(quantityInfo: string): QuantityDataInfo {
        let quantity = QuantityEnum[quantityInfo];
        if (quantity !== undefined && quantity !== null) {
            return this._quantityMeasurements?.find((quantity) => quantity.quantity === QuantityEnum[quantityInfo]);
        }
        return null;
    }

    private getParameterPhaseForChannel(phaseInfo: string): PhaseDataInfo {
        let phaseId = phaseInfo.split(this._parameterInfoSeparator)[1];
        return new PhaseDataInfo({ phaseName: phaseInfo, description: `Channel ${phaseId}`, phase: null });
    }

    private filterParameterInfosByType(baseParametrerType: BaseParameterType, parameterInfos: string[]): string[] {
        switch (baseParametrerType) {
            case BaseParameterType.Logical:
                return parameterInfos.filter((parameterInfo) => this._logicalPattern.test(parameterInfo));
            case BaseParameterType.Channel:
                return parameterInfos.filter((parameterInfo) => this._channelPattern.test(parameterInfo));
            case BaseParameterType.Additional:
                return parameterInfos;
            default:
                return [];
        }
    }
}
