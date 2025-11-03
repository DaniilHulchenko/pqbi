import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetAdditionalParameterSelectionTabComponent } from './gauge-widget-additional-parameter-selection-tab.component';

describe('GaugeWidgetAdditionalParameterSelectionTabComponent', () => {
  let component: GaugeWidgetAdditionalParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetAdditionalParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetAdditionalParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetAdditionalParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
