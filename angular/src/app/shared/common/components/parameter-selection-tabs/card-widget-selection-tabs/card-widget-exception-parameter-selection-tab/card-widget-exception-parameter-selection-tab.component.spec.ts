import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetExceptionParameterSelectionTabComponent } from './card-widget-exception-parameter-selection-tab.component';

describe('CardWidgetExceptionParameterSelectionTabComponent', () => {
  let component: CardWidgetExceptionParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetExceptionParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetExceptionParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetExceptionParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
