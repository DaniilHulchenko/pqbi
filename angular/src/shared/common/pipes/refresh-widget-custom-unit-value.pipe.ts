import { Pipe, PipeTransform } from '@angular/core';
import { RefreshSelectionCustomUnits } from '@app/shared/enums/refresh-selection-custom-units';

@Pipe({
    name: 'refreshWidgetCustomUnitValue',
})
export class RefreshWidgetCustomUnitValuePipe implements PipeTransform {
    transform(unit: RefreshSelectionCustomUnits): number {
        switch(unit) {
            case RefreshSelectionCustomUnits.Sec:
                return 1;
            case RefreshSelectionCustomUnits.Min:
                return 60;
            case RefreshSelectionCustomUnits.Hour:
                return 3600;
            case RefreshSelectionCustomUnits.Day:
                return 86400;
            case RefreshSelectionCustomUnits.Week:
                return 86400 * 7;
            case RefreshSelectionCustomUnits.Month:
                return 86400 * 30;
            case RefreshSelectionCustomUnits.Year:
                return 86400 * 365;
        }
    }
}
