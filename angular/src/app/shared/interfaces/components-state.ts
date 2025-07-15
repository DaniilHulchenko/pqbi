import { FeederComponentInfo } from '@shared/service-proxies/service-proxies';
import { PickListState } from './pick-list-state';

export interface IComponentsState {
    components: any[];
    tags: any[];
    feeders?: FeederComponentInfo[];
    pickListState: PickListState;
}
