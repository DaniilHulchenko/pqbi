import { Pipe, PipeTransform } from '@angular/core';
import { RefreshSelectionUnits } from '@app/shared/enums/refresh-selection-units';

@Pipe({
    name: 'refreshWidgetUnitValue',
})
export class RefreshWidgetUnitValuePipe implements PipeTransform {
    transform(unit: RefreshSelectionUnits): number {
        switch(unit) {
            case RefreshSelectionUnits.NEVER:
                return -1;
            case RefreshSelectionUnits.SEC5:
                return 5;
            case RefreshSelectionUnits.SEC10:
                return 10;
            case RefreshSelectionUnits.MIN1:
                return 60;
            case RefreshSelectionUnits.MIN5:
                return 300;
            case RefreshSelectionUnits.MIN10:
                return 600;
            case RefreshSelectionUnits.HOUR1:
                return 3600;
            case RefreshSelectionUnits.HOUR12:
                return 43200;
            case RefreshSelectionUnits.DAY1:
                return 86400;
        }
    }
}
