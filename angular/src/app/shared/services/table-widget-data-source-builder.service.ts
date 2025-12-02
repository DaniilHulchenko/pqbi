import { Injectable } from '@angular/core';
import { ComponentsState } from '../models/components-state';
import { TagComponentInfo } from '../models/table-widget-component';
import { WidgetParametersColumn } from '../interfaces/widget-parameter-column';
import { TableWidgetResponseItem } from '@shared/service-proxies/service-proxies';


@Injectable({
    providedIn: 'root',
})
export class TableWidgetDataSourceBuilderService {
    convertComponentsTagsArrayToProps(components: ComponentsState, items: any[]) {
        const selectedTagKeys = (components.tags ?? [])
            .map((tag) => this.extractTagKey(tag))
            .filter((key) => !!key);

        return items.map((item) => {
            let component = components.components.find((component) => component.key === item.componentId);

            if (!component) {
                return null;
            }

            component.data.tags.map((tag) => {
                const keyValue = tag.split(':');
                const key = keyValue[0]?.trim();
                const value = keyValue[1]?.trim();
                const isTagSelected = selectedTagKeys.length === 0 || selectedTagKeys.includes(key);
                if (key && value && isTagSelected) {
                    item[key] = value;
                }
            });
            item.componentName = component.label;
            const feeder = component.children.find((f) => f.id === +item.feederId);
            item.feederName = component.label + ': ' + feeder?.label;

            return { ...item };
        }).filter((item) => item !== null);
    }

    extractTagKeys(data: TagComponentInfo[]): string[] {
        const keys: string[] = [];

        data.forEach((item) => {
            item.tags?.forEach((tag) => {
                const key = tag.split(':')[0]?.trim();
                if (!keys.includes(key)) {
                    keys.push(key);
                }
            });
        });

        return keys;
    }

    private extractTagKey(tag: any): string {
        if (!tag) {
            return '';
        }

        if (tag.tagKey) {
            return tag.tagKey;
        }

        if (tag.label) {
            return tag.label.split(':')[0]?.trim();
        }

        if (tag.key) {
            const segments = `${tag.key}`.split(';').pop();
            return segments?.split(':')[0]?.trim();
        }

        return '';
    }

    formatParameterNames(parameters: TableWidgetResponseItem[]) {
        parameters.forEach((param) => {
            if (!param.parameterName.endsWith(param.quantity)) {
                param.parameterName += ` ${param.quantity}`;
            }
        });
    }
}
