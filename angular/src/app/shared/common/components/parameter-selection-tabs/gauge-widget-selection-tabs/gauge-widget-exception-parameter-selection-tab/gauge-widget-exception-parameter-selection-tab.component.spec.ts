import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetExceptionParameterSelectionTabComponent } from './gauge-widget-exception-parameter-selection-tab.component';

describe('GaugeWidgetExceptionParameterSelectionTabComponent', () => {
  let component: GaugeWidgetExceptionParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetExceptionParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetExceptionParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetExceptionParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
