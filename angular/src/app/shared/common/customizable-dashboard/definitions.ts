export class WidgetViewDefinition {
    id: string;
    component: any;
    defaultWidth: number;
    defaultHeight: number;

    constructor(id: string, component: any, defaultWidth: number = 6, defaultHeight: number = 15, config: string = undefined) {
        this.id = id;
        this.component = component;
        this.defaultWidth = defaultWidth;
        this.defaultHeight = defaultHeight;
    }
}

export class WidgetFilterViewDefinition {
    id: string;
    component: any;
    constructor(id: string, component: any) {
        this.id = id;
        this.component = component;
    }
}
