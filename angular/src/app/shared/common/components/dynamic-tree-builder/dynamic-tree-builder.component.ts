import { Component, EventEmitter, forwardRef, Output, AfterViewInit, OnInit } from '@angular/core';
import { TreeNode } from 'primeng/api';
import { PickListModule } from 'primeng/picklist';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { PickListState } from '@app/shared/interfaces/pick-list-state';
import { CreateOrEditDefaultValueDto, TagDtoV2 } from '@shared/service-proxies/service-proxies';
import { DefaultValuesService } from '@app/shared/services/default-values-service.service';
import { TreeBuilderService } from '@app/shared/services/tree-builder.service';
import { DxCheckBoxModule, DxButtonModule } from 'devextreme-angular';
import { CommonModule } from '@angular/common';
import { ArrayUtils } from '@app/shared/services/array-utils.service';
import { UtilsModule } from '../../../../../shared/utils/utils.module';

@Component({
    selector: 'app-dynamic-tree-builder',
    standalone: true,
    imports: [PickListModule, DxCheckBoxModule, DxButtonModule, CommonModule, UtilsModule],
    templateUrl: './dynamic-tree-builder.component.html',
    styleUrl: './dynamic-tree-builder.component.css',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => DynamicTreeBuilderComponent),
            multi: true,
        },
    ],
})
export class DynamicTreeBuilderComponent implements OnInit, ControlValueAccessor {
    @Output() pickListChange = new EventEmitter<any>();

    checkedSetAsDefault = false;

    pickListState: PickListState = {
        source: [
            // Initial source nodes here
        ],
        target: [
            // Initial target nodes here
        ],
    };

    components: any;

    tree: TreeNode<any>[] = [];

    private defaultState: PickListState;
    private readonly _defaultStateSettingName = 'UI.Tags.PickList.State';
    private tagsTree: TagDtoV2[];

    private emptyTag: TagDtoV2;
    private emptyTagTree: any;

    constructor(
        private treeBuilderService: TreeBuilderService,
        private defaultValuesService: DefaultValuesService,
    ) {}

    ngOnInit() {
        setTimeout(() => {
            this.treeBuilderService.tagsTree().subscribe((response) => {
                this.tagsTree = response.tags.filter((t) => t.tagName != '');
                this.emptyTag = response.tags.find((t) => t.tagName === '');

                if (this.emptyTag) {
                    this.emptyTag = JSON.parse(JSON.stringify(this.emptyTag));
                    this.emptyTag.tagName = 'Others';
                    this.emptyTagTree = {
                        key: this.emptyTag.tagName,
                        label: this.emptyTag.tagName,
                        expanded: true,
                        selectable: false,
                        children: this.emptyTag.labels[0].components.map((component) => {
                            return {
                                key: component.componentId,
                                label: component.componentName,
                                leaf: true,
                                parameterInfos: component.parameterInfos,
                                selectable: true,
                                data: {
                                    tags: component.tags.map((tag) => tag.tagDescription),
                                    // parameterNames: component.parameterNames.map(parameter => parameter.split('#')[0]),
                                    feeders: component.feeders,
                                },
                            };
                        }),
                    };
                }

                if (this.pickListState.source?.length === 0 && this.pickListState.target?.length === 0) {
                    this.defaultValuesService.getValue(this._defaultStateSettingName).subscribe((result) => {
                        if (result.defaultValue?.value) {
                            this.defaultState = JSON.parse(result.defaultValue?.value);
                            this.prefillPickList();
                            this.checkIfDefault(); // Check if the state is equal to the default state
                        }
                    });
                }

                let tags = this.emptyTag ? [...this.tagsTree, this.emptyTag] : this.tagsTree;

                this.components = this.treeBuilderService.extractLeafNodes(
                    this.treeBuilderService.getTreeByType(tags, 'components'),
                );

                this.prefillPickList();
            });
        });
    }

    apply() {
        this.updateState(this.pickListState.source, this.pickListState.target);
        if (this.checkedSetAsDefault && !this.isStateEqual(this.pickListState, this.defaultState)) {
            // Check if the state is not equal to the default state
            this.defaultValuesService
                .createOrEdit(
                    new CreateOrEditDefaultValueDto({
                        id: null,
                        name: this._defaultStateSettingName,
                        value: JSON.stringify(this.pickListState),
                    }),
                )
                .subscribe(() => {
                    this.defaultState = this.pickListState;
                });
        }
    }

    prefillPickList() {
        if (this.defaultState) {
            this.pickListState = this.defaultState;
            this.updateState(this.pickListState.source, this.pickListState.target);
        } else if (this.tagsTree) {
            this.pickListState.source = this.treeBuilderService.getTreeByType(this.tagsTree, 'labels');
            this.updateState(this.pickListState.source, this.pickListState.target);
        }
    }

    emitEvent() {
        this.pickListChange.emit({
            resultTree: this.tree && this.tree.length > 0 ? this.tree : this.components,
            state: this.pickListState,
        });
        // this.pickListChange.emit({ resultTree: this.tree.value });
    }

    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(obj: any): void {
        if (obj && this.pickListState !== obj) {
            this.pickListState = obj;
            if (this.pickListState.source.length === 0 && this.pickListState.target.length === 0) {
                this.prefillPickList();
            } else if (this.pickListState.target.length > 0) {
                this.updateState(this.pickListState.source, this.pickListState.target);
            }
        }
    }
    registerOnChange(fn: any): void {
        this.onChange = fn; //this.tree.valueChanges.subscribe(fn);
    }
    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    onPickListChange() {
        this.checkIfDefault();
    }

    private updateState(source: TreeNode<any>[], target: TreeNode<any>[]) {
        const filteredSource = source.filter(
            item => !target.some(t => t.key === item.key)
        );
        this.pickListState = {
            source: filteredSource,
            target: [...target],
        };

        this.tree = this.addComponentsRecursively(
            this.convertToTreeNode(this.generateTreeData(this.pickListState.target)),
            this.components,
        );

        if (this.tree?.length > 0 && this.emptyTagTree) {
            this.tree.push(this.emptyTagTree);
        }

        // this.tree.patchValue(
        //     this.addComponentsRecursively(
        //         this.convertToTreeNode(this.generateTreeData(this.pickListState.target)),
        //         this.components,
        //     ),
        // );

        this.emitEvent();
    }

    private convertToTreeNode(data) {
        function createNode(key, value) {
            if (Array.isArray(value)) {
                return {
                    key: key,
                    label: key,
                    expanded: true,
                    children: value.map((item) => {
                        const itemKey = Object.keys(item)[0];
                        return createNode(itemKey, item[itemKey]);
                    }),
                };
            } else if (typeof value === 'object') {
                return {
                    key: key,
                    label: key,
                    children: Object.keys(value).map((subKey) => createNode(subKey, value[subKey])),
                };
            } else {
                return {
                    key: value,
                    label: `${key}: ${value}`,
                };
            }
        }

        return data.map((item) => {
            const key = Object.keys(item)[0];
            return createNode(key, item[key]);
        });
    }

    private generateTreeData(data, level = 0): any[] {
        if (level >= data.length) {
            return [];
        }

        const treeData = [];

        data[level].children.forEach((child) => {
            const children = this.generateTreeData(data, level + 1);
            const node = { [`${data[level].label}:${child.label}`]: children.length ? children : [] };
            treeData.push(node);
        });

        return treeData;
    }

    private addComponentsRecursively(targetTree, components) {
        function matchComponents(node, currentPath) {
            currentPath.push(node.label);

            if (node.children?.length > 0) {
                node.children.forEach((child) => matchComponents(child, currentPath));
            } else {
                // Check for matching components
                components?.forEach((component) => {
                    // find a full match no matter the order of elements
                    if (currentPath.every((tag) => component.data.tags.includes(tag))) {
                        if (!node.children) {
                            node.children = [];
                        }
                        node.children.push(component);
                    }
                });
            }

            currentPath.pop();
        }

        function removeEmptyChildren(arr) {
            return arr
                .map((item) => {
                    // Recursively process the children if they exist
                    if (item.children && item.children.length > 0) {
                        item.children = removeEmptyChildren(item.children);
                    }
                    return item;
                })
                .filter((item) => !item.children || item.children.length > 0); // Remove elements with empty children
        }

        targetTree.forEach((node) => matchComponents(node, []));
        const filteredData = removeEmptyChildren(targetTree);

        return filteredData;
    }

    private checkIfDefault() {
        // Method to automatically set the checkbox
        this.checkedSetAsDefault = this.isStateEqual(this.pickListState, this.defaultState);
    }

    private isStateEqual(state1: PickListState, state2: PickListState): boolean {
        // Method to compare two states
        return (
            JSON.stringify(state1?.source) === JSON.stringify(state2?.source) &&
            JSON.stringify(state1?.target) === JSON.stringify(state2?.target)
        );
    }
}
