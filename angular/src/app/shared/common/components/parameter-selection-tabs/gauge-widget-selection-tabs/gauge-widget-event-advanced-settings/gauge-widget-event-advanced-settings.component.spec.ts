import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetEventAdvancedSettingsComponent } from './gauge-widget-event-advanced-settings.component';

describe('GaugeWidgetEventAdvancedSettingsComponent', () => {
  let component: GaugeWidgetEventAdvancedSettingsComponent;
  let fixture: ComponentFixture<GaugeWidgetEventAdvancedSettingsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetEventAdvancedSettingsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetEventAdvancedSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
