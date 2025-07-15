import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { AppFonts } from '@app/shared/enums/app-fonts';

export interface TableDesignOptions {
    headerBackgroundColor: string;
    headerTextColor: string;
    borderColor: string;
    bandedRows: boolean;
    headerFontFamily: string;
    cellFontFamily: string;
}

@Component({
    selector: 'table-design-options',
    templateUrl: './table-design-options.component.html',
    styleUrls: ['./table-design-options.component.css'],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => TableDesignOptionsComponent),
            multi: true
        }
    ]
})
export class TableDesignOptionsComponent implements ControlValueAccessor {
    @Input() designOptions: TableDesignOptions = {
        headerBackgroundColor: '#ffffff',
        headerTextColor: '#333333',
        borderColor: '#cccccc',
        bandedRows: false,
        headerFontFamily: 'Arial, sans-serif',
        cellFontFamily: 'Arial, sans-serif'
    };
    @Output() designOptionsChange = new EventEmitter<TableDesignOptions>();

    fonts = [
        { name: 'Arial', value: AppFonts.ARIAL },
        { name: 'Helvetica', value: AppFonts.HELVETICA },
        { name: 'Times New Roman', value: AppFonts.TIMES_NEW_ROMAN },
        { name: 'Courier New', value: AppFonts.COURIER_NEW },
        { name: 'Verdana', value: AppFonts.VERDANA },
        { name: 'Tahoma', value: AppFonts.TAHOMA },
        { name: 'Trebuchet MS', value: AppFonts.TREBUCHET_MS },
        { name: 'Impact', value: AppFonts.IMPACT },
        { name: 'Georgia', value: AppFonts.GEORGIA },
        { name: 'Palatino', value: AppFonts.PALATINO },
        { name: 'Lucida Sans', value: AppFonts.LUCIDA_SANS },
        { name: 'Comic Sans', value: AppFonts.COMIC_SANS },
        { name: 'Lucida Console', value: AppFonts.LUCIDA_CONSOLE }
    ];

    onChange: any = () => {};
    onTouched: any = () => {};

    writeValue(value: TableDesignOptions): void {
        if (value) {
            this.designOptions = value;
        }
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn;
    }

    updateDesignOptions() {
        this.onChange(this.designOptions);
        this.onTouched();
        this.designOptionsChange.emit(this.designOptions);
    }
}
