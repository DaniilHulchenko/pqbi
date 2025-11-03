import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetCustomParameterSelectionTabComponent } from './card-widget-custom-parameter-selection-tab.component';

describe('CardWidgetCustomParameterSelectionTabComponent', () => {
  let component: CardWidgetCustomParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetCustomParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetCustomParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetCustomParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
