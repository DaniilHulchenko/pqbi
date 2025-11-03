import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GaugeWidgetStyleSelectorComponent } from './gauge-widget-style-selector.component';

describe('GaugeWidgetStyleSelectorComponent', () => {
  let component: GaugeWidgetStyleSelectorComponent;
  let fixture: ComponentFixture<GaugeWidgetStyleSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GaugeWidgetStyleSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GaugeWidgetStyleSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
