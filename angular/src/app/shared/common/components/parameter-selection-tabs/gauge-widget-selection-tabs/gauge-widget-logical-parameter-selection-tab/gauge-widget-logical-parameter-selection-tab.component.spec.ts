import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetLogicalParameterSelectionTabComponent } from './gauge-widget-logical-parameter-selection-tab.component';

describe('GaugeWidgetLogicalParameterSelectionTabComponent', () => {
  let component: GaugeWidgetLogicalParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetLogicalParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetLogicalParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetLogicalParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
