import { Component, HostBinding, Input } from '@angular/core';
import { WidgetParametersColumn } from '@app/shared/interfaces/widget-parameter-column';
import { ComponentsState } from '@app/shared/models/components-state';
import { TableWidgetDataSourceBuilderService } from '@app/shared/services/table-widget-data-source-builder.service';
import { TableDesignOptions } from '../table-design-options/table-design-options.component';
import { TagComponentInfo } from '@app/shared/models/table-widget-component';

@Component({
    selector: 'table-preview',
    styleUrls: ['./table-preview.component.css'],
    templateUrl: './table-preview.component.html',
})
export class TablePreviewComponent {
    @HostBinding('style.--header-bg-color') headerBgColor: string;
    @HostBinding('style.--header-text-color') headerTextColor: string;
    @HostBinding('style.--border-color') borderColor: string;
    @HostBinding('style.--cell-font-family') cellFontFamily: string;
    @HostBinding('style.--header-font-family') headerFontFamily: string;

    protected dataSource: any;
    protected columns: any;

    private _parameters: WidgetParametersColumn[];
    private _componentsState: ComponentsState;
    private _designOptions: TableDesignOptions;

    constructor(
        private _tableWidgetDataSourseBuilder: TableWidgetDataSourceBuilderService) {}

    get designOptions(): TableDesignOptions {
        return this._designOptions;
    }

    @Input() set parameters(parameters: WidgetParametersColumn[]) {
        this._parameters = parameters;
        this.refreshTable();
    }
    @Input() set components(componentsState: ComponentsState) {
        this._componentsState = componentsState;
        this.refreshTable();
    }
    @Input() set designOptions(options: TableDesignOptions) {
        if (options) {
            this._designOptions = { ...options };
            this.borderColor = this._designOptions.borderColor;
            this.headerBgColor = this._designOptions.headerBackgroundColor;
            this.cellFontFamily = this._designOptions.cellFontFamily;
            this.headerFontFamily = this._designOptions.headerFontFamily;
            this.headerTextColor = this._designOptions.headerTextColor;
            this.refreshTable();
        }
    }

    private refreshTable() {
        if (!this._componentsState || !this._parameters) {
            return;
        }
        let items = this.GenerateCombinations();

        let components = this._componentsState.components?.map((treeNode) => {
            return new TagComponentInfo(treeNode.key, treeNode.label, treeNode.data?.tags);
        });
        let extractedTags = this._tableWidgetDataSourseBuilder.extractTagKeys(components);
        const orderedTags = this._componentsState.tags.map(t => t.key);
        if (orderedTags.length === 0) {
            orderedTags.push(...extractedTags);
        }
        items = items.filter(i =>
            this._componentsState.components.some(c => c.key === i.componentId)
            && (i.feederId == null || this._componentsState.feeders?.some(f =>
                f.componentId === i.componentId && f.id === i.feederId
            ))
        );
        items = this._tableWidgetDataSourseBuilder.convertComponentsTagsArrayToProps(this._componentsState, items);

        const treeData: any[] = [];
        const treeMap = new Map<string, any>();
        const feedersByComponent = items
            .filter(i => i.feederId != null)
            .reduce((map, item) => {
                const compKey = `comp_${item.componentName}`;
                const arr = map.get(compKey) || [];
                arr.push(item);
                map.set(compKey, arr);
                return map;
            }, new Map<string, any[]>());

        items.forEach((i) => {
            const tagPath = orderedTags.map(tag => i[tag]).filter(Boolean);
            let parentId: string | null = null;
            tagPath.forEach((tag, index) => {
                const tagKey = `tag_${tagPath.slice(0, index + 1).join('_')}`;
                if (!treeMap.has(tagKey)) {
                    const tagNode = {
                        id: tagKey,
                        parentId: parentId,
                        name: tag,
                        expanded: true
                    };
                    treeData.push(tagNode);
                    treeMap.set(tagKey, tagNode);
                }
                parentId = tagKey;
            });

            const componentKey = `comp_${i.componentName}`;
            if (!treeMap.has(componentKey)) {
                const componentNode = {
                    id: componentKey,
                    parentId: parentId,
                    name: i.componentName,
                    expanded: true,
                    values: {}
                };
                treeData.push(componentNode);
                treeMap.set(componentKey, componentNode);
            }
            const feeders = feedersByComponent.get(componentKey) || [];
            if (feeders.length > 1 && i.feederId != null) {
                const feederKey = `feeder_${i.feederName}_${i.componentId}_${i.feederId}`;
                if (!treeMap.has(feederKey)) {
                    const feederNode = {
                        id: feederKey,
                        parentId: componentKey,
                        name: i.feederName,
                        expanded: true
                    };
                    treeData.push(feederNode);
                    treeMap.set(feederKey, feederNode);
                }
            }
        });

        const parameterNames = this._parameters.map(p => p.name + ' ' + p.quantity);
        this.columns = [
            { dataField: 'name', caption: '' },
            ...parameterNames.map(p => ({
                dataField: p,
                caption: p,
            }))
        ];
        this.dataSource = treeData;
    }


    private GenerateCombinations(): any[] {
        const result: { componentId: string; parameterName: string; feederId?: number }[] = [];
        for (const component of this._componentsState.components ?? []) {
            for (const parameter of this._parameters) {
                const associatedFeeders = this._componentsState.feeders?.filter((feeder) => feeder.componentId === component.key);
                if (associatedFeeders.length > 0) {
                    for (const feeder of associatedFeeders) {
                        result.push({ componentId: component.key, parameterName: parameter.name + ' ' + parameter.quantity, feederId: feeder.id });
                    }
                } else {
                    result.push({ componentId: component.key, parameterName: parameter.name + ' ' + parameter.quantity });
                }
            }
        }
        return result;
    }

}
