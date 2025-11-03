import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidgetChannelParameterSelectionTabComponent } from './card-widget-channel-parameter-selection-tab.component';

describe('CardWidgetChannelParameterSelectionTabComponent', () => {
  let component: CardWidgetChannelParameterSelectionTabComponent;
  let fixture: ComponentFixture<CardWidgetChannelParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidgetChannelParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CardWidgetChannelParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
