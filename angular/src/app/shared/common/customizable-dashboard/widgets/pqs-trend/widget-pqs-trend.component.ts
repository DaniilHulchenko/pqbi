import { Component, ElementRef, Injector, OnDestroy, OnInit } from '@angular/core';
import { PQBIAxisData, TenantDashboardServiceProxy } from '@shared/service-proxies/service-proxies';
import { WidgetComponentBaseComponent } from '../widget-component-base'
import { CustomParametersServiceProxy } from '@shared/service-proxies/service-proxies';

// import { multi } from './data';
import { NgForm } from '@angular/forms';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { Subject, interval, switchMap, takeUntil, timer } from 'rxjs';

@Component({
  selector: 'app-widget-pqs-trend',
  templateUrl: './widget-pqs-trend.component.html',
  styleUrls: ['./widget-pqs-trend.component.css'],
})
export class WidgetPQSTrendComponent extends WidgetComponentBaseComponent implements OnInit, OnDestroy {
  stopStream$ = new Subject()
  isActive = false;

  /** form props */
  formModel = {
    customParameter: null,
    startDate: null,
    endDate: new Date(),
    resolution: 'IS1HOUR',
    percentile: null
  };


  customParameterOptions: any[];

  resolutionOptions = [
    { name: 'IS1HOUR' },
    { name: 'IS2HOUR' },
    { name: 'IS1DAY' },
  ];

  percentileOptions = [
    { name: '0.25', value: 0.25 },
    { name: '0.5', value: 0.5 },
    { name: '0.75', value: 0.75 },
  ];


  selectedPercentile: number;

  /** chart props */
  multi: any[];

  view: any[] = [1000, 400];

  // options
  legend = true;
  showLabels = true;
  animations = true;
  xAxis = true;
  yAxis = true;
  showYAxisLabel = true;
  showXAxisLabel = true;
  xAxisLabel = 'Time';
  yAxisLabel = 'Values';
  timeline = true;
  bsConfig: Partial<BsDatepickerConfig> = { dateInputFormat: 'YYYY-M-DTh:m:s.SSSSSSSSSZ', maxDate: new Date(), showClearButton: true, clearPosition: 'right', showTodayButton: true, todayPosition: 'left' }

  colorScheme = {
    domain: ['#5AA454', '#E44D25', '#CFC0BB', '#7aa3e5', '#a8385d', '#aae3f5']
  };
  preContainerWidth: number;
  preContainerHeight: number;
functions: any;
selectedFunctions: any;



  constructor(injector: Injector,
    private _tenantDashboardService: TenantDashboardServiceProxy,
    private _CustomParametersServiceProxy: CustomParametersServiceProxy,
    private elementReference: ElementRef) {
    super(injector, elementReference);
    // Object.assign(this, { multi });
  }

  deleteParameter(_t73: any) {
    throw new Error('Method not implemented.');
  }
    editParameter(_t73: any) {
    throw new Error('Method not implemented.');
  }


  ngOnInit(): void {
    // this.subHelloWorldFilter();
    this.runDelayed(() => {
      this.getAllCustomParameters();
      this.setChartDimensions();
    })
  }

  setChartDimensions() {
    // const preContainerWidth = document.querySelector('.pre-container').clientWidth;
    // const preContainerHeight = document.querySelector('.pre-container').clientHeight;
    const chartContainerWidth = document.querySelector('.chart-container').clientWidth;
    const chartContainerHeight = document.querySelector('.chart-container').clientHeight;

    // this.preContainerWidth = preContainerWidth;
    // this.preContainerHeight = preContainerHeight;
    this.view = [chartContainerWidth, chartContainerHeight];
  }

  getAllCustomParameters() {
    this._CustomParametersServiceProxy.getAll(undefined,undefined, undefined, undefined, undefined, undefined, undefined, undefined)
    .subscribe(result => {
      this.customParameterOptions = result.items.map(item => {
        const parsedParameterList = JSON.parse(item.customParameter.stdpqsParametersList);
        return {
          name: item.customParameter.name,
          value: {
            id: item.customParameter.id,
            name: item.customParameter.name,
            aggregationFunction: item.customParameter.aggregationFunction,
            parameterList: parsedParameterList.map(param => {
              const { ParamName, componentId, quantity } = param.data;
              return { ParamName, componentId, quantity };
            })
          }
        }
      })
    })
  }

  // postPQSTrendData = (input: any): void => {
  //   timer(0, 60000) //every 1 minute
  //     .pipe(
  //       takeUntil(this.stopStream$),
  //       switchMap(() => this._tenantDashboardService.postPQSData(input))
  //     )
  //     .subscribe((result: PQSOutput) => {
  //       const combinedSeriesData = this.combineAndAggregateSeriesData(result.data);
  //       console.log('groupBySeriesDataByName', combinedSeriesData)

  //       this.multi = combinedSeriesData;

  //     });
  // }

  onSubmit(form: any) {
    console.log('onSubmit:',form);
  }

  // reload() {
  //   // this.showLoading();
  //   this.runDelayed(() => {
  //     this.postPQSTrendData(this.formModel);
  //     console.log('reload:',this.formModel)
  //   })
  // }

  start(){
    this.isActive = true
    // this.postPQSTrendData(this.formModel);

  }

  stop(){
    this.isActive = false;
    this.stopStream$.next(null);
  }



  avg(data: Array<number>): number {
    const total = data.reduce((sum, item) => sum + item, 0);
    const average = total / data.length;
    return average;
  }

  percentile(arr: Array<number>, p: number) {
    if (arr.length === 0) {
      return 0;
    }

    // Sort the array in ascending order
    arr.sort((a: number, b: number) => a - b);

    // Calculate the index for the percentile
    const index = (p / 100) * (arr.length - 1);

    if (Number.isInteger(index)) {
        // If the index is an integer, return the element at that index
        return arr[index];
    } else {
        // If the index is not an integer, interpolate between the two surrounding elements
        const lowerIndex = Math.floor(index);
        const upperIndex = Math.ceil(index);
        const weight = index - lowerIndex;
        return arr[lowerIndex] * (1 - weight) + arr[upperIndex] * weight;
    }
}

  combineAndAggregateSeriesData = (data: PQBIAxisData[]) => {
    const combined = data.reduce((acc, parameter) => {
      // Combine the parameter names
      acc.name += (acc.name ? '\n' : '') + parameter.parameterName;

      // Collect the values for each dateTime
      parameter.dataTimeStamps.forEach(({ dateTime, point }) => {
        const key = dateTime.toISO();
        if(!acc.series[key]){
          acc.series[key] = [];
        }
        acc.series[key].push(point || 0.0);
      });


      // Aggregate the values for each key
      Object.keys(acc.series).forEach(key => {
        switch(this.formModel.customParameter?.value?.aggregationFunction){
          case 'AVG':
            acc.result.push({name: key, value: this.avg(acc.series[key])}) ;
            break;
          case 'MAX':
            acc.result.push( {name: key, value: Math.max(...acc.series[key])});
            break;
            case 'MIN':
            acc.result.push({ name: key, value: Math.min(...acc.series[key])}) ;
            break;
            case 'PERCENTILE':
            acc.result.push({ name: key, value: this.percentile(acc.series[key], this.formModel.percentile)}) ;
            break;
          default:
        }

      })

      return acc;
    }, { name: '', series: {}, result: [{name: '', value: 0.0}] });
      console.log('combined:', combined);


    // Return the final structured result
    return [{ name: combined.name, series: combined.result }];
  };






  /** Chart */
  // onSelect(data): void {
  //   console.log('Item clicked', JSON.parse(JSON.stringify(data)));
  // }

  // onActivate(data): void {
  //   console.log('Activate', JSON.parse(JSON.stringify(data)));
  // }

  // onDeactivate(data): void {
  //   console.log('Deactivate', JSON.parse(JSON.stringify(data)));
  // }
}

