import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetEventParameterSelectionTabComponent } from './card-widget-event-parameter-selection-tab.component';

describe('CardWidgetEventParameterSelectionTabComponent', () => {
  let component: CardWidgetEventParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetEventParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetEventParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetEventParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
