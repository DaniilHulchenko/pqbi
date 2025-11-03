import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WidgetPqsCardComponent } from './widget-pqs-card.component';

describe('WidgetPqsCardComponent', () => {
  let component: WidgetPqsCardComponent;
  let fixture: ComponentFixture<WidgetPqsCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WidgetPqsCardComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(WidgetPqsCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
