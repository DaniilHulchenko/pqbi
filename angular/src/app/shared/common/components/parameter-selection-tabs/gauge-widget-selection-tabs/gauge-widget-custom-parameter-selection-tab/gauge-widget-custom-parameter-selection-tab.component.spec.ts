import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetCustomParameterSelectionTabComponent } from './gauge-widget-custom-parameter-selection-tab.component';

describe('GaugeWidgetCustomParameterSelectionTabComponent', () => {
  let component: GaugeWidgetCustomParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetCustomParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetCustomParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetCustomParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
