import { Pipe, PipeTransform } from '@angular/core';
import { RefreshSelectionUnits } from '@app/shared/enums/refresh-selection-units';
import { LocalizePipe } from './localize.pipe';

@Pipe({
    name: 'refreshWidget',
})
export class RefreshWidgetPipe implements PipeTransform {
    constructor(private localizePipe: LocalizePipe) {}
    transform(unit: RefreshSelectionUnits): string {
        switch(unit) {
            case RefreshSelectionUnits.NEVER:
                return this.localizePipe.transform('Never');
            case RefreshSelectionUnits.SEC5:
                return this.localizePipe.transform('SEC5');
            case RefreshSelectionUnits.SEC10:
                return this.localizePipe.transform('SEC10');
            case RefreshSelectionUnits.MIN1:
                return this.localizePipe.transform('MIN1');
            case RefreshSelectionUnits.MIN5:
                return this.localizePipe.transform('MIN5');
            case RefreshSelectionUnits.MIN10:
                return this.localizePipe.transform('MIN10');
            case RefreshSelectionUnits.HOUR1:
                return this.localizePipe.transform('HOUR1');
            case RefreshSelectionUnits.HOUR12:
                return this.localizePipe.transform('HOUR12');
            case RefreshSelectionUnits.DAY1:
                return this.localizePipe.transform('DAY1');
            case RefreshSelectionUnits.INTERVAL:
                return this.localizePipe.transform('INTERVAL');
            default:
                return '';
        }
    }
}
