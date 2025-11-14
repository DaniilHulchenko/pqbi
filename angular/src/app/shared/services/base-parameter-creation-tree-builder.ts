import { Injectable } from '@angular/core';
import {
    AdditionalData,
    BaseDataInfo,
    CalculationBase,
    CustomCalculationBaseInfo,
    Group,
    GroupDataInfo,
    PhaseDataInfo,
    PhaseMeasurementEnum,
    PQSRestApiServiceProxy,
    QuantityDataInfo,
    QuantityEnum,
} from '@shared/service-proxies/service-proxies';
import { BaseParameterType } from '../enums/base-parameter-type';
import { AdditionalParameterTreeModel } from '../models/additional-parameter';
import { MeasurementsService } from './measurements-service.service';

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

    constructor(private _measurementsService: MeasurementsService) {
        _measurementsService.groupMeasurements.subscribe((result: GroupDataInfo[]) => {
            this._groupMeasurements = result;
        });

        _measurementsService.phaseMeasurements.subscribe((result: PhaseDataInfo[]) => {
            this._phaseMeasurements = result;
        });

        _measurementsService.baseMeasurements.subscribe((result: BaseDataInfo[]) => {
            this._baseMeasurements = result;
        });

        _measurementsService.quantityMeasurements.subscribe((result: QuantityDataInfo[]) => {
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

            let base = parameterInfo.propertyName.substring(parameterInfo.propertyName.lastIndexOf('_') + 1);
            let baseInGroup = groupInTree.bases.find((baseInfo) => baseInfo.base === CalculationBase[base]);
            if (!baseInGroup) {
                baseInGroup = { ...this.getParameterBase(base, new Map()) };
                groupInTree.bases.push(baseInGroup);
            }
        });

        return tree;
    }

    buildTree(
        baseParametrerType: BaseParameterType,
        componentId: string,
        componentParameterInfos: string[],
        customBaseList?: CustomCalculationBaseInfo[],
    ): object {
        let tree = {};

        tree[componentId] = {
            groups: [],
            feeders: [],
        };

        const parameterInfos = this.filterParameterInfosByType(baseParametrerType, componentParameterInfos);
        const customBaseDescriptions = this.createCustomBaseDescriptionMap(customBaseList ?? []);

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
            const normalizedBaseKey = base?.toUpperCase();
            const enumKey = normalizedBaseKey?.includes('_') ? normalizedBaseKey.split('_')[0] : normalizedBaseKey;
            const enumValue = enumKey ? CalculationBase[enumKey as keyof typeof CalculationBase] : undefined;

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

            let baseInPhase =
                phaseInGroup.bases.find((baseInfo) => baseInfo.phaseName === base) ??
                phaseInGroup.bases.find((baseInfo) => enumValue !== undefined && baseInfo.base === enumValue);
            if (!baseInPhase) {
                const parameterBase = this.getParameterBase(base, customBaseDescriptions);
                if (!parameterBase) {
                    return;
                }
                baseInPhase = { ...parameterBase };
                baseInPhase.base = enumValue ?? parameterBase.base;
                baseInPhase.phaseName = parameterBase.phaseName ?? base;
                baseInPhase.quantities = [];
                phaseInGroup.bases.push(baseInPhase);
            }

            const customDescription =
                customBaseDescriptions.get(normalizedBaseKey) ??
                customBaseDescriptions.get(enumKey ?? '');
            if (customDescription) {
                baseInPhase.description = customDescription;
            }
            baseInPhase.phaseName = base;

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

    private getParameterBase(
        baseInfo: string,
        customBaseDescriptions: Map<string, string>,
    ): BaseDataInfo {
        const normalizedBaseInfo = baseInfo?.toUpperCase();
        const enumKey = normalizedBaseInfo?.includes('_') ? normalizedBaseInfo.split('_')[0] : normalizedBaseInfo;
        const enumValue = enumKey ? CalculationBase[enumKey as keyof typeof CalculationBase] : undefined;

        const measurement = this._baseMeasurements?.find((base) => base.base === enumValue);
        if (measurement) {
            const descriptionOverride =
                customBaseDescriptions.get(normalizedBaseInfo) ?? customBaseDescriptions.get(enumKey ?? '');
            return new BaseDataInfo({
                base: measurement.base,
                phaseName: baseInfo,
                description: descriptionOverride ?? measurement.description,
            });
        }

        const description =
            customBaseDescriptions.get(normalizedBaseInfo) ?? customBaseDescriptions.get(enumKey ?? '');
        if (description && enumValue !== undefined) {
            return new BaseDataInfo({
                base: enumValue,
                phaseName: baseInfo,
                description,
            });
        }

        return null;
    }

    private getParameterQuantity(quantityInfo: string): QuantityDataInfo {
        let quantity = QuantityEnum[quantityInfo];
        if (quantity !== undefined && quantity !== null) {
            return this._quantityMeasurements?.find((quantity) => quantity.quantity === QuantityEnum[quantityInfo]);
        }
        return null;
    }

    private createCustomBaseDescriptionMap(
        customBaseList: CustomCalculationBaseInfo[],
    ): Map<string, string> {
        const map = new Map<string, string>();

        customBaseList?.forEach((customBase) => {
            const presentedName = (customBase as any)?.presentedName ?? (customBase as any)?.PresentedName;
            if (!presentedName) {
                return;
            }

            const key = this.extractCustomBaseKey(customBase);
            if (!key) {
                return;
            }

            const normalizedKey = key.toUpperCase();
            if (!map.has(normalizedKey)) {
                map.set(normalizedKey, presentedName);
            }

            const baseOnly = normalizedKey.split('_')[0];
            if (baseOnly && !map.has(baseOnly)) {
                map.set(baseOnly, presentedName);
            }
        });

        return map;
    }

    private extractCustomBaseKey(customBase: CustomCalculationBaseInfo): string | null {
        if (!customBase) {
            return null;
        }

        const customBaseAny = customBase as any;
        const rawCalcBase =
            customBaseAny?.calcBase ??
            customBaseAny?.CalcBase ??
            customBaseAny?.base ??
            customBaseAny?.Base;

        let basePart: string | null = null;

        if (typeof rawCalcBase === 'string') {
            basePart = rawCalcBase;
        } else if (typeof rawCalcBase === 'number') {
            basePart = CalculationBase[rawCalcBase] ?? rawCalcBase.toString();
        } else if (rawCalcBase) {
            const enumValue =
                rawCalcBase?.calculationBaseEnum ??
                rawCalcBase?.CalculationBaseEnum ??
                rawCalcBase?.enum ??
                rawCalcBase?.Enum;

            if (typeof enumValue === 'number') {
                basePart = CalculationBase[enumValue] ?? enumValue.toString();
            } else if (typeof enumValue === 'string') {
                basePart = enumValue;
            } else if (typeof rawCalcBase?.toString === 'function') {
                const parsed = rawCalcBase.toString();
                if (parsed && parsed !== '[object Object]') {
                    basePart = parsed;
                }
            }
        }

        const rawWindowInterval =
            customBaseAny?.windowInterval ??
            customBaseAny?.WindowInterval;

        let windowPart: string | null = null;
        if (rawWindowInterval !== undefined && rawWindowInterval !== null) {
            if (typeof rawWindowInterval === 'string' || typeof rawWindowInterval === 'number') {
                windowPart = rawWindowInterval.toString();
            } else {
                const candidate =
                    rawWindowInterval?.value ??
                    rawWindowInterval?.Value ??
                    rawWindowInterval?.windowInterval ??
                    rawWindowInterval?.WindowInterval;
                if (candidate !== undefined && candidate !== null) {
                    windowPart = candidate.toString();
                } else if (typeof rawWindowInterval?.toString === 'function') {
                    const parsed = rawWindowInterval.toString();
                    if (parsed && parsed !== '[object Object]') {
                        windowPart = parsed;
                    }
                }
            }
        }

        if (basePart) {
            if (windowPart) {
                return `${basePart}_${windowPart}`;
            }
            return basePart;
        }

        if (windowPart) {
            return windowPart;
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
