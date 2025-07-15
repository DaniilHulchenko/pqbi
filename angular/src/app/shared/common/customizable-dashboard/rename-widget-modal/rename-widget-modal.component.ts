import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';

@Component({
    selector: 'rename-widget-modal',
    templateUrl: './rename-widget-modal.component.html',
    styleUrl: './rename-widget-modal.component.css',
})
export class RenameWidgetModalComponent {
    @ViewChild('renameForm') pqsForm: NgForm;
    @Output() onSave: EventEmitter<string> = new EventEmitter<string>();

    protected popupVisible = false;

    protected name: string;

    show(name: string) {
        this.name = name;
        this.open();
    }

    protected save() {
        this.onSave.emit(this.name);
        this.hide();
    }

    private open() {
        this.popupVisible = true;
    }

    protected hide() {
        this.popupVisible = false;
    }
}
