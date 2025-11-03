import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetLogicalParameterSelectionTabComponent } from './card-widget-logical-parameter-selection-tab.component';

describe('CardWidgetLogicalParameterSelectionTabComponent', () => {
  let component: CardWidgetLogicalParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetLogicalParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetLogicalParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetLogicalParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
