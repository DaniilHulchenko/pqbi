import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateRangeAndResolutionSelectorComponent } from './date-range-and-resolution-selector.component';

describe('DateRangeAndResolutionSelectorComponent', () => {
  let component: DateRangeAndResolutionSelectorComponent;
  let fixture: ComponentFixture<DateRangeAndResolutionSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DateRangeAndResolutionSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DateRangeAndResolutionSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
