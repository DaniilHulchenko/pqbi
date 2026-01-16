import { Component, EventEmitter, Injector, OnInit, Output } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { PQSRestApiServiceProxy } from '@shared/service-proxies/service-proxies';
import { BaseParameterType } from '@app/shared/enums/base-parameter-type';
import { QuantityUnits } from '@app/shared/enums/quantity-units';
import { orderBy } from 'lodash-es';

export interface AdvancedParameter {
    id?: string;
    type: 'logical' | 'channel';
    group: string;
    phaseOrChannel: string;
    quantity: string;
    color: string;
}

@Component({
    selector: 'app-advanced-parameter-selector',
    templateUrl: './advanced-parameter-selector.component.html',
    styleUrls: ['./advanced-parameter-selector.component.less'],
})
export class AdvancedParameterSelectorComponent extends AppComponentBase implements OnInit {
    @Output() parameterAdded = new EventEmitter<AdvancedParameter>();

    selectedTab: 'logical' | 'channel' = 'logical';

    // Form fields
    selectedGroup: any = null;
    selectedPhaseOrChannel: any = null;
    selectedQuantity: any = null;
    selectedColor: string = '#800000';

    // Options
    groupOptions: any[] = [];
    phaseOrChannelOptions: any[] = [];
    quantityOptions: any[] = [];

    private parameterOptions: any;
    private selectedGroupModel: any;

    constructor(
        injector: Injector,
        private _pqsRestApiServiceProxy: PQSRestApiServiceProxy
    ) {
        super(injector);
    }

    ngOnInit(): void {
        super.ngOnInit();
        this.loadGroupOptions();
        this.updateQuantityOptions();
    }

    onTabChange(tab: 'logical' | 'channel'): void {
        this.selectedTab = tab;
        this.resetForm();
        this.loadGroupOptions();
    }

    onGroupChange(event: any): void {
        const groupValue = event?.value !== undefined ? event.value : this.selectedGroup;
        if (groupValue) {
            this.selectedGroupModel = this.groupOptions.find((option) => option.groupName === groupValue);
            this.updatePhaseOrChannelOptions();
        } else {
            this.selectedGroupModel = null;
            this.phaseOrChannelOptions = [];
            this.updateQuantityOptions();
        }
        this.selectedPhaseOrChannel = null;
        this.selectedQuantity = null;
    }

    onPhaseOrChannelChange(): void {
        this.updateQuantityOptions();
        this.selectedQuantity = null;
    }

    private loadGroupOptions(): void {
        this._pqsRestApiServiceProxy.getStaticData().subscribe((response) => {
            const baseParameterType = this.selectedTab === 'logical' ? BaseParameterType.Logical : BaseParameterType.Channel;
            this.parameterOptions = response.staticTreeNode.children.find(
                (child: any) => child.value.toLowerCase() === baseParameterType.toLowerCase(),
            );
            this.groupOptions = this.parameterOptions?.children || [];
            this.groupOptions.forEach((item: any) => {
                if (!item.groupName) {
                    item.groupName = item.value;
                }
            });
        });
    }

    private updatePhaseOrChannelOptions(): void {
        if (!this.selectedGroupModel?.children) {
            this.phaseOrChannelOptions = [];
            return;
        }

        this.phaseOrChannelOptions = orderBy(
            this.selectedGroupModel.children.map((phase: any) => {
                return {
                    description: phase.description,
                    phaseName: phase.value,
                    children: phase.children || [],
                };
            }),
            'description',
            'asc',
        );
    }

    private updateQuantityOptions(): void {
        // Always include MIN, MAX, AVG from QuantityUnits enum
        const defaultQuantities = [
            {
                phaseName: QuantityUnits.MIN,
                description: QuantityUnits.MIN,
            },
            {
                phaseName: QuantityUnits.MAX,
                description: QuantityUnits.MAX,
            },
            {
                phaseName: QuantityUnits.AVG,
                description: QuantityUnits.AVG,
            },
        ];

        if (!this.selectedPhaseOrChannel) {
            this.quantityOptions = defaultQuantities;
            return;
        }

        const phaseObj = this.phaseOrChannelOptions.find((p: any) => p.phaseName === this.selectedPhaseOrChannel);
        if (!phaseObj?.children) {
            this.quantityOptions = defaultQuantities;
            return;
        }

        const uniqueQuantities = new Map<string, any>();

        // Add default quantities first
        defaultQuantities.forEach((q) => {
            uniqueQuantities.set(q.phaseName, q);
        });

        // Iterate through all bases (children of phase) and collect quantities
        phaseObj.children.forEach((baseItem: any) => {
            if (baseItem.quantity) {
                // If quantity is directly on base item
                if (!uniqueQuantities.has(baseItem.quantity)) {
                    uniqueQuantities.set(baseItem.quantity, {
                        phaseName: baseItem.quantity,
                        description: baseItem.quantity,
                    });
                }
            } else if (baseItem.children) {
                // If quantities are in children
                baseItem.children.forEach((q: any) => {
                    const quantityValue = q.value || q;
                    if (!uniqueQuantities.has(quantityValue)) {
                        uniqueQuantities.set(quantityValue, {
                            phaseName: quantityValue,
                            description: quantityValue,
                        });
                    }
                });
            }
        });

        this.quantityOptions = Array.from(uniqueQuantities.values());
    }

    addParameter(): void {
        if (!this.isFormValid()) {
            return;
        }

        const parameter: AdvancedParameter = {
            id: this.generateId(),
            type: this.selectedTab,
            group: this.selectedGroup || '',
            phaseOrChannel: this.selectedPhaseOrChannel || '',
            quantity: this.selectedQuantity || '',
            color: this.selectedColor,
        };

        this.parameterAdded.emit(parameter);
        this.resetForm();
    }

    private isFormValid(): boolean {
        return !!(this.selectedGroup && this.selectedPhaseOrChannel && this.selectedQuantity);
    }

    private resetForm(): void {
        this.selectedGroup = null;
        this.selectedGroupModel = null;
        this.selectedPhaseOrChannel = null;
        this.selectedQuantity = null;
        this.selectedColor = '#800000';
        this.phaseOrChannelOptions = [];
        this.updateQuantityOptions();
    }

    private generateId(): string {
        return Date.now().toString(36) + Math.random().toString(36).substr(2);
    }
}

