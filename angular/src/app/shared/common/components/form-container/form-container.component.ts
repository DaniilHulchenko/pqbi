import { Component, HostBinding, Input } from '@angular/core';
import { NgClass } from '@node_modules/@angular/common';

@Component({
  selector: 'form-container',
  standalone: true,
  imports: [],
  templateUrl: './form-container.component.html',
  styleUrl: './form-container.component.css',
})
export class FormContainerComponent {
    @Input() wrapperClass = ''

    @HostBinding('class')
    get hostClasses(): string {
        return this.wrapperClass
    }
}
