import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WidgetPqsGaugeComponent } from './widget-pqs-gauge.component';

describe('WidgetPqsGaugeComponent', () => {
  let component: WidgetPqsGaugeComponent;
  let fixture: ComponentFixture<WidgetPqsGaugeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WidgetPqsGaugeComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(WidgetPqsGaugeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
