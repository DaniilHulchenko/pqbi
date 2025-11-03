import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetParameterAdvancedSettingsComponent } from './gauge-widget-parameter-advanced-settings.component';

describe('GaugeWidgetParameterAdvancedSettingsComponent', () => {
  let component: GaugeWidgetParameterAdvancedSettingsComponent;
  let fixture: ComponentFixture<GaugeWidgetParameterAdvancedSettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetParameterAdvancedSettingsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetParameterAdvancedSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
