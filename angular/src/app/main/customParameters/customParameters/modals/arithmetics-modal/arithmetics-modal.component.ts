import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, Injector, Input, OnDestroy, Output, ViewChild, type OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { BsModalRef, ModalDirective } from 'ngx-bootstrap/modal';

@Component({
    selector: 'arithmetics-modal',
    templateUrl: './arithmetics-modal.component.html',
    styleUrls: ['./arithmetics-modal.component.css'],
    providers: [],
})
export class ArithmeticsModalComponent extends AppComponentBase implements OnInit, OnDestroy {
    @Input() parameters;
    @Output() onSelect = new EventEmitter<string>();
    @ViewChild('arithmeticsModal', { static: true }) modal: ModalDirective;
    // @ViewChild('arithmeticsForm', { static: true }) arithmeticsForm: NgForm;

    active = false;
    saving = false;

    numbers = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    operations = ['+', '-', '*', '/'];
    errorMessage: string; // For displaying error messages

    // Flags to track which sections are active
    isOpenParenthesesActive: boolean;
    isVarsActive: boolean;
    isNumbersActive: boolean;
    isPeriodActive: boolean;
    isOperatorsActive: boolean;
    isCloseParenthesesActive: boolean;

    result: string;
    resultBuffer: string[] = [];
    openParenthesesCount = 0;

    constructor(
        injector: Injector,
    ) {
        super(injector);
    }

    resetButtonsAvailability() {
        this.isOpenParenthesesActive = true;
        this.isCloseParenthesesActive = false;
        this.isVarsActive = true;
        this.isNumbersActive = true;
        this.isPeriodActive = false;
        this.isOperatorsActive = false;
    }

    updateButtonsAvailability(currentValue?: string) {
        switch (true) {
            case this.isOpenParenthesesActive && currentValue === '(':
                this.openParenthesesCount++;
                this.isOpenParenthesesActive = true;
                this.isCloseParenthesesActive = true;
                this.isVarsActive = true;
                this.isNumbersActive = true;
                this.isPeriodActive = false;
                this.isOperatorsActive = false;
                break;
            case this.isVarsActive && currentValue.includes('{'):
                this.isOpenParenthesesActive = false;
                this.isCloseParenthesesActive = true;
                this.isVarsActive = false;
                this.isNumbersActive = false;
                this.isPeriodActive = false;
                this.isOperatorsActive = true;
                break;
            case this.isNumbersActive && this.numbers.includes(currentValue):
                this.isOpenParenthesesActive = false;
                this.isCloseParenthesesActive = true;
                this.isVarsActive = false;
                this.isNumbersActive = true;
                this.isPeriodActive = true;
                this.isOperatorsActive = true;
                break;
            case this.isPeriodActive && currentValue === '.':
                this.isOpenParenthesesActive = false;
                this.isCloseParenthesesActive = false;
                this.isVarsActive = false;
                this.isNumbersActive = true;
                this.isPeriodActive = false;
                this.isOperatorsActive = false;
                break;
            case this.isOperatorsActive && this.operations.includes(currentValue):
                this.isOpenParenthesesActive = true;
                this.isCloseParenthesesActive = false;
                this.isVarsActive = true;
                this.isNumbersActive = true;
                this.isPeriodActive = false;
                this.isOperatorsActive = false;
                break;
            case this.isCloseParenthesesActive && currentValue === ')':
                this.openParenthesesCount--;
                this.isOpenParenthesesActive = false;
                this.isCloseParenthesesActive = this.openParenthesesCount === 0 ? false : true;
                this.isVarsActive = false;
                this.isNumbersActive = false;
                this.isPeriodActive = false;
                this.isOperatorsActive = true;
                break;
        }
    }

    onButtonClick(value: string) {
        this.resultBuffer.push(value);
        this.result = this.resultBuffer.join('');
        this.updateButtonsAvailability(value);
        console.log(this.openParenthesesCount);
    }

    deleteLastAction() {
        this.resultBuffer.pop();
        this.result = this.resultBuffer.join('');
        this.updateButtonsAvailability(this.resultBuffer[this.resultBuffer.length - 1]);
    }

    deleteAll() {
        this.resultBuffer = [];
        this.result = this.resultBuffer.join('');
        this.resetButtonsAvailability();
    }

    show(): void {
        this.resetButtonsAvailability();
        this.result = '';
        this.active = true;
        this.modal.show();
    }

    select(): void {
        this.saving = true;

        // Result validation
        if (!this.result) {
            this.errorMessage = 'NoExpressionCreatedError';
            return;
        }

        if (!this.parameters.some(op => this.result.includes(op.name))) {
            this.errorMessage = 'NoExpressionParameterCreatedError';
            return;
        }

        if (this.operations.includes(this.resultBuffer[this.resultBuffer.length - 1])) {
            this.errorMessage = 'ExpressionEndingWithOperatorError';
            return;
        }

        if (['.', '('].includes(this.resultBuffer[this.resultBuffer.length - 1])) {
            this.errorMessage = 'ExpressionEndingWithDotOrOpenParenthesesError';
            return;
        }

        if (!this.areParenthesesBalanced(this.result)) {
            this.errorMessage = 'UnbalancedParenthesesError';
            return;
        }


        // If balanced, emit the result
        this.onSelect.emit(this.result);
        this.saving = false;
        this.close(); // Reset form and close modal
    }

    close(): void {
        this.active = false;

        this.result = '';
        this.resultBuffer = [];
        this.errorMessage = '';

        this.modal.hide();
    }

    // Function to check if parentheses are balanced
    areParenthesesBalanced(expression: string): boolean {
        let stack: string[] = [];
        for (let i = 0; i < expression.length; i++) {
            let char = expression[i];
            if (char === '(') {
                stack.push(char);
            } else if (char === ')') {
                if (stack.length === 0) {
                    return false; // More closing parentheses than opening
                }
                stack.pop();
            }
        }
        return stack.length === 0; // If stack is empty, parentheses are balanced
    }

    ngOnInit(): void {

    }

    ngOnDestroy() {
    }

}
