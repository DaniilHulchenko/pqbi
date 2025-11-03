import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetEventParameterSelectionTabComponent } from './gauge-widget-event-parameter-selection-tab.component';

describe('GaugeWidgetEventParameterSelectionTabComponent', () => {
  let component: GaugeWidgetEventParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetEventParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetEventParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetEventParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
