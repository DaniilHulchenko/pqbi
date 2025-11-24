import { Injectable } from '@angular/core';
import { Observable, of, share, tap } from 'rxjs';
import { TagTreeRootDto, TreeBuilderServiceProxy } from '@shared/service-proxies/service-proxies';
import { TreeNode } from 'primeng/api';
import { channel } from 'node:diagnostics_channel';

@Injectable({
    providedIn: 'root',
})
export class TreeBuilderService {
    private _tagsTree$: Observable<TagTreeRootDto>;
    private _tagsTree: TagTreeRootDto;

    constructor(private _treeBuilderServiceProxy: TreeBuilderServiceProxy) {}

    tagsTree(): Observable<TagTreeRootDto> {
        if (this._tagsTree) {
            return of(this._tagsTree);
        }
        if (!this._tagsTree$) {
            this._tagsTree$ = this._treeBuilderServiceProxy.tagsTree().pipe(
                tap((tree) => {
                    this._tagsTree = tree;
                }),
                share(),
            );
        }
        return this._tagsTree$;
    }

    getTreeByType(sourceTree: any, treeType: string, filter?: string): TreeNode<any>[] {
        let treeResult;
        switch (treeType) {
            case 'tags':
                treeResult = sourceTree
                    .sort((a, b) => a.tagName.localeCompare(b.tagName))
                    .map((tag) => ({
                        key: `${tag.tagName}`,
                        label: `${tag.tagName}`,
                        selectable: false,
                    }));
                break;
            case 'labels':
                treeResult = sourceTree
                    .sort((a, b) => a.tagName.localeCompare(b.tagName))
                    .map((tag) => ({
                        key: `${tag.tagName}`,
                        label: `${tag.tagName}`,
                        type: 'tag',
                        selectable: false,
                        children: tag.labels
                            .sort((a, b) => a.label.localeCompare(b.label))
                            .map((label) => ({
                                key: `${tag.tagName}:${label.label}`,
                                label: `${label.label}`,
                                type: 'label',
                                selectable: false,
                            })),
                    }));
                break;
            case 'components':
                treeResult = sourceTree.map((tag) => ({
                    key: `${tag.tagName}`,
                    label: `${tag.tagName}`,
                    selectable: false,
                    children: [
                        ...tag.labels.map((label) => ({
                            key: `${tag.tagName}:${label.label}`,
                            label: `${label.label}`,
                            selectable: false,
                            children: [
                                ...label.components.map((component) => ({
                                    key: component.componentId,
                                    label: component.componentName,
                                    leaf: true,
                                    parameterInfos: component.parameterInfos,
                                    additionalDatas: component.additionalDatas,
                                    channels: component.channels,
                                    customBaseInfo: component.customBaseList,
                                    selectable: true,
                                    data: {
                                        tags: component.tags.map((tag) => tag.tagDescription),
                                        // parameterNames: component.parameterNames.map(parameter => parameter.split('#')[0]),
                                        feeders: component.feeders,
                                    },
                                })),
                            ],
                        })),
                    ],
                }));
                break;
            default:
                break;
        }

        return treeResult;
    }

    extractLeafNodes(data) {
        let leafNodes = [];

        function traverse(node) {
            if (node.leaf) {
                const exist = leafNodes.find((item) => item.key === node.key);
                if (!exist) {
                    leafNodes.push(node);
                }
            } else if (node.children) {
                node.children.forEach((child) => traverse(child));
            }
        }

        data.forEach((node) => traverse(node));
        return leafNodes;
    }
}
