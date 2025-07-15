import { CommonModule } from '@angular/common';
import { Component, EventEmitter, forwardRef, Input, Output } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ComponentsState } from '@app/shared/models/components-state';
import { DxButtonModule, DxScrollViewModule, DxPopoverModule, DxLoadIndicatorComponent, DxLoadIndicatorModule } from 'devextreme-angular';
import { TreeModule } from 'primeng/tree';
import { DynamicTreeBuilderComponent } from '../dynamic-tree-builder/dynamic-tree-builder.component';
import { TreeBuilderService } from '@app/shared/services/tree-builder.service';
import { FeederComponentInfo } from '@shared/service-proxies/service-proxies';
import { UtilsModule } from '../../../../../shared/utils/utils.module';
import { Subscription, timer } from 'rxjs';


@Component({
    selector: 'componentsSelector',
    standalone: true,
    imports: [
    CommonModule,
    DxButtonModule,
    DxScrollViewModule,
    DxPopoverModule,
    FormsModule,
    TreeModule,
    DynamicTreeBuilderComponent,
    DxLoadIndicatorModule,
    UtilsModule
],
    templateUrl: './components-selector.component.html',
    styleUrl: './components-selector.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ComponentsSelectorComponent),
            multi: true,
        },
    ],
})
export class ComponentsSelectorComponent implements ControlValueAccessor {
    @Input() height: string;
    @Input() isFeederSelectionEnabled = false;
    @Input() componentsOptions: any;
    @Output() componentErrorChange = new EventEmitter<string>();
    @Output() feederErrorChange = new EventEmitter<string>();

    state: ComponentsState = new ComponentsState({ components: null, tags: null, pickListState: { source: [], target: [] } });

    timer: Subscription;

    selectedItems: any[];

    tree: any;

    expandTags = true;
    treeSettingsIsOpen = false;
    componentError = '';
    feederError = '';
    isExpandButtonDisabled = true;

    isLoading = true;

    constructor(
        private treeBuilderService: TreeBuilderService,
    ) {}

    get scrollHeight() {
        return this.height ? `height: ${this.height};` : '';
    }

    get formattedComponentsOptions() {
        return this.expandTags
            ? this.componentsOptions
            : this.treeBuilderService.extractLeafNodes(this.componentsOptions);
    }

    onChange: any = () => {};
    onTouched: any = () => {};

    ngOnInit() {
        this.isLoading = true;

        this.treeBuilderService.tagsTree().subscribe({
            next: (treeData) => {
                this.tree = treeData;
                this.componentsOptions = this.tree;
                this.isLoading = false;
            }
        });
    }

    writeValue(state: ComponentsState): void {
        if (state) {
            this.state = state;
            this.selectedItems = this.state.tags ? [...this.state.tags] : [];
            this.selectedItems.push(...this.state.components);
            this.state.feeders?.forEach((feeder) => {
                this.selectedItems.push(this.createFeederNode(feeder.componentId, feeder));
            });
        if (this.componentsOptions) {
                this.isLoading = false;
            }
        } else {
            this.state = new ComponentsState({ components: null, tags: null, pickListState: { source: [], target: [] } });
            this.selectedItems = null;
            this.componentsOptions = this.tree;
        }
        this.validateSelection();
    }

    isLastParentInTree(node: any): boolean {
        if (!node.children || node.children.length === 0) {
            return false;
        }
        
        const allChildrenAreLeaves = node.children.every(child => !child.children || child.children.length === 0);
        return allChildrenAreLeaves;
    }
    
    getIconForTree(): string {
        // Temporary set same icon for all, when API was be able accept iconType endpoint we can show different icons for different components.
        return 'assets/common/images/icons/G53D.png';
    }    

    changeExpandState() {
        this.expandTags = !this.expandTags;
    }

    changeTreeSettingsState() {
        this.treeSettingsIsOpen = !this.treeSettingsIsOpen;
    }

    closeTreeSettingsState() {
        this.treeSettingsIsOpen = false;
    }

    onSelectionChange(event: any[]) {
        this.selectedItems = event;

        let tags = this.getDistinctComponents(
            event.filter((component) => !(component.type === TreeComponentType.Feeder || component.leaf)),
        );

        let componentsAndFeeders = this.getDistinctComponents(
            event.filter((component) => component.type === TreeComponentType.Feeder || component.leaf),
        );

        if (this.isFeederSelectionEnabled) {
            let components = this.treeBuilderService.extractLeafNodes(this.tree);

            let selectedComponents = componentsAndFeeders?.filter((item) => item.type !== TreeComponentType.Feeder && item.leaf);
            let selectedFeeders = componentsAndFeeders?.filter((item) => item.type === TreeComponentType.Feeder);

            selectedFeeders.forEach((feeder) => {
                if (!selectedComponents.some((component) => component.key === feeder.parentKey)) {
                    let missingComponent = components.find((component) => component.key === feeder.parentKey);
                    if (missingComponent) {
                        componentsAndFeeders.push(missingComponent);
                        this.selectedItems.push(missingComponent);
                    }
                }
            });

            let previouslySelectedComponents = this.selectedItems?.filter(
                (item) => item.type !== TreeComponentType.Feeder && item.leaf,
            );
            let addedComponents = selectedComponents?.filter(
                (newComponent) =>
                    !previouslySelectedComponents?.some(
                        (previouslySelectedComponent) => previouslySelectedComponent.key === newComponent.key,
                    ),
            );
            this.addFeedersToComponents(addedComponents);
        }

        tags.forEach(t => this.removeRedundantInfoInTag(t));

        this.state.tags = tags;
        this.state.components = componentsAndFeeders.filter((item) => item.leaf);
        this.state.feeders = componentsAndFeeders
            .filter((item) => item.type === TreeComponentType.Feeder)
            ?.map((feeder) => {
                return new FeederComponentInfo({ id: feeder.id, name: feeder.label, componentId: feeder.parentKey, compName: '' });
            });
        this.onChange(this.state);
        this.validateSelection();
    }

    onNodeUnselect(event) {
        if (event.node.type === TreeComponentType.Feeder) {
            if (!this.state.components.some((item) => item.key === event.node.parentKey)) {
                this.timer = timer(50).subscribe(() => {
                    const components = this.treeBuilderService.extractLeafNodes(this.componentsOptions);
                    this.selectedItems.push(components.find((item) => item.key === event.node.parentKey));
                    this.onSelectionChange(this.selectedItems);
                    console.log('onNodeUnselect', this.state);
                });
            }
        }
    }

    onPickListChange(newState: any) {
        this.state.pickListState = newState.state;
        this.tree = newState.resultTree;
        this.componentsOptions = this.tree;
        // Add feeders to all components in advance
        const leafComponents = this.treeBuilderService.extractLeafNodes(this.componentsOptions);
        this.addFeedersToComponents(leafComponents);
        this.isExpandButtonDisabled = !this.tree.some(item => !item.leaf);
        this.validateSelection();
        this.closeTreeSettingsState();
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    private validateSelection() {
        this.componentError = '';
        this.feederError = '';

        const selectedComponents = this.state?.components?.filter((item) => item.leaf) || [];
        const selectedFeeders = this.state?.feeders || [];

        if (!selectedComponents.length) {
            this.componentError = 'Select at least one component.';
            return;
        }

        if (
            this.isFeederSelectionEnabled &&
            selectedComponents.some((component) => component.data?.feeders?.length > 0)
        ) {
            const feederRequiredComponents = selectedComponents
                .filter((comp) => comp.data?.feeders?.length > 0)
                .map((comp) => comp.key);
            const selectedFeederKeys = selectedFeeders.map((feeder) => feeder.componentId);
            const missingFeeders = feederRequiredComponents.some((compKey) => !selectedFeederKeys.includes(compKey));
        }
    }

    private addFeedersToComponents(addTo: any[]) {
        if (this.isFeederSelectionEnabled) {
            let components = this.treeBuilderService.extractLeafNodes(this.componentsOptions);
            addTo
                ?.filter((item) => item.type !== TreeComponentType.Feeder)
                ?.forEach((selectedItem) => {
                    let componentInTree = components.find((component) => component.key === selectedItem.key);
                    componentInTree.children = componentInTree.data.feeders.map((feeder) => {
                        return this.createFeederNode(componentInTree.key, feeder);
                    });
                    componentInTree.expanded = true;
                });
        }
    }

    private removeRedundantInfoInTag(tag){
        if (!tag) {
            return;
        }

        tag.parent = null;

        for(let child of tag.children ?? []){
            this.removeRedundantInfoInTag(child)
        }
    }

    private createFeederNode(parentKey, feeder) {
        return {
            type: TreeComponentType.Feeder,
            key: `${parentKey}_${feeder.id}_${feeder.name}`,
            id: feeder.id,
            label: feeder.name,
            parentKey: parentKey,
        };
    }

    private getDistinctComponents(components: any[]) {
        const seen = new Set();
        return components.filter((item) => {
            const value = item['key'];
            if (seen.has(value)) {
                return false;
            }
            seen.add(value);
            return true;
        });
    }
}

enum TreeComponentType {
    Component = 'Component',
    Feeder = 'Feeder',
}
