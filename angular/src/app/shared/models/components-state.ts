import { FeederComponentInfo } from '@shared/service-proxies/service-proxies';
import { IComponentsState } from '../interfaces/components-state';
import { PickListState } from '../interfaces/pick-list-state';

export class ComponentsState implements IComponentsState {
    components: any[];
    tags: any[];
    feeders?: FeederComponentInfo[];
    pickListState: PickListState;

    constructor(state: IComponentsState){
        this.components = state.components;
        this.pickListState = state.pickListState;
        this.feeders = state.feeders;
    }
}
