import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetChannelParameterSelectionTabComponent } from './gauge-widget-channel-parameter-selection-tab.component';

describe('GaugeWidgetChannelParameterSelectionTabComponent', () => {
  let component: GaugeWidgetChannelParameterSelectionTabComponent;
  let fixture: ComponentFixture<GaugeWidgetChannelParameterSelectionTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetChannelParameterSelectionTabComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetChannelParameterSelectionTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
