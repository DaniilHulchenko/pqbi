import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { range as _range, sortBy} from 'lodash-es';
import { Options as DataSourceConfig } from 'devextreme/ui/pivot_grid/data_source';

@Injectable({
    providedIn: 'root',
})
export class PQSMockService {
    constructor(private http: HttpClient) {}
    // getObjects(){
    //   return this.http.get('assets/pqs.get-objects.mock.json');
    //   // return this.http.get<GetObjectsResponse>('assets/pqs.get-objects.mock.json');
    // }

    getDataSourceConfig(): Observable<DataSourceConfig> {
        return of({
            fields: [
                {
                    caption: 'Region',
                    dataField: 'region',
                    area: 'row',
                    expanded: true,
                },
                {
                    caption: 'City',
                    dataField: 'city',
                    area: 'row',
                    expanded: true,
                },
                {
                    caption: 'Name',
                    dataField: 'name',
                    area: 'row',
                    expanded: true,
                },
                {
                    caption: 'Header',
                    dataField: 'header',
                    area: 'column',
                    expanded: true,
                },
                {
                    caption: 'AggregationFunc',
                    dataField: 'aggregationFunc',
                    area: 'column',
                    expanded: true,
                },
                {
                    caption: 'Values',
                    // dataField: 'data', /* when using custom summary with options.totalValue as object, omit dataField */
                    dataType: 'number',
                    area: 'data',
                    summaryType: 'custom',
                    calculateCustomSummary: (options) => {
                      switch(options.summaryProcess) {
                        case 'start':

                          options.totalValue = {
                            sum: 0,
                            count: 0,
                            min: Infinity,
                            max: -Infinity,
                            aggregationFunc: '',
                          };
                            break;
                          case 'calculate':
                            options.totalValue.aggregationFunc = options.value.aggregationFunc;
                            options.totalValue.sum += options.value.data;
                            options.totalValue.count++;
                            options.totalValue.min = Math.min(options.totalValue.min, options.value.data);
                            options.totalValue.max = Math.max(options.totalValue.max, options.value.data);
                            break;
                            case 'finalize':
                                // Calculate the final value based on the aggregationFunc
                                // Calculate all aggregations
                                const aggregations = {
                                    sum: options.totalValue.sum,
                                    avg: options.totalValue.sum / options.totalValue.count,
                                    min: options.totalValue.min,
                                    max: options.totalValue.max,
                                    count: options.totalValue.count
                                };

                                switch (options.totalValue.aggregationFunc.toLowerCase()) {
                                    case 'sum':
                                        options.totalValue = aggregations.sum;
                                        break;
                                    case 'avg':
                                        options.totalValue = aggregations.avg;
                                        break;
                                    case 'min':
                                        options.totalValue = aggregations.min;
                                        break;
                                    case 'max':
                                        options.totalValue = aggregations.max;
                                        break;
                                    case 'count':
                                        options.totalValue = aggregations.count;
                                        break;
                                    default:
                                        options.totalValue = aggregations.sum; // default to sum
                                }
                          break;
                      }
                    },
                },
            ],
            store: [
                {
                    guid: '08c3912f-0275-4278-bf86-917168d88eef',
                    name: 'Comp 1',
                    feeders: [1],
                    region: 'North',
                    city: 'Haifa',
                    type: 'CustomParameter',
                    columnName: 'CustomParameter1',
                    header: 'CustomParameter1',
                    aggregationFunc: 'Avg',
                    data: 5.3,
                    showFlagged: false,
                    normalize: 'no',
                },
                {
                    guid: '08c3912f-0275-4278-bf86-917168d88eef',
                    name: 'Comp 1',
                    feeders: [1],
                    region: 'North',
                    city: 'Haifa',
                    type: 'Event',
                    columnName: 'Event1',
                    header: 'Event 1',
                    aggregationFunc: 'Count',
                    data: 2.1,
                    showFlagged: false,
                    normalize: 'no',
                },
                {
                    guid: '08c3912f-0275-4278-bf86-917168d88eef',
                    name: 'Comp 1',
                    feeders: [1],
                    region: 'North',
                    city: 'Haifa',
                    type: 'BaseParameter',
                    columnName: 'BaseParameter1',
                    header: 'BaseParameter 1',
                    aggregationFunc: 'Avg',
                    data: 2,
                    showFlagged: true,
                    normalize: 'nominal',
                },
                {
                    guid: '08c3912f-0275-4278-bf86-917168d88eef',
                    name: 'Comp 3',
                    feeders: [1],
                    region: 'North',
                    city: 'Haifa',
                    type: 'BaseParameter',
                    columnName: 'BaseParameter1',
                    header: 'BaseParameter 1',
                    aggregationFunc: 'Avg',
                    data: 3,
                    showFlagged: true,
                    normalize: 'nominal',
                },
                {
                    guid: 'a059db13-2390-432a-a062-6ac41f213612',
                    name: 'Comp 2',
                    feeders: [1],
                    region: 'Center',
                    city: 'Tel-Aviv',
                    type: 'CustomParameter',
                    columnName: 'CustomParameter1',
                    header: 'CustomParameter1',
                    aggregationFunc: 'Avg',
                    data: 1.5,
                    showFlagged: false,
                    normalize: 'no',
                },
                {
                    guid: 'a059db13-2390-432a-a062-6ac41f213612',
                    name: 'Comp 2',
                    feeders: [1],
                    region: 'Center',
                    city: 'Tel-Aviv',
                    type: 'CustomParameter',
                    columnName: 'CustomParameter1',
                    header: 'CustomParameter1',
                    aggregationFunc: 'Avg',
                    data: 3.5,
                    showFlagged: false,
                    normalize: 'no',
                },
                {
                    guid: 'a059db13-2390-432a-a062-6ac41f213612',
                    name: 'Comp 2',
                    feeders: [1],
                    region: 'Center',
                    city: 'Tel-Aviv',
                    type: 'Event',
                    columnName: 'Event1',
                    header: 'Event 1',
                    aggregationFunc: 'Count',
                    data: 7.0,
                    showFlagged: false,
                    normalize: 'no',
                },
                {
                    guid: 'a059db13-2390-432a-a062-6ac41f213612',
                    name: 'Comp 2',
                    feeders: [1],
                    region: 'Center',
                    city: 'Tel-Aviv',
                    type: 'BaseParameter',
                    columnName: 'BaseParameter1',
                    header: 'BaseParameter 1',
                    aggregationFunc: 'Min',
                    data: 1.1,
                    showFlagged: true,
                    normalize: 'nominal',
                },
            ],
        });
    }

    getPQSTableData() {
        return this.getDataSourceConfig();
    }

    getParameters() {
        return of([
            {
                id: 1,
                type: 'LOGICAL',
                name: 'Negative Sequence (U2)',
                phase: 'V123',
                base: 'BCYC',
                quantity: 'QAVG',
                aggFunc: 'AVG',
                applyTo: [],
            },
            {
                id: 2,
                type: 'LOGICAL',
                name: 'Negative Sequence (U2)',
                phase: 'V123',
                base: 'B1MIN',
                quantity: 'QSAMPLE',
                aggFunc: 'AVG',
                applyTo: [],
            },
            {
                id: 3,
                type: 'CHANNEL',
                name: 'Analog Input',
                channel: '1',
                base: 'BCYC',
                quantity: 'QAVG',
                aggFunc: 'AVG',
                applyTo: [],
            },
        ]);
    }

    getParameterOptions() {
        return of({
            types: [
                /** LOGICAL | CHANNELS | ADDITIONAL */
                {
                    label: 'Logical',
                    value: 'LOGICAL',
                    children: [
                        /** GROUPS */
                        {
                            label: 'Negative Sequence (U2)',
                            value: 'NSU2',
                            children: [
                                /** PHASES */
                                {
                                    label: 'V123',
                                    value: 'V123',
                                    children: [
                                        /** BASES */ { label: 'Cycle', value: 'CYC', quantity: 'AVG' },
                                        { label: 'Half Cycle', value: 'HCYC', quantity: 'AVG' },
                                        { label: '10/12 Cycle', value: '???', quantity: 'AVG' },
                                        { label: '150/180 Cycle', value: '???', quantity: 'AVG' },
                                        { label: '10 Minutes', value: '10MIN', quantity: 'SAMPLE' },
                                        { label: '2 Hours', value: '2HOUR', quantity: 'SAMPLE' },
                                    ],
                                },
                                { label: '68', value: '68', children: [] },
                                { label: 'I123', value: 'I123', children: [] },
                            ],
                        },
                        { label: 'RMS', value: 'RMS', children: [] },
                        { label: 'THD', value: 'THD', children: [] },
                    ],
                },
                { label: 'Channel', value: 'CHANNEL', children: [] },
                { label: 'Additional', value: 'ADDITIONAL', children: [] },
            ],
            aggregationFunctions: sortBy(
                [
                    { name: 'AVG' },
                    { name: 'MAX' },
                    { name: 'MIN' },
                    { name: 'PERCENTILE' },
                    { name: 'ABSOLUTE' },
                    { name: 'COUNT' },
                    { name: 'DIVIDE' },
                    { name: 'EXP' },
                    { name: 'LN' },
                    { name: 'LOG' },
                    { name: 'POWER' },
                    { name: 'SQRT' },
                ],
                ['name'],
            ),

            // feeders: _range(1, 32, 1),
            channels: _range(1, 1000, 1),
            // quantities: [
            //     {label: 'AVG', value: 'AVG'},
            //     {label: 'MIN', value: 'MIN'},
            //     {label: 'MAX', value: 'MAX'},
            // ],
        });
    }
}
