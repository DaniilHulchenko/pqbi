import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetAdditionalParameterSelectionTabComponent } from './card-widget-additional-parameter-selection-tab.component';

describe('CardWidgetAdditionalParameterSelectionTabComponent', () => {
  let component: CardWidgetAdditionalParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetAdditionalParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetAdditionalParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetAdditionalParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
